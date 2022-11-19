using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace underwolf_config
{
    public class ExtensionManifest {
        public Dictionary<string, string> meta { get; set; }
    }


    public class OverwolfExtension: INotifyPropertyChanged {

        private static readonly List<string> EXCLUSIONS = new(){
            "Settings",
            "Achievement Rewards",
            "Launcher Events Provider",
            "Overwolf Support",
            "Overwolf General GameEvents Provider",
            "Achievement Rewards UI",
            "Refer a Friend",
            "OWOBS",
            "Overwolf Appstore",
            "Overwolf notifications",
            "Exclusive mode",
            "Overwolf Remote Configurations"
        };

        public bool OldState;
        private bool _Enabled;
        public bool Enabled { 
            get { return _Enabled; }
            set {  _Enabled = value; OnPropertyChanged(); }
        }
        public string Title { get; set; }
        public string ExtensionID { get; set; }
        public string IconPngPath { get; set; }
        public bool CanEnable { get; set; }


        public string ExtensionPath;
        public string ConfigPath;
        public Version LatestVersion;
        public string? IconIcoPath;

        public OverwolfExtension( string path ) { 
            ExtensionPath = path;
            ExtensionID = Path.GetFileName( path );

            // get the folder of the latest version
            LatestVersion = new Version( "0.0.0" );
            foreach ( string versionString in Directory.GetDirectories(ExtensionPath)) {
                Version version = new( Path.GetFileName( versionString ));
                if ( version > LatestVersion ) LatestVersion = version;
            }
            ExtensionPath = Path.Join( ExtensionPath, LatestVersion.ToString() );

            // read the extensions manifest
            string manifestString = File.ReadAllText(Path.Join(ExtensionPath, "manifest.json"));
            ExtensionManifest? manifest = JsonSerializer.Deserialize<ExtensionManifest>(manifestString);
            if ( manifest == null ) throw new Exception( "Extension has no manifest" );

            Title = manifest.meta["name"];
            IconIcoPath = Path.Join( ExtensionPath, manifest.meta.GetValueOrDefault("launcher_icon") );
            IconPngPath = Path.Join( ExtensionPath, manifest.meta.GetValueOrDefault("icon") );

            // dont allow user to enable underwolf for extensions like Overwolf Settings
            CanEnable = !EXCLUSIONS.Contains(Title);

            // create the config dir
            ConfigPath = Path.Join(MainWindow.CONFIG_FOLDER, ExtensionID);
            if (!Directory.Exists(ConfigPath) && CanEnable) Directory.CreateDirectory(ConfigPath);
        }

        /// <summary>
        /// Checks if the state of the extension has changed
        /// </summary>
        /// <returns>True if the state has changed otherwise false</returns>
        public bool IsStateChanged() {
            return Enabled != OldState;
        }

        /// <summary>
        /// Reverts the enabled state of the extension
        /// </summary>
        public void RevertEnabledState() {
            Enabled = OldState;
        }

        /// <summary>
        /// Toggles the enabled state of the extension
        /// </summary>
        public void ToggleUnderwolf() {
            if ( Enabled ) EnableUnderwolf();
            else DisableUnderwolf();
            OldState = Enabled;
        }

        /// <summary>
        /// Remakes extension's shortcut to point to Underwolf
        /// </summary>
        private void EnableUnderwolf() {
            string shortcutLocation = Path.Join( MainWindow.START_MENU_FOLDER, $"{Title}.lnk" );
            string iconLocation = IconIcoPath ?? string.Empty; 

            File.Delete( shortcutLocation );
            ShortcutBuilder.Build( shortcutLocation, MainWindow.UNDERWOLF_EXECUTABLE, $"\"{Title}\" \"{ExtensionID}\"", iconLocation );
        }

        /// <summary>
        /// Remakes extension's shortcut to point to Overwolf
        /// </summary>
        private void DisableUnderwolf() {
            string shortcutLocation = Path.Join( MainWindow.START_MENU_FOLDER, $"{Title}.lnk" );
            string iconLocation = IconIcoPath ?? string.Empty;

            File.Delete(shortcutLocation);
            ShortcutBuilder.Build( shortcutLocation, MainWindow.OVERWOLF_EXECUTABLE, $"-launchapp {ExtensionID} -from-desktop", iconLocation );
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged( [CallerMemberName] string? name = null ) {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
        }
    }
}
