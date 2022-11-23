using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace underwolf_config {
    public partial class ExtensionControl : UserControl, INotifyPropertyChanged {

        public static readonly RoutedEvent OnEnabledChangedEvent = EventManager.RegisterRoutedEvent( name: "OnEnabledChanged", routingStrategy: RoutingStrategy.Bubble, handlerType: typeof(RoutedEventHandler), ownerType: typeof(ExtensionControl));
        public event RoutedEventHandler OnEnabledChanged {
            add { AddHandler(OnEnabledChangedEvent, value); }
            remove { RemoveHandler(OnEnabledChangedEvent, value); }
        }

        public static readonly RoutedEvent OnOpenSettingsEvent = EventManager.RegisterRoutedEvent( name: "OnOpenSettings", routingStrategy: RoutingStrategy.Bubble, handlerType: typeof(RoutedEventHandler), ownerType: typeof(ExtensionControl));
        public event RoutedEventHandler OnOpenSettings {
            add { AddHandler(OnOpenSettingsEvent, value); }
            remove { RemoveHandler(OnOpenSettingsEvent, value); }
        }

        public OverwolfExtension Extension {
            get { return (OverwolfExtension)GetValue(ExtensionProperty); }
            set { SetValue(ExtensionProperty, value); }
        }
        public static readonly DependencyProperty ExtensionProperty = DependencyProperty.Register("Extension", typeof(OverwolfExtension), typeof(ExtensionControl), new PropertyMetadata(null, ExtensionChangedCallback));

        private static readonly Color ACTIVE_COLOUR = Color.FromRgb(74, 203, 138);
        private static readonly Color INACTIVE_COLOUR = Color.FromRgb(249, 67, 72);
        private static readonly SolidColorBrush ACTIVE_BRUSH = new(ACTIVE_COLOUR);
        private static readonly SolidColorBrush INACTIVE_BRUSH = new(INACTIVE_COLOUR);

        public ExtensionControl() {
            InitializeComponent();
        }

        /// <summary>
        /// Triggered once all the Dependency properties have been loaded
        /// </summary>
        private void OnLoad(object sender, RoutedEventArgs e) {
            if (!Extension.CanEnable) SettingsButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Change the button text and colour when its enabled vs disabled
        /// </summary>
        private void UpdateAppearance() {
            if ( Extension.Enabled ) {
                Button.Background = ACTIVE_BRUSH;
                Button.Content = "Disable";
            } else {
                Button.Background = INACTIVE_BRUSH;
                Button.Content = "Enable";
            }
        }

        /// <summary>
        /// Send and event to open the settings for the current extension
        /// </summary>
        private void OnSettingsClicked(object sender, RoutedEventArgs e) {
            RoutedEventArgs args = new(routedEvent: OnOpenSettingsEvent);
            RaiseEvent(args);
        }

        /// <summary>
        /// Toggle the state of Enabled when the button is clicked
        /// </summary>
        private void OnClick( object sender, RoutedEventArgs e ) {
            Extension.Enabled = !Extension.Enabled;
            UpdateAppearance();

            RoutedEventArgs args = new(routedEvent: OnEnabledChangedEvent);
            RaiseEvent(args);
        }

        /// <summary>
        /// Update the appearance of the button when the extension variable is changed
        /// </summary>
        private static void ExtensionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            ExtensionControl? ext = sender as ExtensionControl;
            if (ext == null) return;
            ext.UpdateAppearance();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
