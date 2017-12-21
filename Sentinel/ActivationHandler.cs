using System;
using System.Runtime.InteropServices;
using static Sentinel.COMServerHelpers;

namespace Sentinel
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("7A75434B-A215-440E-BC92-3949316DC3B4"), ComVisible(true)]
    public class ActivationHandler : INotificationActivationCallback
    {
        public void Activate(string appUserModelId, string invokedArgs, NotificationData[] data, uint dataCount)
        {
            // TODO
            Console.WriteLine(appUserModelId);
            Console.WriteLine(invokedArgs);
            Console.WriteLine("A");
        }

        public static void Initialize()
        {
            regService = new RegistrationServices();
            cookie = regService.RegisterTypeForComClients(typeof(ActivationHandler), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse);
        }

        public static void Uninitialize()
        {
            if (cookie != -1 && regService != null)
            {
                regService.UnregisterTypeForComClients(cookie);
            }
        }

        private static int cookie = -1;
        private static RegistrationServices regService = null;
    }
}
