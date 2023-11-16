using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class AddLifeformDialog : Window
    {
        public string LifeformTypeString { get; set; }

        private Dictionary<string, string> lifeformNames;
        private string? matchedNameString;

        public AddLifeformDialog(Dictionary<string, string> lifeformNames, LifeformType lifeformType)
        {
            this.lifeformNames = lifeformNames;
            this.LifeformTypeString = lifeformType.ToString();
            InitializeComponent();
            FocusManager.SetFocusedElement(this, lifeformNameInput);
            DataContext = this;
            lifeformNameInput.DataContext = this;
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void lifeformNameInputChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMatchIndicatorVisibility();
            addLifeformButton.IsEnabled = lifeformNameInput.Text.Length > 0;
        }

        private void UpdateMatchIndicatorVisibility()
        {
            if (matchIndicatorLabel == null) { return; } // wait for UI to load

            var matchedNames = lifeformNames.Where(pair => pair.Key.StartsWith(lifeformNameInput.Text.ToLower()));

            // present suggestion if exactly 1 lifeform name is matched
            if (matchedNames.Count() == 1)
            {
                var matchedName = matchedNames.First();
                matchedNameString = matchedName.Value;

                // restore capitalized version
                if (!matchedName.Value.StartsWith(lifeformNameInput.Text))
                {
                    lifeformNameInput.Text = matchedName.Value.Substring(0, lifeformNameInput.Text.Length);
                    lifeformNameInput.SelectionStart = lifeformNameInput.Text.Length;
                }

                matchIndicatorLabel.Visibility = Visibility.Visible;
                lifeformNameInputHint.Content = matchedName.Value;
                lifeformNameInputHint.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
                lifeformNameInputHint.Visibility = Visibility.Hidden;
                matchedNameString = null;
            }
        }

        private void lifeformNameInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lifeformNameInput.Text.Length > 0)
            {
                DialogResult = true;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (matchedNameString != null && matchedNameString != lifeformNameInput.Text)
                {
                    lifeformNameInput.Text = matchedNameString;
                    lifeformNameInput.SelectionStart = lifeformNameInput.Text.Length;
                    e.Handled = true;
                }
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
    }
}
