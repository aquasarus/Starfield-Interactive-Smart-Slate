using Starfield_Interactive_Smart_Slate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class LifeformEditor : Window
    {
        public string LifeformTypeString { get; set; }

        private int faunaID;
        private int floraID;
        private HashSet<string> lifeformNames;

        public LifeformEditor(Fauna fauna, List<Resource> resources, HashSet<string> lifeformNames)
        {
            InitializeComponent();

            faunaID = fauna.FaunaID;
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

        public LifeformEditor(Flora flora, List<Resource> resources, HashSet<string> lifeformNames)
        {
            InitializeComponent();

            floraID = flora.FloraID;
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
            var resultingFauna = new Fauna
            {
                FaunaID = faunaID,
                FaunaName = lifeformNameTextbox.Text,
                FaunaNotes = lifeformNotesTextbox.Text
            };

            if (lifeformResourceComboBox.SelectedIndex != 0)
            {
                resultingFauna.AddPrimaryDrop(lifeformResourceComboBox.SelectedItem as Resource);
            }

            return resultingFauna;
        }

        public Flora GetResultingFlora()
        {
            var resultingFlora = new Flora
            {
                FloraID = floraID,
                FloraName = lifeformNameTextbox.Text,
                FloraNotes = lifeformNotesTextbox.Text
            };

            if (lifeformResourceComboBox.SelectedIndex != 0)
            {
                resultingFlora.AddPrimaryDrop(lifeformResourceComboBox.SelectedItem as Resource);
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
            if (matchIndicatorLabel == null) { return; }

            if (lifeformNames.Contains(lifeformNameTextbox.Text))
            {
                matchIndicatorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                matchIndicatorLabel.Visibility = Visibility.Hidden;
            }
        }
    }
}
