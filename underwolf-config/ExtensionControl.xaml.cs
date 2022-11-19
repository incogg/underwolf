using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace underwolf_config {
    public partial class ExtensionControl : UserControl, INotifyPropertyChanged {

        public static readonly RoutedEvent OnEnabledChangedEvent = EventManager.RegisterRoutedEvent( name: "OnEnabledChanged", routingStrategy: RoutingStrategy.Bubble, handlerType: typeof(RoutedEventHandler), ownerType: typeof(ExtensionControl));
        public event RoutedEventHandler OnEnabledChanged {
            add { AddHandler(OnEnabledChangedEvent, value); }
            remove { RemoveHandler(OnEnabledChangedEvent, value); }
        }

        public bool Enabled {
            get { return (bool)GetValue( EnabledProperty ); }
            set { SetValue( EnabledProperty, value ); OnPropertyChanged(); }
        }
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(false, EnabledChangedCallback));

        public bool CanEnable {
            get { return (bool)GetValue( CanEnableProperty ); }
            set { SetValue( CanEnableProperty, value ); }
        }
        public static readonly DependencyProperty CanEnableProperty = DependencyProperty.Register("CanEnable", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(true));

        public string Title {
            get { return (string)GetValue( TitleProperty ); }
            set { SetValue( TitleProperty, value ); }
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Title"));

        public string ExtensionID {
            get { return (string)GetValue(ExtensionIDProperty); }
            set { SetValue(ExtensionIDProperty, value); }
        }
        public static readonly DependencyProperty ExtensionIDProperty = DependencyProperty.Register("ExtensionID", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Extension ID"));

        public string IconPngPath {
            get { return (string)GetValue( IconPngPathProperty ); }
            set { SetValue( IconPngPathProperty, value ); }
        }
        public static readonly DependencyProperty IconPngPathProperty = DependencyProperty.Register("IconPngPath", typeof(string), typeof(ExtensionControl), new PropertyMetadata(string.Empty, IconPngPathChangedCallback));

        private static readonly Color ACTIVECOLOR = Color.FromRgb(74, 203, 138);
        private static readonly Color INACTIVECOLOR = Color.FromRgb(249, 67, 72);
        private static readonly SolidColorBrush ACTIVEBRUSH = new(ACTIVECOLOR);
        private static readonly SolidColorBrush INACTIVEBRUSH = new(INACTIVECOLOR);

        public ExtensionControl() {
            InitializeComponent();
        }

        
        /// <summary>
        /// Triggers an event when Enabled is changed
        /// </summary>
        private void EnabledChanged() {
            UpdateAppearance();

            RoutedEventArgs args = new(routedEvent: OnEnabledChangedEvent);
            RaiseEvent( args );
        }

        /// <summary>
        /// Update the image when the path is changed
        /// </summary>
        private void OnIconPngPathChanged() {
            Image.Source = new BitmapImage( new Uri( IconPngPath ) );
        }

        /// <summary>
        /// Change the button text and colour when its enabled vs disabled
        /// </summary>
        private void UpdateAppearance() {
            if ( Enabled ) {
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
            Enabled = !Enabled;
            UpdateAppearance();
        }

        /// <summary>
        /// Static callback to call EnabledChanged()
        /// </summary>
        private static void EnabledChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            ExtensionControl? ext = sender as ExtensionControl;
            if (ext == null)
                return;
            ext.EnabledChanged();
        }

        /// <summary>
        /// Static callback to call OnIconPngPathChanged()
        /// </summary>
        private static void IconPngPathChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            ExtensionControl? ext = sender as ExtensionControl;
            if (ext == null) return;
            ext.OnIconPngPathChanged();
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
