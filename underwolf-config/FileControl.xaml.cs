using System;
using System.IO;
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
using System.Diagnostics;

namespace underwolf_config {
    public partial class FileControl : UserControl, INotifyPropertyChanged {
        public string FilePath {
            get { return (string)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); OnPropertyChanged(); }
        }
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register("FilePath", typeof(string), typeof(FileControl), new PropertyMetadata(""));

        private string  _FileName;
        public string FileName { get { return _FileName; } set { _FileName = value; OnPropertyChanged(); } }

        private bool _Enabled;
        public bool Enabled { get { return _Enabled; } set { _Enabled = value; OnPropertyChanged(); } }

        public static readonly Color ENABLED_COLOUR = Color.FromArgb(255, 255, 255, 255);
        public static readonly Color DISABLED_COLOUR = Color.FromArgb(255, 61, 61, 61);
        private static readonly SolidColorBrush ENABLED_BRUSH = new(ENABLED_COLOUR);
        private static readonly SolidColorBrush DISABLED_BRUSH = new(DISABLED_COLOUR);
        public FileControl() {
            InitializeComponent();
        }

        private void OnLoad(object sender, RoutedEventArgs e) {
            Enabled = Path.GetExtension(FilePath) != ".disabled";
            UpdateFileName();
        }

        private void UpdateFileName() {
            FileName = Enabled ? Path.GetFileName(FilePath) : Path.GetFileName(FilePath).Replace(".disabled", "");
            TextBlock.Foreground = Enabled ? ENABLED_BRUSH : DISABLED_BRUSH;
        }

        private void ToggleEnabled() {
            string newPath = !Enabled ? FilePath + ".disabled" : Path.ChangeExtension(FilePath, "")[0..^1];
            File.Move(FilePath, newPath);
            FilePath = newPath;
            UpdateFileName();
        }

        private void CheckBoxClicked(object sender, RoutedEventArgs e) {
            ToggleEnabled();
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
