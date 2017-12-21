using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebSocketSharp;

namespace Sentinel
{
    /**
     * Class that manages a connection with the LCU. This will automatically connect to the League
     * client once it starts, and exposes a simple API for listening to requests, making requests
     * and lifetime events.
     */
    class LeagueConnection
    {
        private static HttpClient HTTP_CLIENT = new HttpClient();

        private Timer leaguePollTimer;
        private WebSocket socketConnection;
        private Tuple<int, string, string> processInfo;
        private bool connected = false;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<OnWebsocketEventArgs> OnWebsocketEvent;

        /**
         * Returns if this connection is currently connected.
         */
        public bool IsConnected {
            get { return connected; }
        }

        /**
         * Creates a new LeagueConnection instance. This will immediately start trying
         * to connect to League.
         */
        public LeagueConnection()
        {
            leaguePollTimer = new Timer();
            leaguePollTimer.Interval = 5000;
            leaguePollTimer.Elapsed += TryConnect;
            leaguePollTimer.Start();
        }

        /**
         * Tries to connect to a currently running league process. Called
         * by the connection timer every 5 seconds.
         */
        private void TryConnect(object sender, EventArgs args)
        {
            // We're already connected.
            if (connected) return;

            // Check league status, abort if league is not running.
            var status = LeagueUtils.GetLeagueStatus();
            if (status == null) return;

            // Update local state.
            leaguePollTimer.Stop();
            processInfo = status;
            connected = true;

            // Set the password and base address for our httpclient so we don't have to specify it every time.
            var byteArray = Encoding.ASCII.GetBytes("riot:" + status.Item2);
            HTTP_CLIENT.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            // Connect to our websocket.
            socketConnection = new WebSocket("wss://127.0.0.1:" + status.Item3 + "/", "wamp");
            socketConnection.SetCredentials("riot", status.Item2, true);
            socketConnection.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            socketConnection.SslConfiguration.ServerCertificateValidationCallback = (a, b, c, d) => true;
            socketConnection.OnMessage += HandleMessage;
            socketConnection.OnClose += HandleDisconnect;
            socketConnection.Connect();

            // Subscribe to Json API events from the LCU.
            socketConnection.Send("[5,\"OnJsonApiEvent\"]");

            // Emit our events.
            OnConnected?.Invoke();
        }

        /**
         * Called when our websocket connection is closed. Responsible for updating internal state.
         */
        private void HandleDisconnect(object sender, CloseEventArgs args)
        {
            // Update internal state.
            leaguePollTimer.Start();
            processInfo = null;
            connected = false;
            socketConnection = null;

            // Notify observers.
            OnDisconnected?.Invoke();
        }

        /**
         * Called when we receive a websocket message. Responsible for invoking event handlers.
         */
        private void HandleMessage(object sender, MessageEventArgs args)
        {
            // Abort if we get an invalid payload.
            if (!args.IsText) return;
            var payload = SimpleJson.DeserializeObject<JsonArray>(args.Data);

            // Abort if this is not a OnJsonApiEvent.
            if (payload.Count != 3) return;
            if ((long)payload[0] != 8 || !((string)payload[1]).Equals("OnJsonApiEvent")) return;

            // Invoke our listeners.
            var ev = (dynamic)payload[2];
            OnWebsocketEvent?.Invoke(new OnWebsocketEventArgs()
            {
                Path = ev["uri"],
                Type = ev["eventType"],
                Data = ev["data"]
            });
        }

        /**
         * Performs a GET request on the specified URL.
         */
        public async Task<dynamic> Get(string url)
        {
            if (!connected) throw new InvalidOperationException("Not connected to LCU");

            var res = await HTTP_CLIENT.GetAsync("https://127.0.0.1:" + processInfo.Item3 + url);
            var stringContent = await res.Content.ReadAsStringAsync();
            return SimpleJson.DeserializeObject(stringContent);
        }

        /**
         * Performs a POST request on the specified URL with the specified body.
         */
        public async Task Post(string url, string body)
        {
            if (!connected) throw new InvalidOperationException("Not connected to LCU");

            await HTTP_CLIENT.PostAsync("https://127.0.0.1:" + processInfo.Item3 + url, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        /**
         * Performs a PUT request on the specified URL with the specified body.
         */
        public async Task Put(string url, string body)
        {
            if (!connected) throw new InvalidOperationException("Not connected to LCU");

            await HTTP_CLIENT.PutAsync("https://127.0.0.1:" + processInfo.Item3 + url, new StringContent(body, Encoding.UTF8, "application/json"));
        }

        /**
         * Performs a DELETE request on the specified URL.
         */
        public async Task Delete(string url)
        {
            if (!connected) throw new InvalidOperationException("Not connected to LCU");

            await HTTP_CLIENT.DeleteAsync("https://127.0.0.1:" + processInfo.Item3 + url);
        }
    }

    /**
     * Represents an OnJsonApiEvent sent by the websocket API.
     */
    public class OnWebsocketEventArgs : EventArgs
    {
        /**
         * The path that was modified.
         */
        public string Path { get; set; }

        /**
         * The type of the modification (Create/Update/Delete).
         */
        public string Type { get; set; }

        /**
         * The payload of the event. May be null.
         */
        public dynamic Data { get; set; }
    }
}
