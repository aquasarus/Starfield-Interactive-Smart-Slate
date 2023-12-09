using Starfield_Interactive_Smart_Slate.Models.Entities;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class AddLifeformDialog : Window
    {
        private MainViewModel mainViewModel = MainViewModel.Instance;

        public string LifeformTypeString { get; set; }

        private string? matchedNameString;
        private LifeformType lifeformType;

        public AddLifeformDialog(LifeformType lifeformType)
        {
            this.lifeformType = lifeformType;
            LifeformTypeString = lifeformType.ToString();
            InitializeComponent();
            FocusManager.SetFocusedElement(this, lifeformNameInput);

            // skipping a view model here for now since it's one simple binding
            DataContext = this;
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

            var lifeformNames = mainViewModel.GetLifeformNames(lifeformType);
            var matchedNames = lifeformNames.Where(pair => pair.Key.StartsWith(lifeformNameInput.Text.ToLower()));

            // present suggestion if exactly 1 lifeform name is matched
            if (matchedNames.Count() == 1)
            {
                matchedNameString = matchedNames.First().Value;
            }
            else if (matchedNames.Any(name => name.Key == lifeformNameInput.Text.ToLower()))
            {
                matchedNameString = matchedNames.First(name => name.Key == lifeformNameInput.Text.ToLower()).Value;
            }
            else
            {
                matchedNameString = null;
            }

            if (matchedNameString != null)
            {
                // restore capitalized version
                if (!matchedNameString.StartsWith(lifeformNameInput.Text))
                {
                    lifeformNameInput.Text = matchedNameString.Substring(0, lifeformNameInput.Text.Length);
                    lifeformNameInput.SelectionStart = lifeformNameInput.Text.Length;
                }

                matchIndicatorLabel.Visibility = Visibility.Visible;
                lifeformNameInputHint.Content = matchedNameString;
                lifeformNameInputHint.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
                lifeformNameInputHint.Visibility = Visibility.Hidden;
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
