using Starfield_Interactive_Smart_Slate.Common;
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
    public partial class OrganicResourceSearch : UserControl
    {
        private OrganicResourceSearchViewModel viewModel = OrganicResourceSearchViewModel.Instance;

        public OrganicResourceSearch()
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

        private void OrganicResourceFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterResources(organicResourceFilter.Text, organicResourceListView);
        }

        private void OrganicResourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.ClearSelections();

            var selectedItems = organicResourceListView.SelectedItems;

            if (selectedItems.Count == 0)
            {
                App.Current.PlayCancelSound();
                viewModel.SearchCelestialBodiesForResource(null);
            }
            else if (selectedItems.Count == 1)
            {
                App.Current.PlayClickSound();

                // cast to non-generic type
                var selectedResources = new List<Resource>();
                foreach (Resource resource in selectedItems)
                {
                    selectedResources.Add(resource);
                }

                var selectedResource = (Resource)selectedItems[0];
                AnalyticsUtil.TrackResourceEvent("Search organic resource", selectedResource);
                viewModel.SearchCelestialBodiesForResource(selectedResources);
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

        private void OrganicResultsListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is CelestialBody)
            {
                var celestialBody = listViewItem.DataContext as CelestialBody;
                if (viewModel.DisplayedCelestialBody != celestialBody)
                {
                    App.Current.PlayScrollSound();

                    if (viewModel.SelectedCelestialBody == null)
                    {
                        viewModel.DisplayCelestialBody(celestialBody);
                    }
                }
            }
        }

        private void OrganicResultsListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = Framework.FindParentListView(clickedItem);

            if (celestialBody.Equals(viewModel.SelectedCelestialBody))
            {
                App.Current.PlayCancelSound();

                viewModel.ClearSelections(propagateDisplay: false);

                parent.UnselectAll();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                viewModel.SelectCelestialBody(celestialBody);

                parent.SelectedItem = celestialBody;
                clickedItem.IsSelected = true;
                clickedItem.Focus();

                // clear previous selection
                Framework.ClearInnerListViews(organicSolarSystemResultsListView, parent);
            }

            e.Handled = true;
        }

        private void ResourceSearchListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            App.Current.PlayScrollSound();
        }
    }
}