using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Sentinel
{
    static class NotificationManager
    {
        private static ToastNotifier NOTIFIER = ToastNotificationManager.CreateToastNotifier(App.APP_ID);
        private static int COUNTER = 0;

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
        public static void ShowInviteNotification(string id, string name, string description)
        {
            // Do not show invites twice.
            if (inviteToasts.ContainsKey(id)) return;

            // Build our toast.
            ToastContent toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "League Of Legends | Invite From " + name
                            },

                            new AdaptiveText()
                            {
                                Text = description
                            }
                        }
                    },
                },

                Actions = new ToastActionsCustom()
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

            try
            {
                NOTIFIER.Hide(toast);
                ToastNotificationManager.History.Remove(toast.Tag);
            } catch
            {
                // If these were removed by the user, this might not work. We don't really bother.
            }
        }

        /**
         * Internal helper method to convert the specified ToastContent into 
         * a ToastNotification. This will immediately show the toast.
         */
        private static ToastNotification SendNotification(ToastContent content)
        {
            var xml = new XmlDocument();
            xml.LoadXml(content.GetContent());

            var toast = new ToastNotification(xml);
            toast.Tag = "" + COUNTER++;

            NOTIFIER.Show(toast);
            return toast;
        }
    }
}
