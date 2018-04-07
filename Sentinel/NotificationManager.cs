using System.Collections.Generic;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Sentinel
{
    static class NotificationManager
    {
        private static ToastNotifier NOTIFIER = ToastNotificationManager.CreateToastNotifier(App.AppId);
        private static int COUNTER;

        private static Dictionary<string, ToastNotification> inviteToasts = new Dictionary<string, ToastNotification>();
        private static Dictionary<string, List<ToastNotification>> chatToasts = new Dictionary<string, List<ToastNotification>>();

        /**
         * Removes all notifications from the notification center and the local storage.
         */
        public static void Clear()
        {
            ToastNotificationManager.History.Clear();
            inviteToasts.Clear();
            chatToasts.Clear();
        }

        /**
         * Shows a notification for a new game invite. Does nothing if a notification
         * with the specified ID is already shown.
         */
        public static void ShowInviteNotification(string id, string icon, string name, string description)
        {
            // Do not show invites twice.
            if (inviteToasts.ContainsKey(id)) return;

            // Build our toast.
            ToastContent toastContent = new ToastContent
            {
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = "Invite From " + name
                            },

                            new AdaptiveText
                            {
                                Text = description
                            }
                        },

                        AppLogoOverride = new ToastGenericAppLogo
                        {
                            Source = icon
                        },

                        Attribution = new ToastGenericAttributionText
                        {
                            Text = "Via League of Legends"
                        }
                    }
                },

                Actions = new ToastActionsCustom
                {
                    Buttons =
                    {
                        new ToastButton("Accept", "invite|" + id + "|accept"),
                        new ToastButton("Decline", "invite|" + id + "|decline")
                    }
                },

                // Simply focus the league client if nothing is done.
                Launch = "focus",
                ActivationType = ToastActivationType.Foreground
            };

            var toast = SendNotification(toastContent);
            inviteToasts.Add(id, toast);
        }

        /**
         * Removes the notification for the specified game id, unless we have never shown one.
         */
        public static void HideInviteNotification(string id)
        {
            if (!inviteToasts.ContainsKey(id)) return;
            var toast = inviteToasts[id];

            inviteToasts.Remove(id);

            // Each one of these might throw if the user interacted with them.
            try { NOTIFIER.Hide(toast); } catch { /* ignored */ }
            try { ToastNotificationManager.History.Remove(toast.Tag); } catch { /* ignored */ }
        }

        /**
         * Shows a notification for a chat message sent to the player.
         */
        public static void ShowChatNotification(string id, string icon, string from, string content, bool openChat)
        {
            // Get or put the list of toast notifications for this convo
            var convoMessages = chatToasts[id] = chatToasts.ContainsKey(id) ? chatToasts[id] : new List<ToastNotification>();

            // Build our toast.
            ToastContent toastContent = new ToastContent
            {
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = from
                            },

                            new AdaptiveText
                            {
                                Text = content
                            }
                        },

                        AppLogoOverride = new ToastGenericAppLogo
                        {
                            Source = icon
                        },

                        Attribution = new ToastGenericAttributionText
                        {
                            Text = "Via League of Legends"
                        }
                    }
                },

                Actions = new ToastActionsCustom
                {
                    Inputs =
                    {
                        new ToastTextBox("content")
                        {
                            PlaceholderContent = "Type a response..."
                        }
                    },

                    Buttons =
                    {
                        new ToastButton("Reply", "reply|" + id)
                        {
                            TextBoxId = "content"
                        }
                    }
                },

                // Either open the chat or just focus the client, based on the param
                Launch = openChat ? "focus_chat|" + id : "focus",
                ActivationType = ToastActivationType.Foreground,
                Duration = ToastDuration.Short
            };

            var toast = SendNotification(toastContent);
            convoMessages.Add(toast);
        }

        /**
         * Removes all notifications that were shown for the specified conversation id.
         */
        public static void HideChatNotifications(string id)
        {
            if (!chatToasts.ContainsKey(id)) return;

            var toasts = chatToasts[id];
            foreach (var toast in toasts)
            {
                // These may throw.
                try { NOTIFIER.Hide(toast); } catch { /* ignored */ }
                try { ToastNotificationManager.History.Remove(toast.Tag); } catch { /* ignored */ }
            }

            toasts.Clear();
            chatToasts.Remove(id);
        }

        /**
         * Internal helper method to convert the specified ToastContent into 
         * a ToastNotification. This will immediately show the toast.
         */
        private static ToastNotification SendNotification(ToastContent content)
        {
            var xml = new XmlDocument();
            xml.LoadXml(content.GetContent());

            var toast = new ToastNotification(xml)
            {
                Tag = "" + COUNTER++
            };

            NOTIFIER.Show(toast);
            return toast;
        }
    }
}
