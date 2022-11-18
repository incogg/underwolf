using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace underwolf_config {

    public partial class MainWindow : Window, INotifyPropertyChanged {

#if (DEBUG)
        [DllImport( "kernel32" )]
        static extern int AllocConsole();
#endif

        public static string OVERWOLF_FOLDER = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Overwolf");
        public static string EXTENSION_FOLDER = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Overwolf", "Extensions");
        public static string START_MENU_FOLDER = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "Overwolf");
        public static string WORKING_FOLDER = Directory.GetCurrentDirectory();
        public static string CONFIG_FOLDER = Path.Join(WORKING_FOLDER , "config");
        public static string UNDERWOLF_FOLDER = Path.Join(WORKING_FOLDER, "bin");

        public static string OVERWOLF_EXECUTABLE = Path.Join( OVERWOLF_FOLDER, "OverwolfLauncher.exe" );
        public static string UNDERWOLF_EXECUTABLE = Path.Join(UNDERWOLF_FOLDER, "underwolf.exe");
        public static string CONFIG_PATH_FILE = Path.Join( UNDERWOLF_FOLDER, "config-path" );
        public static string OVERWOLF_PATH_FILE = Path.Join (UNDERWOLF_FOLDER, "underwolf-path");
        public static string ENABLED_EXTENSIONS_FILE = Path.Join(WORKING_FOLDER, "enabled-extensions.json");

        private Dictionary<string, bool> EnabledExtensions = new();
        private List<OverwolfExtension> _Extensions = new();
        public List<OverwolfExtension> Extensions {
            get { return _Extensions; }
            set { _Extensions = value; OnPropertyChanged(); }
        }

        public MainWindow() {
#if (DEBUG)
            AllocConsole();
#endif
            InitializeComponent();

            // write config folder location in /bin
            File.WriteAllText( CONFIG_PATH_FILE, CONFIG_FOLDER );

            // get overwolf path
            if ( File.Exists(OVERWOLF_PATH_FILE)) OVERWOLF_FOLDER = File.ReadAllText(OVERWOLF_PATH_FILE);

            // check if the overwolf path is correct 
            if ( !File.Exists( OVERWOLF_EXECUTABLE ) ) UpdateOverwolfPath();
            File.WriteAllText( OVERWOLF_PATH_FILE, OVERWOLF_FOLDER );

            // get all of the extensions 
            Extensions = new();
            string[] extensionPaths = Directory.GetDirectories(EXTENSION_FOLDER);
            foreach ( string extensionPath in extensionPaths )
                Extensions.Add( new OverwolfExtension( extensionPath ) );

            // check which extensions are currently enabled
            LoadEnabledExtensions();
        }

        /// <summary>
        /// Prompts the user to find the OverwolfLauncher executable
        /// </summary>
        private void UpdateOverwolfPath() {
            MessageBox.Show( "Couldn't locate Overwolf intall location. Please select the correct location." );
            OpenFileDialog ofd = new() {
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 ),
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "exe"
            };
            ofd.ShowDialog();

            while (Path.GetFileName(ofd.FileName) != "OverwolfLauncher.exe" ) {
                MessageBox.Show( $"Incorrect File {ofd.FileName}" );
                ofd.ShowDialog();
            }
            OVERWOLF_FOLDER = Path.GetDirectoryName( ofd.FileName )!;
        }

        /// <summary>
        /// Loads a list of enabled extensions from a JSON file
        /// </summary>
        private void LoadEnabledExtensions() {
            if ( File.Exists( ENABLED_EXTENSIONS_FILE ) ) {
                try {
                    EnabledExtensions = JsonSerializer.Deserialize<Dictionary<string, bool>>( File.ReadAllText( ENABLED_EXTENSIONS_FILE ) )!;

                    foreach ( string id in EnabledExtensions.Keys ) {
                        bool state = EnabledExtensions[id];
                        OverwolfExtension? ext = Extensions.Find( x => x.ExtensionID == id);
                        if ( ext == null )
                            EnabledExtensions.Remove( id );
                        else {
                            ext.Enabled = state;
                            ext.OldState = state;
                        }
                        
                    }
                    return;
                } catch {}
            }

            foreach ( OverwolfExtension ext in Extensions ) {
                if ( !ext.CanEnable ) continue;
                EnabledExtensions.Add( ext.ExtensionID, ext.Enabled );
            }
        }

        /// <summary>
        /// Writes a list of enabled extensions to a JSON file
        /// </summary>
        private void SaveEnabledExtensions() {
            foreach(OverwolfExtension ext in Extensions ) {
                if ( !ext.CanEnable ) continue;
                EnabledExtensions[ext.ExtensionID] = ext.Enabled;
            }
            File.WriteAllText( ENABLED_EXTENSIONS_FILE, JsonSerializer.Serialize( EnabledExtensions ) );
        }
        
        /// <summary>
        /// Applies the current states of the changed Extensions
        /// </summary>
        private void OnApplyClicked( object sender, RoutedEventArgs e ) {
            foreach (OverwolfExtension ext in Extensions ) if ( ext.IsStateChanged() ) ext.ToggleUnderwolf();
            UpdateButtons(sender, e);
        }

        /// <summary>
        /// Reverts the states of the changed extensions
        /// </summary>
        private void OnCancelClicked( object sender, RoutedEventArgs e ) {
            foreach (OverwolfExtension ext in Extensions) ext.RevertEnabledState();
            UpdateButtons(sender, e);
        }

        /// <summary>
        /// Enables the Cancel and Save buttons if there is a pending state change
        /// </summary>
        private void UpdateButtons(object s, RoutedEventArgs e) {
            foreach (OverwolfExtension ext in Extensions) {
                if (ext.IsStateChanged()) {
                    ApplyButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                    return;
                }
            }
            ApplyButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }

        /// <summary>
        /// Saves the Enabled extensions to a JSON file before the window is closed
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e) {
            SaveEnabledExtensions();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged( [CallerMemberName] string? name = null ) {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
        }
    }
}
