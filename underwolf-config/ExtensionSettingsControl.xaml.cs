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
    public partial class ExtensionSettingsControl : UserControl {


        public bool Enabled {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); OnPropertyChanged(); }
        }
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(false));

        public bool CanEnable {
            get { return (bool)GetValue(CanEnableProperty); }
            set { SetValue(CanEnableProperty, value); }
        }
        public static readonly DependencyProperty CanEnableProperty = DependencyProperty.Register("CanEnable", typeof(bool), typeof(ExtensionControl), new PropertyMetadata(true));

        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Title"));

        public string ExtensionID {
            get { return (string)GetValue(ExtensionIDProperty); }
            set { SetValue(ExtensionIDProperty, value); }
        }
        public static readonly DependencyProperty ExtensionIDProperty = DependencyProperty.Register("ExtensionID", typeof(string), typeof(ExtensionControl), new PropertyMetadata("Extension ID"));

        public string IconPngPath {
            get { return (string)GetValue(IconPngPathProperty); }
            set { SetValue(IconPngPathProperty, value); }
        }
        public static readonly DependencyProperty IconPngPathProperty = DependencyProperty.Register("IconPngPath", typeof(string), typeof(ExtensionControl), new PropertyMetadata(string.Empty));

        public string ExtensionConfigPath {
            get { return (string)GetValue(ExtensionConfigPathProperty); }
            set { SetValue(ExtensionConfigPathProperty, value); }
        }
        public static readonly DependencyProperty ExtensionConfigPathProperty = DependencyProperty.Register("ExtensionConfigPath", typeof(string), typeof(ExtensionSettingsControl), new PropertyMetadata(null));




        public ExtensionSettingsControl() {
            InitializeComponent();
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
