using System;
using System.Collections.Generic;
using System.Linq;

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

        private LeagueConnection league = new LeagueConnection();

        // Keeps track of our invitations, so we can diff and remove
        // remove notifications if an invitation gets removed.
        private List<string> invitationIds = new List<string>();

        public Sentinel()
        {
            league.OnConnected += HandleConnect;
            league.OnDisconnected += HandleDisconnect;

            league.Observe("/lol-lobby/v2/received-invitations", HandleInviteUpdate);
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

                // Either accept or deny the given invite. Also focus the league client.
                case "invite":
                    league.Post("/lol-lobby/v2/received-invitations/" + args[1] + "/" + args[2], "");
                    league.Focus();
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
                    var queueInfo = QUEUES[(int) invite["gameConfig"]["queueId"]];
                    var details = queueInfo != null ? queueInfo.Item1 + " - " + queueInfo.Item2 : "Rotating Gamemode";

                    NotificationManager.ShowInviteNotification(invite["invitationId"], invite["fromSummonerName"], details);
                } else
                {
                    // This will do nothing if we didn't previously display it, so it is fine.
                    NotificationManager.HideInviteNotification(invite["invitationId"]);
                }
            }
        }
    }
}
