using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        private string _Title;
        public string Title { get { return _Title; } set { _Title = value; OnPropertyChanged(); } }
        public string ExtensionID { get; set; }
        public string IconPngPath { get; set; }
        public string ExtensionPath { get; set; }
        public string ConfigPath { get; set; }
        public Version LatestVersion { get; set; }
        public string? IconIcoPath { get; set; }
        public bool CanEnable { get; set; }

        public OverwolfExtension( string path ) {
            ExtensionPath = path;
            ExtensionID = Path.GetFileName( path );

            LatestVersion = new Version( "0.0.0" );
            string[] versions = Directory.GetDirectories(ExtensionPath);
            foreach ( string versionString in versions ) {
                Version version = new( Path.GetFileName( versionString ));
                if ( version > LatestVersion )
                    LatestVersion = version;
            }

            ExtensionPath = Path.Join( ExtensionPath, LatestVersion.ToString() );

            string manifestString = File.ReadAllText(Path.Join(ExtensionPath, "manifest.json"));
            ExtensionManifest? manifest = JsonSerializer.Deserialize<ExtensionManifest>(manifestString);
            if ( manifest == null )
                throw new Exception( "Extension has no manifest" );

            Title = manifest.meta["name"];
            IconIcoPath = Path.Join( ExtensionPath, manifest.meta.GetValueOrDefault("launcher_icon") );
            IconPngPath = Path.Join( ExtensionPath, manifest.meta.GetValueOrDefault("icon") );

            CanEnable = !EXCLUSIONS.Contains(Title);
            // create the config dir
            ConfigPath = Path.Join(MainWindow.CONFIG_FOLDER, ExtensionID);
            if (!Directory.Exists(ConfigPath) && CanEnable)
                Directory.CreateDirectory(ConfigPath);
        }

        public bool IsStateChanged() {
            return Enabled != OldState;
        }

        public void RevertEnabledState() {
            Enabled = OldState;
        }

        public void ToggleUnderwolf() {
            if ( Enabled ) EnableUnderwolf();
            else DisableUnderwolf();
            OldState = Enabled;
        }

        private void EnableUnderwolf() {
            string shortcutLocation = Path.Join( MainWindow.START_MENU_FOLDER, $"{Title}.lnk" );
            string iconLocation = "";
            if ( IconIcoPath != null )
                iconLocation = IconIcoPath;

            File.Delete( shortcutLocation );
            ShortcutBuilder.Build( shortcutLocation, MainWindow.UNDERWOLF_EXECUTABLE, $"\"{Title}\" \"{ExtensionID}\"", iconLocation );
        }

        private void DisableUnderwolf() {
            string shortcutLocation = Path.Join( MainWindow.START_MENU_FOLDER, $"{Title}.lnk" );
            string iconLocation = "";
            if ( IconIcoPath != null ) iconLocation = IconIcoPath;

            File.Delete( shortcutLocation );
            ShortcutBuilder.Build( shortcutLocation, MainWindow.OVERWOLF_EXECUTABLE, $"-launchapp {ExtensionID} -from-desktop", iconLocation );
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged( [CallerMemberName] string? name = null ) {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
        }
    }
}
