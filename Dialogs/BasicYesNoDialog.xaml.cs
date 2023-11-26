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
            App.Current.PlayClickSound();
            DialogResult = true;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.PlayCancelSound();
            ExplicitNo = true;
            Close();
        }
    }
}
