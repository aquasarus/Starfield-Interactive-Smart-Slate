using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using Starfield_Interactive_Smart_Slate.Screens.PlanetaryData;
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
        private PlanetaryDataViewModel viewModel = PlanetaryDataViewModel.Instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;

        private LifeformEntity originalEntity;
        private string? matchedNameString;

        public LifeformEditor(LifeformEntity entity)
        {
            InitializeComponent();

            originalEntity = entity;

            if (entity is Fauna)
            {
                Title = "Edit Fauna";
                lifeformNameTitle.Content = "Fauna Name 🛈";
                lifeformNameTooltip.Content = "Fauna";
            }
            else
            {
                Title = "Edit Flora";
                lifeformNameTitle.Content = "Flora Name 🛈";
                lifeformNameTooltip.Content = "Flora";
            }

            lifeformNameTextbox.Text = entity.Name;

            if (entity.IsSurveyed)
            {
                var resourceDrop = entity.PrimaryDrops[0];
                var index = viewModel.SelectableOrganicResources.IndexOf(resourceDrop);
                lifeformResourceComboBox.SelectedIndex = index;
            }
            else
            {
                lifeformResourceComboBox.SelectedIndex = 0;
            }

            if (entity.Notes != null)
            {
                lifeformNotesTextbox.Text = entity.Notes;
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
            var resultingFauna = (originalEntity as Fauna).DeepCopy();
            UpdateLifeformAttributes(resultingFauna);
            return resultingFauna;
        }

        public Flora GetResultingFlora()
        {
            var resultingFlora = (originalEntity as Flora).DeepCopy();
            UpdateLifeformAttributes(resultingFlora);
            return resultingFlora;
        }

        private void UpdateLifeformAttributes(LifeformEntity lifeform)
        {
            lifeform.Name = lifeformNameTextbox.Text;
            lifeform.Notes = lifeformNotesTextbox.Text;

            var selectedItem = lifeformResourceComboBox.SelectedItem as Resource;
            if (selectedItem != null && selectedItem.FullName != "Unknown")
            {
                lifeform.PrimaryDrops = new List<Resource> { lifeformResourceComboBox.SelectedItem as Resource };
            }
            else
            {
                lifeform.PrimaryDrops = null;
            }
        }

        private void lifeformNameTextboxChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMatchIndicatorVisibility();
            SaveButton.IsEnabled = lifeformNameTextbox.Text.Length > 0;
        }

        private void UpdateMatchIndicatorVisibility()
        {
            if (matchIndicatorLabel == null) { return; } // wait for UI to load

            var lifeformNames = mainViewModel.GetLifeformNames(originalEntity.LifeformType);

            var inputToMatch = lifeformNameTextbox.Text.ToLower();
            var matchedNames = lifeformNames.Where(pair => pair.Key.StartsWith(inputToMatch));

            // present suggestion if exactly 1 lifeform name is matched
            if (matchedNames.Count() == 1)
            {
                matchedNameString = matchedNames.First().Value;
            }
            else if (matchedNames.Any(name => name.Key == inputToMatch))
            {
                matchedNameString = matchedNames.First(name => name.Key == inputToMatch).Value;
            }
            else
            {
                matchedNameString = null;
            }

            if (matchedNameString != null)
            {
                // restore capitalized version
                if (!matchedNameString.StartsWith(lifeformNameTextbox.Text))
                {
                    lifeformNameTextbox.Text = matchedNameString.Substring(0, lifeformNameTextbox.Text.Length);
                    lifeformNameTextbox.SelectionStart = lifeformNameTextbox.Text.Length;
                }

                matchIndicatorLabel.Visibility = Visibility.Visible;
                lifeformNameHint.Content = matchedNameString;
                lifeformNameHint.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
                lifeformNameHint.Visibility = Visibility.Hidden;
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
            if (DialogResult == true)
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
