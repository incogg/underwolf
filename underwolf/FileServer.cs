using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.NetworkInformation;

namespace underwolf {

    public delegate void ExtensionReload();

    internal class FileServer {

        public string Url;
        public string FolderPath;
        public event ExtensionReload? OnExtensionReload;

        private HttpListener Listener;
        private Logger Logger = new("FileServer");
        private List<string> Files = new();
        private bool Running;

        public FileServer(string path) {
            Url = $"http://localhost:{GetFirstAvailablePort(8000)}/";
            FolderPath = path;

            Listener = new();
            Listener.Prefixes.Add( Url );
        }

        public void Start() {
            Listener.Start();
            Running = true;
            Logger.Info( $"Started listening at {Url}" );
            Task.Run(HandleIncomingConnections);
        }

        public void Stop() {
            Logger.Info( "Waiting for disconnect request..." );
            while ( Running ) ; 
            Logger.Info( "Stopped" );
            Listener.Stop();
        }

        public async Task HandleIncomingConnections() {
            while ( Running ) {
                HttpListenerContext ctx = await Listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;
                if ( req.Url == null )
                    continue;

                Files = new( Directory.GetFiles( FolderPath ) );
                string path = req.Url.AbsolutePath;
                if ( path == "/disconnect" ) {
                    res.StatusCode = 200;
                    res.Close();
                    break;
                } else if ( path == "/reload" ) {
                    OnExtensionReload?.Invoke();
                    res.StatusCode = 200;
                    res.Close();
                    break;
                }

                string fileName = CombinePaths( FolderPath, path.Split("/"));
                if ( !Files.Contains( fileName ) ) {
                    Logger.Warn( $"Request for {fileName} ignored: File doesn't exist" );
                    res.StatusCode = 404;
                    res.Close();
                    continue;
                }

                switch ( Path.GetExtension( fileName )[1..] ) {
                    case "js":
                        res.ContentType = "text/javascript";
                        break;
                    case "css":
                        res.ContentType = "text/css";
                        break;
                    default:
                        res.ContentType = "text/plain";
                        break;
                }

                res.ContentEncoding = Encoding.UTF8;
                byte[] data = File.ReadAllBytes( fileName );
                await res.OutputStream.WriteAsync( data, 0, data.Length );
                res.Close();
                Logger.Info( $"Sent file {fileName}" );
            }

            Logger.Info("Recieved disconnect request");
            Running = false;
        }

        private static string CombinePaths( string path, params string[] paths ) {
            if (paths == null) return path;
            return paths.Aggregate( path, ( acc, p ) => Path.Combine( acc, p ) );
        }

        public static int GetFirstAvailablePort( int startingPort ) {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // get active connections
            var tcpConnectionPorts = properties.GetActiveTcpConnections()
                .Where(n => n.LocalEndPoint.Port >= startingPort)
                .Select(n => n.LocalEndPoint.Port);

            // get active tcp listners
            var tcpListenerPorts = properties.GetActiveTcpListeners()
                .Where(n => n.Port >= startingPort)
                .Select(n => n.Port);

            // get active udp listeners
            var udpListenerPorts = properties.GetActiveUdpListeners()
                .Where(n => n.Port >= startingPort)
                .Select(n => n.Port);

            var port = Enumerable.Range(startingPort, ushort.MaxValue)
                .Where(i => !tcpConnectionPorts.Contains(i))
                .Where(i => !tcpListenerPorts.Contains(i))
                .Where(i => !udpListenerPorts.Contains(i))
                .FirstOrDefault();

            return port;
        }
    }
}
