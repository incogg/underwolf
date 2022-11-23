using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace underwolf {
    class Program {

#if (DEBUG)
        [DllImport( "kernel32" )]
        static extern int AllocConsole();
#endif

        public static readonly string DEBUGGER_URL = "http://localhost:54284/json";
        public static readonly int APPLICATION_TIMEOUT = 20; // in seconds

        public static string WORKING_FOLDER = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!;
        public static string CONFIG_PATH_FILE = Path.Join( WORKING_FOLDER, "config-path" );
        public static string OVERWOLF_PATH_FILE = Path.Join (WORKING_FOLDER, "underwolf-path");
        public static string CONFIG_PATH = "";
        public static string OVERWOLF_PATH = "";

        private static readonly Logger Logger = new("Main");

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">should be in format {appTitle} {appID} [keepAlive]</param>
        static async Task Main( string[] args ) {
#if (DEBUG)
            AllocConsole();
#endif

            Console.Title = "Underwolf";

            if (args.Length < 2 ) {
                Logger.Error( "Invalid arguments provided" );
                goto end;
            }

            string appTitle = args[0];
            string appID = args[1];
            bool keepAlive = false;

            foreach (string arg in args) if (arg == "--keep-alive") keepAlive = true;

            if ( !ReadFile( CONFIG_PATH_FILE, out CONFIG_PATH ) ) goto end;
            if ( !ReadFile( OVERWOLF_PATH_FILE, out OVERWOLF_PATH ) ) goto end;

            if ( !StartApplication( appID, appTitle ) ) goto end;

            ExtensionJSON? curseJson = await GetExtension(appTitle);
            if ( curseJson == null || curseJson.IsInvalid() ) {
                Logger.Error( $"Couldn't find extension {appTitle}. Is the application running?" );
                goto end;
            }

            OverwolfExtension curse = new(curseJson, appID, keepAlive);
            await curse.Connect();
            curse.InjectAllFiles();

end:
#if (DEBUG)
            Console.ReadKey();
#endif
        }

        /// <summary>
        /// Reads a file
        /// </summary>
        /// <param name="file">the location of the file</param>
        /// <param name="contents">contains the output of the file if the file exists</param>
        /// <returns>true if the file exists else false</returns>
        static bool ReadFile( string file, out string contents ) {
            if ( !File.Exists( file ) ) {
                Logger.Error( $"Can't find file: {file}" );
                contents = string.Empty;
                return false;
            }
            contents = File.ReadAllText( file );
            return true;
        }

        /// <summary>
        /// Runs an Overwolf Extension
        /// </summary>
        /// <param name="appID">The ID of the extension to run</param>
        /// <param name="appTitle">The Title of the extension to run</param>
        /// <returns>true if the extension started and was found otherwise false</returns>
        static bool StartApplication( string appID, string appTitle ) {
            Process.Start( Path.Join( OVERWOLF_PATH, "OverwolfLauncher.exe" ), $"-launchapp {appID} -from-desktop" );

            Task waitTask = Task.Run(() => {
                Logger.Info($"Waiting for {appTitle} to start...");
                while ( true ) {
                    Process[] processlist = Process.GetProcesses();
                    foreach ( Process process in processlist ) {
                        if ( process.MainWindowTitle == appTitle ){
                            Logger.Info($"{appTitle} found!");
                            return;
                        }
                    }
                    Thread.Sleep( 100 );
                }
            });
            if ( !waitTask.Wait( APPLICATION_TIMEOUT * 1000 ) /* convert seconds to milliseconds */ ) {
                Logger.Error( $"Failed to start the application in time" );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Makes a request to the Overwolf Dev port to find an Extension
        /// </summary>
        /// <param name="appTitle">The title of the extension</param>
        /// <returns>a json object of the Overwolf extension or null if none was found</returns>
        static async Task<ExtensionJSON?> GetExtension( string appTitle ) {

            string response;
            try {
                HttpClient client = new();
                response = await client.GetStringAsync( DEBUGGER_URL );
            } catch ( Exception ) {
                Logger.Error( "Failed to get a response from overwolf remote debugger" );
                return null;
            }

            List<ExtensionJSON>? list = JsonSerializer.Deserialize<List<ExtensionJSON>>(response);
            if ( list == null ) {
                Logger.Error( "Failed to get a valid response from overwolf remote debugger" );
                return null;
            }
            return list.Find( x => x.title == appTitle );
        }
    }
}