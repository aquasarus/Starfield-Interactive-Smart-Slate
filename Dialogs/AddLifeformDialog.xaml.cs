using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class AddLifeformDialog : Window
    {
        public string LifeformTypeString { get; set; }

        private Dictionary<string, string> lifeformNames;

        public AddLifeformDialog(Dictionary<string, string> lifeformNames, LifeformType lifeformType)
        {
            this.lifeformNames = lifeformNames;
            this.LifeformTypeString = lifeformType.ToString();
            InitializeComponent();
            FocusManager.SetFocusedElement(this, lifeformNameInput);
            DataContext = this;
        }

        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
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
            if (matchIndicatorLabel == null) { return; }

            if (lifeformNames.ContainsKey(lifeformNameInput.Text.ToLower()))
            {
                // restore capitalized version if applicable
                if (lifeformNames[lifeformNameInput.Text.ToLower()] != lifeformNameInput.Text)
                {
                    lifeformNameInput.Text = lifeformNames[lifeformNameInput.Text.ToLower()];
                    lifeformNameInput.SelectionStart = lifeformNameInput.Text.Length;
                }

                matchIndicatorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
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
        }
    }
}
