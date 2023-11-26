using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate.Dialogs
{
    public partial class AddOutpostDialog : Window
    {
        public AddOutpostDialog()
        {
            InitializeComponent();
            FocusManager.SetFocusedElement(this, outpostNameInput);
        }

        private void outpostNameInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && outpostNameInput.Text.Length > 0)
            {
                DialogResult = true;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (this.DialogResult == true)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void outpostNameInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            addButton.IsEnabled = outpostNameInput.Text.Length > 0;
        }
    }
}
