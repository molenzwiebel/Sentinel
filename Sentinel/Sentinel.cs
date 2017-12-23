using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sentinel
{
    /**
     * Main class that handles listening to LCU events and submitting events.
     */
    class Sentinel
    {
        private static Dictionary<int, Tuple<string, string>> QUEUES = new Dictionary<int, Tuple<string, string>>
        {
            { 400, new Tuple<string, string>("Draft Pick", "Summoner's Rift") },
            { 420, new Tuple<string, string>("Ranked Solo", "Summoner's Rift") },
            { 430, new Tuple<string, string>("Blind Pick", "Summoner's Rift") },
            { 440, new Tuple<string, string>("Ranked Flex", "Summoner's Rift") },
            { 450, new Tuple<string, string>("ARAM", "Howling Abyss") },
            { 460, new Tuple<string, string>("Blind 3v3", "Twisted Treeline") },
            { 470, new Tuple<string, string>("Ranked 3v3", "Twisted Treeline") },
        };
        private static Regex CONVERSATION_REGEX = new Regex("/lol-chat/v1/conversations/([^/]+)$");

        private LeagueConnection league = new LeagueConnection();

        // Keeps track of our invitations, so we can diff and remove
        // remove notifications if an invitation gets removed.
        private List<string> invitationIds = new List<string>();

        // Keeps track of our unread message count. If we have unread messages
        // and they increase, it means that we received a new message that we
        // need to notify the user for.
        private Dictionary<string, int> unreadMessageCounts = new Dictionary<string, int>();

        private string activeConversation = null;

        public Sentinel()
        {
            league.OnConnected += HandleConnect;
            league.OnDisconnected += HandleDisconnect;
            league.OnWebsocketEvent += PotentiallyHandleNewMessage;

            league.Observe("/lol-lobby/v2/received-invitations", HandleInviteUpdate);
            league.Observe("/lol-chat/v1/conversations/active", HandleActiveConversationUpdate);
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

        private void HandleConnect()
        {
            Console.WriteLine("[+] Connected to League.");
            NotificationManager.Clear();
        }

        private void HandleDisconnect()
        {
            Console.WriteLine("[+] Disconnected from League.");
            NotificationManager.Clear();
        }

        private void HandleInviteUpdate(dynamic dynInvites)
        {
            List<dynamic> invites = (List<dynamic>)dynInvites;
            
            // Remove invites that no longer exist.
            var newIds = invites.Select(x => (string)x["invitationId"]);
            foreach (var oldId in invitationIds.Except(newIds))
            {
                NotificationManager.HideInviteNotification(oldId);
            }
            invitationIds = newIds.ToList();

            // For every invite that still exists, show it if applicable.
            foreach (var invite in invites)
            {
                if (invite["canAcceptInvitation"] && invite["state"] == "Pending")
                {
                    var queueId = (int) invite["gameConfig"]["queueId"];
                    var queueInfo = QUEUES.ContainsKey(queueId) ? QUEUES[queueId] : null;
                    var details = queueInfo != null ? queueInfo.Item1 + " - " + queueInfo.Item2 : "Rotating Gamemode";

                    NotificationManager.ShowInviteNotification(invite["invitationId"], invite["fromSummonerName"], details);
                } else
                {
                    // This will do nothing if we didn't previously display it, so it is fine.
                    NotificationManager.HideInviteNotification(invite["invitationId"]);
                }
            }
        }

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

        private void PotentiallyHandleNewMessage(OnWebsocketEventArgs payload)
        {
            var match = CONVERSATION_REGEX.Match(payload.Path);
            if (match == null || !match.Success) return;

            var id = match.Groups[1].Value;
            if (id == "notify" || id == "active") return;

            // If the number of unread messages increased, it means we have a new message to emit.
            var lastUnread = unreadMessageCounts.ContainsKey(id) ? unreadMessageCounts[id] : 0;
            if (lastUnread < payload.Data["unreadMessageCount"] && id != activeConversation)
            {
                NotificationManager.ShowChatNotification(id, payload.Data["name"], payload.Data["lastMessage"]["body"]);
            }

            unreadMessageCounts[id] = (int) payload.Data["unreadMessageCount"];
        }
    }
}
