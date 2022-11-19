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
    public delegate void ExtensionDisconnect();
    public delegate void FilesChanged();

    internal class FileServer {

        public string Url;
        public string FolderPath;
        public event ExtensionReload? OnExtensionReload;
        public event ExtensionDisconnect? OnExtensionDisconnect;
        public event FilesChanged? OnFilesChanged;

        private static Dictionary<string, string> ContentTypes = new(){
            { "js", "text/javascript" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "jpg", "image/jpg" },
        };

        private HttpListener Listener;
        private Logger Logger = new("FileServer");
        private FileSystemWatcher Watcher;
        private Dictionary<string, string> Resources;
        private Dictionary<string, string> AdditionalResources;
        private Dictionary<string, Delegate> FunctionResources;
        private Dictionary<string, string> UnderwolfVariables;
        private bool Running;

        public FileServer(string path) {
            Url = $"http://localhost:{GetFirstAvailablePort(8000)}/";
            FolderPath = path;

            Listener = new();
            Listener.Prefixes.Add(Url);

            Watcher = new(FolderPath) {
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size
            };
            Watcher.IncludeSubdirectories = true;
            Watcher.EnableRaisingEvents = true;
            Watcher.Changed += (object sender, FileSystemEventArgs e) => OnFilesChanged?.Invoke(); // this runs twice...

            Resources = new();
            AdditionalResources = new();
            FunctionResources = new() {
                { "reload", () => OnExtensionReload?.Invoke() },
                { "disconnect", () => { OnExtensionDisconnect?.Invoke(); Running = false; } },
            };
            UnderwolfVariables = new();
        }

        public void Start(bool keepAlive = false) {
            Listener.Start();
            Running = true;
            Logger.Info($"Started listening at {Url}");
            Task.Run(HandleIncomingConnections);
        }

        public void Stop() {
            Logger.Info("Waiting for disconnect request...");
            while (Running) ;
            Logger.Info("Stopped");
            Listener.Stop();
        }

        public async Task HandleIncomingConnections() {
            while (Running) {
                HttpListenerContext ctx = await Listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;
                if (req.Url == null) continue;

                GetFiles();

                string path = req.Url.AbsolutePath[1..]; // remove leading slash
                Delegate? func;
                FunctionResources.TryGetValue(path, out func);
                if (func != null) {
                    func.DynamicInvoke();
                    res.StatusCode = 200;
                    res.Close();
                    continue;
                }

                string? file;
                Resources.TryGetValue(path, out file);
                if (file == null) {
                    Logger.Warn($"Request for {path} ignored: No endpoint exists");
                    res.StatusCode = 404;
                    res.Close();
                    continue;
                }

                string extension = Path.GetExtension( file )[1..];
                string? contentType;
                ContentTypes.TryGetValue(extension, out contentType);
                if (contentType == null) contentType = "text/plain";
                res.ContentType = contentType;

                string data = File.ReadAllText(file);
                data = ReplaceAllVariables(data);
                byte[] bytes = Encoding.UTF8.GetBytes(data);

                await res.OutputStream.WriteAsync(bytes);
                res.Close();
                Logger.Info($"Sent file {path}");
            }

            Logger.Info("Stopped Listening");
            Running = false;
        }

        private string ReplaceAllVariables(string data) {
            foreach (string key in UnderwolfVariables.Keys) {
                data = data.Replace(key, UnderwolfVariables[key]);
            }
            return data;
        }

        public void AddVariable(string name, string value) {
            UnderwolfVariables.TryAdd(name, value);
        }

        public void RemoveVariable(string name) {
            if (UnderwolfVariables.ContainsKey(name)) UnderwolfVariables.Remove(name);
        }

        public void AddResource(string path, string file) {
            AdditionalResources.TryAdd(path, file);
        }

        public void RemoveResource(string path) {
            if (AdditionalResources.ContainsKey(path)) AdditionalResources.Remove(path);
        }

        private void GetFiles() {
            Resources.Clear();
            List<string> files = new(Directory.GetFiles(FolderPath));
            foreach (string file in files) {
                string fileName = Path.GetFileName(file);
                Resources.Add(fileName, file);
            }
            Resources = Resources.Concat(AdditionalResources).ToDictionary(x => x.Key, x => x.Value);
        }

        private static string CombinePaths(string path, params string[] paths) {
            if (paths == null) return path;
            return paths.Aggregate(path, (acc, p) => Path.Combine(acc, p));
        }

        public static int GetFirstAvailablePort(int startingPort) {
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
