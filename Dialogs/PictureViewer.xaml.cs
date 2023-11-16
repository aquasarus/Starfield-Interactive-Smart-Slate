using Starfield_Interactive_Smart_Slate.Models;
using System.Windows;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate.Dialogs
{
    public partial class PictureViewer : Window
    {
        private bool IsClosing = false;

        public PictureViewer(Picture picture)
        {
            InitializeComponent();
            this.DataContext = picture;
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosing = true;
            App.Current.PlayCancelSound();
        }
    }
}
