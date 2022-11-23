using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class ListPage : Page, INotifyPropertyChanged {

        public delegate void OpenSettingsHandler(object sender, OverwolfExtension extension);
        public event OpenSettingsHandler? OnOpenSettings;

        private List<OverwolfExtension> _Extensions = new();
        public List<OverwolfExtension> Extensions {
            get { return _Extensions; }
            set { _Extensions = value; OnPropertyChanged(); }
        }
        
        public ListPage(List<OverwolfExtension> extensions) {
            InitializeComponent();
            Extensions = extensions;
        }

        /// <summary>
        /// Raises an event to open the settings page for a given extension
        /// </summary>
        private void OpenSettings(object sender, RoutedEventArgs e) {
            OnOpenSettings?.Invoke(this, ((ExtensionControl)sender).Extension);
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
        /// Applies the current states of the changed Extensions
        /// </summary>
        private void OnApplyClicked(object sender, RoutedEventArgs e) {
            foreach (OverwolfExtension ext in Extensions) if (ext.IsStateChanged()) ext.ToggleUnderwolf();
            UpdateButtons(sender, e);
        }

        /// <summary>
        /// Reverts the states of the changed extensions
        /// </summary>
        private void OnCancelClicked(object sender, RoutedEventArgs e) {
            foreach (OverwolfExtension ext in Extensions) ext.RevertEnabledState();
            UpdateButtons(sender, e);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
