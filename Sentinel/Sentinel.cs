using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Sentinel
{
    /**
     * Main class that handles listening to LCU events and submitting events.
     */
    class Sentinel
    {
        public static readonly string StorageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sentinel");
        public static readonly string SettingsPath = Path.Combine(StorageDir, "settings.bin");

        private static readonly Regex ConversationRegex = new Regex("/lol-chat/v1/conversations/([^/]+)$");

        private readonly LeagueConnection league = new LeagueConnection();
        private readonly Dictionary<long, dynamic> summonerCache = new Dictionary<long, dynamic>();

        // Keeps track of our invitations, so we can diff and remove
        // remove notifications if an invitation gets removed.
        private List<string> _invitationIds = new List<string>();

        // Keeps track of our unread message count. If we have unread messages
        // and they increase, it means that we received a new message that we
        // need to notify the user for.
        private readonly Dictionary<string, int> unreadMessageCounts = new Dictionary<string, int>();

        private bool gameClientRunning = false;
        private long summonerId = -1;
        private string activeConversation;
        public SentinelSettings settings;

        public Sentinel()
        {
            league.OnConnected += HandleConnect;
            league.OnDisconnected += HandleDisconnect;
            league.OnWebsocketEvent += PotentiallyHandleNewMessage;

            league.Observe("/lol-lobby/v2/received-invitations", HandleInviteUpdate);
            league.Observe("/lol-chat/v1/conversations/active", HandleActiveConversationUpdate);
            league.Observe("/lol-gameflow/v1/session", HandleGameflowUpdate);
            league.Observe("/lol-summoner/v1/current-summoner", summ => summonerId = summ != null ? summ["summonerId"] : -1);

            // This will do nothing if the directory already exists.
            Directory.CreateDirectory(StorageDir);

            // Load settings, or use defaults if they don't exist.
            if (!File.Exists(SettingsPath))
            {
                settings = new SentinelSettings();
            }
            else
            {
                using (Stream stream = File.Open(SettingsPath, FileMode.Open))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    settings = (SentinelSettings) binaryFormatter.Deserialize(stream);
                }
            }
        }

        /**
         * Handles an activation by the user clicking on a notification.
         */
        public void HandleActivation(string[] args, Dictionary<string, string> values)
        {
            // Our actions are always in the format "action|arg1|arg2|..."
            switch (args[0])
            {
                // Focus the league client, without doing anything.
                case "focus":
                    league.Focus();
                    return;

                // Focus the league client and open the specified chat.
                case "focus_chat":
                    league.Focus();
                    league.Put("/lol-chat/v1/conversations/active", "{\"id\":\"" + args[1] + "\"}");
                    return;

                // Either accept or deny the given invite.
                // Also focus the league client if we accepted.
                case "invite":
                    league.Post("/lol-lobby/v2/received-invitations/" + args[1] + "/" + args[2], "");
                    if (args[2] != "decline") league.Focus();
                    return;

                // Reply to the user in the background.
                case "reply":
                    league.Post("/lol-chat/v1/conversations/" + args[1] + "/messages", SimpleJson.SerializeObject(new
                    {
                        body = values["content"]
                    }));
                    return;

                default:
                    // Not sure what to do here?
                    break;
            }
        }

        /**
         * Handles the event fired by LeagueConnection when it connects to league.
         */
        private void HandleConnect()
        {
            Console.WriteLine("[+] Connected to League.");
            NotificationManager.Clear();
        }

        /**
         * Handles the event fired by LeagueConnection when it disconnects from league.
         */
        private void HandleDisconnect()
        {
            Console.WriteLine("[+] Disconnected from League.");
            NotificationManager.Clear();
        }

        /**
         * Asynchronously finds the summoner information of the specified summoner.
         */
        private async Task<dynamic> GetSummoner(long summonerId)
        {
            // We make a request regardless of if we already have it cached.
            // Note that we do not await this task (yet). It will run async.
            var task = Task.Run(async () =>
            {
                var summoner = await league.Get("/lol-summoner/v1/summoners/" + summonerId);
                summonerCache[summonerId] = summoner;
                return summoner;
            });

            // If we had the user cached, return the cached icon.
            // If they changed their icon in the meantime, the task we just started will update it for next time.
            if (summonerCache.ContainsKey(summonerId))
            {
                return summonerCache[summonerId];
            }

            // If we didn't have it cached, we will have to wait for the operation to return.
            return await task;
        }

        /**
         * Finds the summoner icon of the specified summoner and returns the path
         * to a local version of the icon. This will potentially "download" the icon
         * from the LCU, to store it locally.
         */
        private async Task<string> GetSummonerIconPath(long summonerId)
        {
            var summoner = await GetSummoner(summonerId);
            var icon = (int) summoner["profileIconId"];
            var iconPath = Path.Combine(StorageDir, icon + ".png");

            // Download if it does not exist.
            if (!File.Exists(iconPath))
            {
                File.WriteAllBytes(iconPath, await league.GetAsset("/lol-game-data/assets/v1/profile-icons/" + icon + ".jpg"));
            }

            return iconPath;
        }

        /**
         * Called when our list of invites changes. We do a diff check to
         * see which invites disappeared (and remove those notifications)
         * and for all other notifications we either show them or remove them, 
         * depending on their state.
         */
        private async void HandleInviteUpdate(dynamic dynInvites)
        {
            var invites = (List<dynamic>)dynInvites;
            
            // Remove invites that no longer exist.
            var newIds = invites.Select(x => (string)x["invitationId"]);
            foreach (var oldId in _invitationIds.Except(newIds))
            {
                NotificationManager.HideInviteNotification(oldId);
            }
            _invitationIds = newIds.ToList();

            // For every invite that still exists, show it if applicable.
            foreach (var invite in invites)
            {
                if (invite["canAcceptInvitation"] && invite["state"] == "Pending")
                {
                    string queueName;
                    if ((long) invite["gameConfig"]["queueId"] == -1)
                    {
                        queueName = "Custom Game";
                    }
                    else
                    {
                        var queueInfo = await league.Get("/lol-game-queues/v1/queues/" + invite["gameConfig"]["queueId"]);
                        queueName = queueInfo["shortName"];
                    }

                    var mapInfo = await league.Get("/lol-maps/v1/map/" + invite["gameConfig"]["mapId"]);
                    var iconPath = await GetSummonerIconPath(invite["fromSummonerId"]);

                    NotificationManager.ShowInviteNotification(
                        invite["invitationId"],
                        iconPath,
                        invite["fromSummonerName"],
                        mapInfo["name"] + " - " + queueName
                    );
                } else
                {
                    // This will do nothing if we didn't previously display it, so it is fine.
                    NotificationManager.HideInviteNotification(invite["invitationId"]);
                }
            }
        }

        /**
         * Called when our current conversation changes. We keep track of the current
         * conversation to make sure that we do not send notificatons for the convo that
         * the user is currently looking at.
         */
        private void HandleActiveConversationUpdate(dynamic payload)
        {
            if (payload == null)
            {
                activeConversation = null;
            } else
            {
                // Hide all the notifications for the chat that just became active.
                activeConversation = payload["id"];
                NotificationManager.HideChatNotifications(activeConversation);
            }
        }

        /**
         * Handles an update to a conversation. We (ab)use the unreadMessageCount prop
         * to figure out if the user received a new message that we should show. This is
         * cleaner than figuring out if the message was previously displayed and it seems
         * to work fairly reliably.
         */
        private async void PotentiallyHandleNewMessage(OnWebsocketEventArgs payload)
        {
            // Only listen to /lol-chat/v1/conversation/<id> updates.
            var match = ConversationRegex.Match(payload.Path);
            if (!match.Success) return;

            var id = match.Groups[1].Value;
            if (id == "notify" || id == "active" || payload.Data == null || payload.Data["lastMessage"] == null) return;

            // We don't care about messages if we're currently ingame and have in-game notifications turned off.
            if (gameClientRunning && !settings.ShowWhileIngame) return;

            // Ignore anything that is not DMs or club chats, unless we have all messages turned on.
            if (!settings.ShowAllConversations && payload.Data["type"] != "chat" && payload.Data["type"] != "club") return;

            // Stop spamming me if the club is marked as muted.
            if (payload.Data["type"] == "club" && payload.Data["isMuted"] == true) return;

            // If this is a lobby/champselect chat, only send messages if we're not focused.
            if (payload.Data["type"] != "chat" && payload.Data["type"] != "club" && league.IsFocused) return;

            // If the number of unread messages increased, it means we have a new message to emit.
            var lastUnread = unreadMessageCounts.ContainsKey(id) ? unreadMessageCounts[id] : 0;

            // Show a message if the unread counter increased or if we are currently not focused
            // and our open conversation got a message not sent by us.
            var shouldShow = (lastUnread < payload.Data["unreadMessageCount"] && id != activeConversation)
                             || (!league.IsFocused && id == activeConversation && payload.Data["lastMessage"]["fromSummonerId"] != summonerId);  

            if (shouldShow)
            {
                var name = (string) payload.Data["name"];
                if (name.IsNullOrEmpty())
                {
                    var summoner = await GetSummoner(payload.Data["lastMessage"]["fromSummonerId"]);
                    name = "Lobby - " + summoner["displayName"];
                }

                var iconPath = await GetSummonerIconPath(payload.Data["lastMessage"]["fromSummonerId"]);
                NotificationManager.ShowChatNotification(
                    id,
                    iconPath,
                    name,
                    payload.Data["lastMessage"]["body"],
                    payload.Data["type"] == "chat" || payload.Data["type"] == "club"
                );
            }

            unreadMessageCounts[id] = (int) payload.Data["unreadMessageCount"];
        }

        /**
         * Listens to any lol-gameflow updates to check if we're currently in-game or not.
         */
        private void HandleGameflowUpdate(dynamic gameflow)
        {
            // The game client is running if the gameflow session isn't null and gameClient.running is true.
            gameClientRunning = gameflow != null && gameflow["gameClient"] != null && gameflow["gameClient"]["running"];
        }
    }

    [Serializable]
    public class SentinelSettings
    {
        /**
         * If we should show all notifications, not just clubs + dms.
         */
        public bool ShowAllConversations = false;

        /**
         * If we should send messages while we're ingame.
         */
        public bool ShowWhileIngame = false;
    }
}
