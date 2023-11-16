using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class LifeformEditor : Window
    {
        public string LifeformTypeString { get; set; }

        private Fauna originalFauna;
        private Flora originalFlora;
        private Dictionary<string, string> lifeformNames;
        private string? matchedNameString;

        public LifeformEditor(Fauna fauna, List<Resource> resources, Dictionary<string, string> lifeformNames)
        {
            InitializeComponent();

            originalFauna = fauna;
            this.lifeformNames = lifeformNames;

            this.Title = "Edit Fauna";
            lifeformNameTitle.Content = "Fauna Name 🛈";

            LifeformTypeString = "Fauna";
            DataContext = this;

            lifeformNameTextbox.Text = fauna.FaunaName;

            var resourcesToDisplay = resources.OrderBy(r => r.FullName).ToList();
            resourcesToDisplay.Insert(0, new Resource(-1, ResourceType.Organic, "Unknown", null, Rarity.Common));
            lifeformResourceComboBox.ItemsSource = resourcesToDisplay;

            if (fauna.IsSurveyed)
            {
                var resourceDrop = fauna.PrimaryDrops[0];
                var index = resourcesToDisplay.IndexOf(resourceDrop);
                lifeformResourceComboBox.SelectedIndex = index;
            }
            else
            {
                lifeformResourceComboBox.SelectedIndex = 0;
            }

            if (fauna.FaunaNotes != null)
            {
                lifeformNotesTextbox.Text = fauna.FaunaNotes;
            }
        }

        public LifeformEditor(Flora flora, List<Resource> resources, Dictionary<string, string> lifeformNames)
        {
            InitializeComponent();

            originalFlora = flora;
            this.lifeformNames = lifeformNames;

            this.Title = "Edit Flora";
            lifeformNameTitle.Content = "Flora Name 🛈";

            LifeformTypeString = "Flora";
            DataContext = this;

            lifeformNameTextbox.Text = flora.FloraName;

            var resourcesToDisplay = resources.OrderBy(r => r.FullName).ToList();
            resourcesToDisplay.Insert(0, new Resource(-1, ResourceType.Organic, "Unknown", null, Rarity.Common));
            lifeformResourceComboBox.ItemsSource = resourcesToDisplay;

            if (flora.IsSurveyed)
            {
                var resourceDrop = flora.PrimaryDrops[0];
                var index = resourcesToDisplay.IndexOf(resourceDrop);
                lifeformResourceComboBox.SelectedIndex = index;
            }
            else
            {
                lifeformResourceComboBox.SelectedIndex = 0;
            }

            if (flora.FloraNotes != null)
            {
                lifeformNotesTextbox.Text = flora.FloraNotes;
            }
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

        public Fauna GetResultingFauna()
        {
            var resultingFauna = originalFauna.DeepCopy();
            resultingFauna.FaunaName = lifeformNameTextbox.Text;
            resultingFauna.FaunaNotes = lifeformNotesTextbox.Text;

            if (lifeformResourceComboBox.SelectedIndex != 0)
            {
                resultingFauna.PrimaryDrops = new List<Resource> { lifeformResourceComboBox.SelectedItem as Resource };
            }
            else
            {
                resultingFauna.PrimaryDrops = null;
            }

            return resultingFauna;
        }

        public Flora GetResultingFlora()
        {
            var resultingFlora = originalFlora.DeepCopy();
            resultingFlora.FloraName = lifeformNameTextbox.Text;
            resultingFlora.FloraNotes = lifeformNotesTextbox.Text;

            if (lifeformResourceComboBox.SelectedIndex != 0)
            {
                resultingFlora.PrimaryDrops = new List<Resource> { lifeformResourceComboBox.SelectedItem as Resource };
            }
            else
            {
                resultingFlora.PrimaryDrops = null;
            }

            return resultingFlora;
        }

        private void lifeformNameTextboxChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMatchIndicatorVisibility();
            SaveButton.IsEnabled = lifeformNameTextbox.Text.Length > 0;
        }

        private void UpdateMatchIndicatorVisibility()
        {
            if (matchIndicatorLabel == null) { return; } // wait for UI to load

            var matchedNames = lifeformNames.Where(pair => pair.Key.StartsWith(lifeformNameTextbox.Text.ToLower()));

            // present suggestion if exactly 1 lifeform name is matched
            if (matchedNames.Count() == 1)
            {
                var matchedName = matchedNames.First();
                matchedNameString = matchedName.Value;

                // restore capitalized version
                if (!matchedName.Value.StartsWith(lifeformNameTextbox.Text))
                {
                    lifeformNameTextbox.Text = matchedName.Value.Substring(0, lifeformNameTextbox.Text.Length);
                    lifeformNameTextbox.SelectionStart = lifeformNameTextbox.Text.Length;
                }

                matchIndicatorLabel.Visibility = Visibility.Visible;
                lifeformNameHint.Content = matchedName.Value;
                lifeformNameHint.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
                lifeformNameHint.Visibility = Visibility.Hidden;
                matchedNameString = null;
            }
        }

        private void lifeformNameTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (matchedNameString != null && matchedNameString != lifeformNameTextbox.Text)
                {
                    lifeformNameTextbox.Text = matchedNameString;
                    lifeformNameTextbox.SelectionStart = lifeformNameTextbox.Text.Length;
                    e.Handled = true;
                }
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

        private void lifeformResourceComboBoxClicked(object sender, MouseButtonEventArgs e)
        {
            App.Current.PlayClickSound();
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
    }
}
