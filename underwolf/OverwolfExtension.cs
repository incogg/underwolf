using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client;

namespace underwolf {
    class JSRequest {
        public int id { get; set; }
        public string method { get; set; }
        public Dictionary<string, string> @params { get; set; }

        public JSRequest( int id, string expression ) {
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
            if ( title == null )
                return true;
            if ( webSocketDebuggerUrl == null )
                return true;
            return false;
        }
    }


    internal class OverwolfExtension {
        public string Title;
        public string ExtensionID;

        private Logger Logger = new("Extension");
        private WebsocketClient WSClient;
        private FileServer FileServer;
        private List<int> ProcessingIds = new();
        private bool Connected;
        private int CurrentID = 0;
        private string WebSocketDebuggerUrl;
        private string ConfigPath;

        public OverwolfExtension( ExtensionJSON json, string appID ) {
            Title = json.title;
            ExtensionID = appID;
            WebSocketDebuggerUrl = json.webSocketDebuggerUrl;
            ConfigPath = Path.Join( Program.CONFIG_PATH, ExtensionID );

            FileServer = new( ConfigPath );
            FileServer.OnExtensionReload += OnExtensionReload;
            FileServer.OnExtensionDisconnect += OnExtensionDisconnect;
            FileServer.OnFilesChanged += OnFilesChanged;
            FileServer.AddResource("underwolf-utilities.js", Path.Join(Program.CONFIG_PATH, "utilities.js"));
            FileServer.AddVariable("[UNDERWOLF-FILESERVER]", FileServer.Url);

            Logger = new( Title );

            WSClient = new( new Uri( WebSocketDebuggerUrl ) );
            WSClient.ReconnectTimeout = TimeSpan.FromSeconds( 30 );
            WSClient.ReconnectionHappened.Subscribe( info => {
                //if ( info.Type != ReconnectionType.Initial ) return;
                Logger.Info($"Web socket connected : {info.Type}");
                Connected = true;
            } );
            WSClient.DisconnectionHappened.Subscribe( info => {
                Logger.Info( $"Web socket disconnected: {info.Type}" );
                Connected = false;
            } );
            WSClient.MessageReceived.Subscribe( msg => LogResponse( msg.Text ) );
        }

        public async Task Connect() {
            FileServer.Start();
            await WSClient.Start();
            while ( !Connected ) ;  // wait for the socket to connect
        }

        public async Task Disconnect() {
            FileServer.Stop();
            while ( ProcessingIds.Count > 0 ) ; // wait until we are no longer waiting for responses
            await WSClient.Stop( WebSocketCloseStatus.NormalClosure, "" );
            while ( Connected ) ; // wait for the socket to disconnect
        }

        private void OnFilesChanged() {
            Logger.Info("Files changed. reloading Extension");
            InjectJS("location.reload();");
        }

        private void OnExtensionReload() {
            Logger.Info("Extension Refreshed");
            Thread.Sleep(2000); // just make sure the page has finished reloading
            InjectAllFiles();
        }

        private void OnExtensionDisconnect() {
            Task.Run(Disconnect);
        }

        public void InjectJS( string js ) {
            int id = GetNextID();
            JSRequest jsr = new(id, js);
            Logger.Info( $"[{id}] Injected JS: {js.Replace("\n", " ")}" );
            WSClient.Send( JsonSerializer.Serialize( jsr ).Replace( "\\u0027", "'" ) );
        }

        public void InjectJSFile( string name ) {
            string content = "var underwolfScript = document.createElement('script');\n" +
                            $"underwolfScript.src = '{FileServer.Url}{name}';\n" +
                             "document.head.appendChild(underwolfScript);";
            InjectJS( content );
        }

        public void InjectCSSFile( string name ) {
            string content = "var underwolfLink = document.createElement('link');\n" +
                             "underwolfLink.rel = 'stylesheet';\n" +
                             "underwolfLink.type = 'text/css';\n" +
                            $"underwolfLink.href = '{FileServer.Url}{name}';\n" +
                             "document.head.appendChild(underwolfLink);";
            InjectJS( content );
        }

        public void InjectUtilities() {
            InjectJSFile("underwolf-utilities.js");
        }

        public void InjectAllFiles() {
            InjectUtilities();
            foreach ( string file in Directory.GetFiles( ConfigPath ) ) {
                string fileName = file.Replace(ConfigPath, string.Empty ).Replace("\\", "");
                switch ( Path.GetExtension( fileName )[1..] ) {
                    case "js":
                        InjectJSFile( fileName );
                        break;
                    case "css":
                        InjectCSSFile( fileName );
                        break;
                }
            }
        }

        private void ClearID( int id ) {
            ProcessingIds.Remove( id );
        }

        private int GetNextID() {
            int ret = CurrentID;
            ProcessingIds.Add( ret );
            CurrentID++;
            return ret;
        }

        private void LogResponse( string response ) {
            JSResponseWrapper? wrapper = JsonSerializer.Deserialize<JSResponseWrapper>( response );
            if ( wrapper == null || wrapper.result == null ) return;
            JSResponse? res = wrapper.result["result"];

            string result = "";
            if ( res != null ) result = res.description;
            ClearID( wrapper.id );
            Logger.Info( $"[{wrapper.id}] Processed response: {result}" );
        }
    }
}
