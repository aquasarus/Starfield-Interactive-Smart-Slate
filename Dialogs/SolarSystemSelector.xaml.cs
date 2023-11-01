using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class SolarSystemSelector : Window, INotifyPropertyChanged
    {
        public SolarSystem SelectedSolarSystem { get; private set; }
        public bool HasItemSelected { get { return solarSystemComboBox.SelectedItem != null; } }
        public event PropertyChangedEventHandler PropertyChanged;

        public SolarSystemSelector(List<SolarSystem> solarSystems)
        {
            InitializeComponent();

            // Populate ComboBox with SolarSystem objects
            solarSystemComboBox.ItemsSource = solarSystems;
            solarSystemComboBox.DisplayMemberPath = "SystemName";
            discoverButton.DataContext = this;

            FocusManager.SetFocusedElement(this, solarSystemComboBox);
        }


        private void SolarSystemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle the selection changed event here
            if (solarSystemComboBox.SelectedItem != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(HasItemSelected)));
            }
        }

        private void DiscoverButtonClicked(object sender, RoutedEventArgs e)
        {
            // Set the selected SolarSystem and close the dialog
            this.DialogResult = true;
            SelectedSolarSystem = solarSystemComboBox.SelectedItem as SolarSystem;
            Close();
        }

        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && solarSystemComboBox.SelectedItem != null)
            {
                DialogResult = true;
                SelectedSolarSystem = solarSystemComboBox.SelectedItem as SolarSystem;
                Close();
                e.Handled = true;
            }
        }
    }
}
