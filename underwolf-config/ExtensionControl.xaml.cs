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

        public OverwolfExtension Extension {
            get { return (OverwolfExtension)GetValue(ExtensionProperty); }
            set { SetValue(ExtensionProperty, value); }
        }
        public static readonly DependencyProperty ExtensionProperty = DependencyProperty.Register("Extension", typeof(OverwolfExtension), typeof(ExtensionControl), new PropertyMetadata(null, ExtensionChangedCallback));

        private static readonly Color ACTIVECOLOR = Color.FromRgb(74, 203, 138);
        private static readonly Color INACTIVECOLOR = Color.FromRgb(249, 67, 72);
        private static readonly SolidColorBrush ACTIVEBRUSH = new(ACTIVECOLOR);
        private static readonly SolidColorBrush INACTIVEBRUSH = new(INACTIVECOLOR);

        public ExtensionControl() {
            InitializeComponent();
        }

        /// <summary>
        /// Change the button text and colour when its enabled vs disabled
        /// </summary>
        private void UpdateAppearance() {
            if ( Extension.Enabled ) {
                Button.Background = ACTIVEBRUSH;
                Button.Content = "Disable";
            } else {
                Button.Background = INACTIVEBRUSH;
                Button.Content = "Enable";
            }
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
