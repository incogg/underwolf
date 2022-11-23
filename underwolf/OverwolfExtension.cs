using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace underwolf {
    class JSRequest {
        public int id { get; set; }
        public string method { get; set; }
        public Dictionary<string, string> @params { get; set; }

        public JSRequest(int id, string expression) {
            this.id = id;
            method = "Runtime.evaluate";
            @params = new Dictionary<string, string> {
                { "expression", expression.Replace("\"", "'") }
            };
        }
    }

    class JSResponse {
        public string className { get; set; }
        public string description { get; set; }

    }

    class JSResponseWrapper {
        public int id { get; set; }
        public Dictionary<string, JSResponse>? result { get; set; }
    }

    class ExtensionJSON {
        public string title { get; set; }
        public string webSocketDebuggerUrl { get; set; }

        public bool IsInvalid() {
            return (title == null || webSocketDebuggerUrl == null);
        }
    }


    internal class OverwolfExtension {
        public string Title;
        public string ExtensionID;

        private Logger Logger;
        private WebsocketClient WSClient;
        private FileServer FileServer;
        private List<int> ProcessingIds = new();
        private bool Connected;
        private int CurrentID = 0;
        private string WebSocketDebuggerUrl;
        private string ConfigPath;
        private bool KeepAlive;

        public OverwolfExtension(ExtensionJSON json, string appID, bool keepAlive = false) {
            Title = json.title;
            ExtensionID = appID;
            WebSocketDebuggerUrl = json.webSocketDebuggerUrl;
            ConfigPath = Path.Join(Program.CONFIG_FOLDER, ExtensionID);
            Logger = new(Title);
            KeepAlive = keepAlive;
            if (KeepAlive) Logger.Info("Keep alive enabled");

            FileServer = new(ConfigPath);
            FileServer.OnExtensionReload += OnExtensionReload;
            FileServer.OnExtensionDisconnect += OnExtensionDisconnect;
            FileServer.OnFilesChanged += OnFilesChanged;
            FileServer.AddResource("underwolf-utilities.js", Path.Join(Program.CONFIG_FOLDER, "utilities.js"));
            FileServer.AddVariable("[UNDERWOLF-FILESERVER]", FileServer.Url);

            WSClient = new(new Uri(WebSocketDebuggerUrl)) {
                ReconnectTimeout = TimeSpan.FromSeconds(30)
            };
            WSClient.ReconnectionHappened.Subscribe(info => {
                Logger.Info($"Web socket connected : {info.Type}");
                Connected = true;
            });
            WSClient.DisconnectionHappened.Subscribe(info => {
                Logger.Info($"Web socket disconnected: {info.Type}");
                Connected = false;
            });
            WSClient.MessageReceived.Subscribe(msg => OnWebsocketResponse(msg.Text));
        }

        /// <summary>
        /// Starts the file server and connects to the Websocket
        /// </summary>
        public async Task Connect() {
            FileServer.Start();
            await WSClient.Start();
            while (!Connected) ;  // wait for the socket to connect
        }

        /// <summary>
        /// Stops the file server and disconnects from the Websocket
        /// </summary>
        public async Task Disconnect() {
            FileServer.Stop();
            while (ProcessingIds.Count > 0) ; // wait until we are no longer waiting for responses
            await WSClient.Stop(WebSocketCloseStatus.NormalClosure, "");
            while (Connected) ; // wait for the socket to disconnect
        }

        /// <summary>
        /// Refreshes the extension when files are changed in the config directory
        /// </summary>
        private void OnFilesChanged() {
            Logger.Info("Files changed. reloading Extension");
            InjectJS("location.reload();");
        }

        /// <summary>
        /// Re injects js files when the extension is refreshed
        /// </summary>
        private void OnExtensionReload() {
            Logger.Info("Extension Refreshed");
            Thread.Sleep(2000); // just make sure the page has finished reloading
            InjectAllFiles();
        }

        /// <summary>
        /// Runs the Disconnect method when the extension makes a disconnect request
        /// </summary>
        private void OnExtensionDisconnect() {
            Task.Run(Disconnect);
        }

        /// <summary>
        /// Makes a Websocket request to inject a string of js
        /// </summary>
        /// <param name="js">js to inject</param>
        public void InjectJS(string js) {
            JSRequest jsr = new(CurrentID, js);
            Logger.Info($"[{CurrentID}] Injected JS: {js.Replace("\n", " ")}");

            ProcessingIds.Add(CurrentID);
            CurrentID++;

            WSClient.Send(JsonSerializer.Serialize(jsr).Replace("\\u0027", "'"));
        }

        /// <summary>
        /// Injects a js file
        /// </summary>
        /// <param name="name">name of the resource</param>
        public void InjectJSFile(string name) {
            string content = "var underwolfScript = document.createElement('script');\n" +
                            $"underwolfScript.src = '{FileServer.Url}{name}';\n" +
                             "document.head.appendChild(underwolfScript);";
            InjectJS(content);
        }

        /// <summary>
        /// Injects a css file
        /// </summary>
        /// <param name="name">name of the resource</param>
        public void InjectCSSFile(string name) {
            string content = "var underwolfLink = document.createElement('link');\n" +
                             "underwolfLink.rel = 'stylesheet';\n" +
                             "underwolfLink.type = 'text/css';\n" +
                            $"underwolfLink.href = '{FileServer.Url}{name}';\n" +
                             "document.head.appendChild(underwolfLink);";
            InjectJS(content);
        }

        /// <summary>
        /// Gets all the file names in the config directory and injects them into the extension
        /// </summary>
        public void InjectAllFiles() {
            InjectJSFile("underwolf-utilities.js");

            foreach (string file in Directory.GetFiles(ConfigPath)) {
                string fileName = file.Replace(ConfigPath, string.Empty ).Replace("\\", "");
                switch (Path.GetExtension(fileName)[1..]) {
                    case "js": InjectJSFile(fileName); break;
                    case "css": InjectCSSFile(fileName); break;
                }
            }

            if (!KeepAlive) InjectJSFile("disconnect");
        }

        /// <summary>
        /// Processes the Websocket Reponse
        /// </summary>
        /// <param name="response">response content</param>
        private void OnWebsocketResponse(string response) {
            JSResponseWrapper? wrapper = JsonSerializer.Deserialize<JSResponseWrapper>( response );
            if (wrapper == null || wrapper.result == null) return;
            JSResponse? res = wrapper.result["result"];

            ProcessingIds.Remove(wrapper.id);
            string result = (res != null) ? res.description : "";
            Logger.Info($"[{wrapper.id}] Processed response: {result}");
        }
    }
}
