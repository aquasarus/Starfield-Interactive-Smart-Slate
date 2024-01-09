using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate.Screens
{
    public partial class InorganicResourceSearch : UserControl
    {
        private InorganicResourceSearchViewModel viewModel = InorganicResourceSearchViewModel.Instance;

        public InorganicResourceSearch()
        {
            InitializeComponent();
        }

        private static void FilterResources(string filterText, ListView listView)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            view.Filter = item =>
            {
                if (item is Resource resource)
                {
                    return resource.FullName.ToLower().Contains(filterText.ToLower());
                }
                return false;
            };
        }

        private void CelestialBodyListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            MouseWheelEventArgs mouseArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            mouseArgs.RoutedEvent = MouseWheelEvent;
            mouseArgs.Source = sender;
            var parent = VisualTreeHelper.GetParent(sender as UIElement) as UIElement;
            parent.RaiseEvent(mouseArgs);
        }

        private void InorganicResourceFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterResources(inorganicResourceFilter.Text, inorganicResourceListView);
        }

        private void InorganicResourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = inorganicResourceListView.SelectedItems;

            if (selectedItems.Count == 0)
            {
                App.Current.PlayCancelSound();
                viewModel.SearchCelestialBodiesForResource(null);
            }
            else if (selectedItems.Count == 1)
            {
                App.Current.PlayClickSound();

                var selectedResource = (Resource)selectedItems[0];
                AnalyticsUtil.TrackResourceEvent("Search inorganic resource", selectedResource);
                viewModel.SearchCelestialBodiesForResource(new List<Resource> { selectedResource });
            }
            else
            {
                App.Current.PlayClickSound();

                // cast to non-generic type
                var selectedResources = new List<Resource>();
                foreach (Resource resource in selectedItems)
                {
                    selectedResources.Add(resource);
                }

                // TODO: multi-search event
                viewModel.SearchCelestialBodiesForResource(selectedResources);
            }
        }

        private void ResourceSearchListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            App.Current.PlayScrollSound();
        }
    }
}