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

        public List<OverwolfExtension> _Extensions = new();
        public List<OverwolfExtension> Extensions {
            get { return _Extensions; }
            set { _Extensions = value; OnPropertyChanged(); }
        }


        private Dictionary<string, bool> enabledExtensions = new();


        public MainWindow() {
#if (DEBUG)
            AllocConsole();
#endif
            InitializeComponent();

            // write config folder location in /bin
            File.WriteAllText( CONFIG_PATH_FILE, CONFIG_FOLDER );

            // get overwolf path
            if ( File.Exists(OVERWOLF_PATH_FILE)) OVERWOLF_FOLDER = File.ReadAllText(OVERWOLF_PATH_FILE);

            if ( !File.Exists( OVERWOLF_EXECUTABLE ) ) UpdateOverwolfPath();
            File.WriteAllText( OVERWOLF_PATH_FILE, OVERWOLF_FOLDER );

            // get all of the extensions 
            Extensions = new();
            string[] extensionPaths = Directory.GetDirectories(EXTENSION_FOLDER);
            foreach ( string extensionPath in extensionPaths )
                Extensions.Add( new OverwolfExtension( extensionPath ) );

            LoadEnabledExtensions();
            
            
            // create shortcut to use underwolf
            // create directory for all the js that needs to be injected
            // -> configure underwolf to look in this directory and inject and .js / .css files
            // allow the removal of underwolf

        }

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

        private void LoadEnabledExtensions() {
            if ( File.Exists( ENABLED_EXTENSIONS_FILE ) ) {
                try {
                    enabledExtensions = JsonSerializer.Deserialize<Dictionary<string, bool>>( File.ReadAllText( ENABLED_EXTENSIONS_FILE ) )!;

                    foreach ( string id in enabledExtensions.Keys ) {
                        bool state = enabledExtensions[id];
                        OverwolfExtension? ext = Extensions.Find( x => x.ExtensionID == id);
                        if ( ext == null )
                            enabledExtensions.Remove( id );
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
                enabledExtensions.Add( ext.ExtensionID, ext.Enabled );
            }
        }

        private void SaveEnabledExtensions() {
            foreach(OverwolfExtension ext in Extensions ) {
                if ( !ext.CanEnable ) continue;
                enabledExtensions[ext.ExtensionID] = ext.Enabled;
            }

            File.WriteAllText( ENABLED_EXTENSIONS_FILE, JsonSerializer.Serialize( enabledExtensions ) );
        }

        private void ExtensionControl_OnEnabledChanged( object s, RoutedEventArgs e ) {
            ExtensionControl sender = ( ExtensionControl )s;
            OverwolfExtension? ext = Extensions.Find( x => x.ExtensionID == sender.ExtensionID);
            if ( ext == null ) return;
            ext.Enabled = sender.Enabled; // this shouldnt be necessary 

            UpdateButtons();
        }

        private void Window_Closing( object sender, CancelEventArgs e ) {
            SaveEnabledExtensions();
        }

        private void UpdateButtons() {
            foreach (OverwolfExtension ext in Extensions ) {
                if (ext.IsStateChanged()) {
                    ApplyButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                    return;
                }
            }
            ApplyButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
        }

        private void ApplyButton_Click( object sender, RoutedEventArgs e ) {
            foreach (OverwolfExtension ext in Extensions ) {
                if ( ext.IsStateChanged() )
                    ext.ToggleUnderwolf();
            }

            UpdateButtons();
        }

        private void CancelButton_Click( object sender, RoutedEventArgs e ) {
            /*foreach ( OverwolfExtension ext in Extensions ) {
                ext.RevertEnabledState();
                Console.WriteLine( $"{ext.Title}: {ext.Enabled}" );
            }*/
            MessageBox.Show("Yeah this doesnt reall work yet.");
            UpdateButtons();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged( [CallerMemberName] string? name = null ) {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
        }
    }
}
