using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
    public partial class ExtensionControl : UserControl, INotifyPropertyChanged {

        public static readonly RoutedEvent OnEnabledChangedEvent = EventManager.RegisterRoutedEvent(
            name: "OnEnabledChanged",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(ExtensionControl));

        public event RoutedEventHandler OnEnabledChanged {
            add { AddHandler(OnEnabledChangedEvent, value); }
            remove { RemoveHandler(OnEnabledChangedEvent, value); }
        }

        public bool Enabled {
            get { return (bool)GetValue( EnabledProperty ); }
            set { SetValue( EnabledProperty, value ); OnPropertyChanged(); }
        }

        // Using a DependencyProperty as the backing store for Enabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.Register("Enabled", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(false, EnabledChangedCallback));

        public bool CanEnable {
            get { return (bool)GetValue( CanEnableProperty ); }
            set { SetValue( CanEnableProperty, value ); }
        }

        // Using a DependencyProperty as the backing store for CanEnable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanEnableProperty =
            DependencyProperty.Register("CanEnable", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(true));

        public string Title {
            get { return (string)GetValue( TitleProperty ); }
            set { SetValue( TitleProperty, value ); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Title"));

        public string ExtensionID {
            get { return (string)GetValue( ExtensionIDProperty ); }
            set { SetValue( ExtensionIDProperty, value ); }
        }

        // Using a DependencyProperty as the backing store for ExtensionID.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExtensionIDProperty =
            DependencyProperty.Register("ExtensionID", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Extension ID"));

        public string IconPngPath {
            get { return (string)GetValue( IconPngPathProperty ); }
            set { SetValue( IconPngPathProperty, value ); }
        }

        // Using a DependencyProperty as the backing store for IconPngPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconPngPathProperty =
            DependencyProperty.Register("IconPngPath", typeof(string), typeof(ExtensionControl), new PropertyMetadata(string.Empty, IconPngPathChangedCallback));

        private static readonly Color ACTIVECOLOR = Color.FromRgb(74, 203, 138);
        private static readonly SolidColorBrush ACTIVEBRUSH = new SolidColorBrush(ACTIVECOLOR);

        private static readonly Color INACTIVECOLOR = Color.FromRgb(249, 67, 72);
        private static readonly SolidColorBrush INACTIVEBRUSH = new SolidColorBrush(INACTIVECOLOR);

        public ExtensionControl() {
            InitializeComponent();
        }

        private static void EnabledChangedCallback( DependencyObject sender, DependencyPropertyChangedEventArgs e ) {
            ExtensionControl? ext = sender as ExtensionControl;
            if ( ext == null )
                return;
            ext.EnabledChanged();
        }

        private void EnabledChanged() {
            UpdateAppearance();

            RoutedEventArgs args = new(routedEvent: OnEnabledChangedEvent);
            RaiseEvent( args );
        }

        private static void IconPngPathChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            ExtensionControl? ext = sender as ExtensionControl;
            if ( ext == null ) return;
            ext.OnIconPngPathChanged();
        }

        private void OnIconPngPathChanged() {
            Image.Source = new BitmapImage( new Uri( IconPngPath ) );
        }

        private void UpdateAppearance() {
            if ( Enabled ) {
                Button.Background = ACTIVEBRUSH;
                Button.Content = "Disable";
            } else {
                Button.Background = INACTIVEBRUSH;
                Button.Content = "Enable";
            }
        }

        private void OnClick( object sender, RoutedEventArgs e ) {
            Enabled = !Enabled;
            UpdateAppearance();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged( [CallerMemberName] string? name = null ) {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ) );
        }

    }
}
