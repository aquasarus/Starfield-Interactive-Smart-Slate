using System.ComponentModel;
using System.Windows;

namespace Starfield_Interactive_Smart_Slate.Dialogs
{
    public partial class BasicYesNoDialog : Window
    {
        public bool ExplicitNo = false;

        public BasicYesNoDialog(string title, string body, string yesText, string? noText = null)
        {
            InitializeComponent();

            Title = title;
            MessageText.Text = body;
            YesButton.Content = yesText;

            if (noText != null)
            {
                NoButton.Content = noText;
                NoButton.Visibility = Visibility.Visible;
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            ExplicitNo = true;
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult == true)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }
    }
}
