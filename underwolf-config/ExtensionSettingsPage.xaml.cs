using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace underwolf_config {


    public partial class ExtensionSettingsPage : Page, INotifyPropertyChanged {

        public static readonly RoutedEvent OnBackEvent = EventManager.RegisterRoutedEvent( name: "OnBackEvent", routingStrategy: RoutingStrategy.Bubble, handlerType: typeof(RoutedEventHandler), ownerType: typeof(ExtensionSettingsPage));
        public event RoutedEventHandler OnBack {
            add { AddHandler(OnBackEvent, value); }
            remove { RemoveHandler(OnBackEvent, value); }
        }

        private OverwolfExtension _Extension;
        public OverwolfExtension Extension { get { return _Extension; } set { _Extension = value; OnPropertyChanged(); } }
       
        public ExtensionSettingsPage(OverwolfExtension extension) {
            InitializeComponent();
            Extension = extension;
        }

        private void OnOpenFolderClicked(object sender, RoutedEventArgs e) {
            if (Extension == null) return;
            Process.Start("explorer.exe", Extension.ConfigPath);
        }

        private void OnLaunchClicked(object sender, RoutedEventArgs e) {
            if (Extension == null) return;
            Process.Start(MainWindow.UNDERWOLF_EXECUTABLE, $"\"{Extension.Title}\" \"{Extension.ExtensionID}\" --keep-alive");
        }

        private void OnBackClicked(object sender, RoutedEventArgs e) {
            RoutedEventArgs args = new(routedEvent: OnBackEvent);
            RaiseEvent(args);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
