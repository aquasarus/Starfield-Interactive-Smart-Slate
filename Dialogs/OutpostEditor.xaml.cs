using Starfield_Interactive_Smart_Slate.Models.Entities;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate.Dialogs
{
    public partial class OutpostEditor : Window
    {
        private Outpost originalOutpost;

        public OutpostEditor(Outpost outpost)
        {
            InitializeComponent();

            originalOutpost = outpost;
            DataContext = this;

            outpostNameTextbox.Text = outpost.Name;

            if (outpost.Notes != null)
            {
                outpostNotesTextbox.Text = outpost.Notes;
            }
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DialogResult = true;
                Close();
                e.Handled = true;
            }
        }

        private void outpostNameTextboxChanged(object sender, TextChangedEventArgs e)
        {
            SaveButton.IsEnabled = outpostNameTextbox.Text.Length > 0;
        }

        public void SaveClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        public void CancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public Outpost GetResultingOutpost()
        {
            var resultingOutpost = originalOutpost.DeepCopy();
            resultingOutpost.Name = outpostNameTextbox.Text;
            resultingOutpost.Notes = outpostNotesTextbox.Text;
            return resultingOutpost;
        }
    }
}
