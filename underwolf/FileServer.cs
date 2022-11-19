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
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            Watcher.Changed += (object sender, FileSystemEventArgs e) => OnFilesChanged?.Invoke(); // this runs twice...

            Resources = new();
            AdditionalResources = new();
            FunctionResources = new() {
                { "reload", () => OnExtensionReload?.Invoke() },
                { "disconnect", () => { OnExtensionDisconnect?.Invoke(); Running = false; } },
            };
            UnderwolfVariables = new();
        }

        /// <summary>
        /// Starts the file server
        /// </summary>
        public void Start() {
            Listener.Start();
            Running = true;
            Logger.Info($"Started listening at {Url}");
            Task.Run(HandleIncomingConnections);
        }

        /// <summary>
        /// Stops the file server
        /// </summary>
        public void Stop() {
            Logger.Info("Waiting for disconnect request...");
            while (Running) ;
            Logger.Info("Stopped");
            Listener.Stop();
        }

        /// <summary>
        /// Processes any incoming connections
        /// </summary>
        private async Task HandleIncomingConnections() {
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

                Resources.TryGetValue(path, out string? file);
                if (file == null) {
                    Logger.Warn($"Request for {path} ignored: No endpoint exists");
                    res.StatusCode = 404;
                    res.Close();
                    continue;
                }

                string extension = Path.GetExtension( file )[1..];
                ContentTypes.TryGetValue(extension, out string? contentType);
                res.ContentType = contentType ?? "text/plain";

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

        /// <summary>
        /// Adds a varaible to replace in the content of files sent
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <param name="value">value to replace with</param>
        public void AddVariable(string name, string value) {
            UnderwolfVariables.TryAdd(name, value);
        }

        /// <summary>
        /// Removes a varaible
        /// </summary>
        /// <param name="name">name of the variable</param>
        public void RemoveVariable(string name) {
            if (UnderwolfVariables.ContainsKey(name)) UnderwolfVariables.Remove(name);
        }
        
        /// <summary>
        /// Replaces all variables with their values
        /// </summary>
        /// <param name="data">input data</param>
        /// <returns>returns data with all the variables replaced</returns>
        private string ReplaceAllVariables(string data) {
            foreach (string key in UnderwolfVariables.Keys) data = data.Replace(key, UnderwolfVariables[key]);
            return data;
        }

        /// <summary>
        /// Adds a resource that isn't in the FolderPath
        /// </summary>
        /// <param name="path">resource path</param>
        /// <param name="file">file path</param>
        public void AddResource(string path, string file) {
            AdditionalResources.TryAdd(path, file);
        }

        /// <summary>
        /// Removes a hosted resource
        /// </summary>
        /// <param name="path">path to resource</param>
        public void RemoveResource(string path) {
            if (AdditionalResources.ContainsKey(path)) AdditionalResources.Remove(path);
        }

        /// <summary>
        /// Add all the files from FolderPath into the Resources dictionary
        /// </summary>
        private void GetFiles() {
            Resources.Clear();
            List<string> files = new(Directory.GetFiles(FolderPath));
            foreach (string file in files) {
                string fileName = Path.GetFileName(file);
                Resources.Add(fileName, file);
            }
            Resources = Resources.Concat(AdditionalResources).ToDictionary(x => x.Key, x => x.Value);
        }

        // Credit to stackoverflow
        // Source: https://stackoverflow.com/a/45384984
        /// <summary>
        /// Gets the first port that is not in use
        /// </summary>
        /// <param name="startingPort">the lowest possible port to return</param>
        /// <returns>an avaliable port</returns>
        private static int GetFirstAvailablePort(int startingPort) {
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
