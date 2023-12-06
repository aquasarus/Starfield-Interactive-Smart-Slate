using Starfield_Interactive_Smart_Slate.Common;
using Starfield_Interactive_Smart_Slate.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate.Screens
{
    public partial class OrganicResourceSearch : UserControl
    {
        private List<SolarSystem>? discoveredSolarSystems;
        private CelestialBody? displayedOrganicResultCelestialBody;
        private CelestialBody? selectedOrganicResultCelestialBody;

        public OrganicResourceSearch()
        {
            InitializeComponent();

            var resources = DataRepository.GetResources();
            organicResourceListView.ItemsSource = resources.Where(r => r.GetType() == ResourceType.Organic);

            IsVisibleChanged += (sender, args) =>
            {
                RefreshData();
            };
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

        private void DisplayOrganicResultCelestialBody(CelestialBody celestialBody)
        {
            faunaResultsListView.ItemsSource = celestialBody.Faunas;
            floraResultsListView.ItemsSource = celestialBody.Floras;
        }

        private void OrganicResourceFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterResources(organicResourceFilter.Text, organicResourceListView);
        }

        private void OrganicResourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            faunaResultsListView.ItemsSource = null;
            floraResultsListView.ItemsSource = null;
            selectedOrganicResultCelestialBody = null;
            displayedOrganicResultCelestialBody = null;

            var selectedResource = (Resource)organicResourceListView.SelectedItem;

            if (selectedResource == null)
            {
                App.Current.PlayCancelSound();
            }
            else
            {
                AnalyticsUtil.TrackEvent("Search organic resource");
                App.Current.PlayClickSound();
            }

            organicSolarSystemResultsListView.ItemsSource = SearchCelestialBodiesAndLifeformsForResource(selectedResource);
        }

        private void OrganicResultsListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is CelestialBody)
            {
                var celestialBody = listViewItem.DataContext as CelestialBody;
                if (selectedOrganicResultCelestialBody == null && displayedOrganicResultCelestialBody != celestialBody)
                {
                    DisplayOrganicResultCelestialBody(celestialBody);
                }
                App.Current.PlayScrollSound();
            }
        }

        private void OrganicResultsListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = Framework.FindParentListView(clickedItem);

            if (celestialBody.Equals(selectedOrganicResultCelestialBody))
            {
                App.Current.PlayCancelSound();

                selectedOrganicResultCelestialBody = null;
                parent.UnselectAll();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                selectedOrganicResultCelestialBody = celestialBody;
                parent.SelectedItem = celestialBody;
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                Framework.ClearInnerListViews(organicSolarSystemResultsListView, parent);
                DisplayOrganicResultCelestialBody(celestialBody);
            }

            e.Handled = true;
        }

        private void RefreshData()
        {
            var solarSystems = DataRepository.GetSolarSystems();
            discoveredSolarSystems = solarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();
        }

        private void ResourceSearchListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            App.Current.PlayScrollSound();
        }

        private IEnumerable<SolarSystem> SearchCelestialBodiesAndLifeformsForResource(Resource resource)
        {
            return discoveredSolarSystems.Select(
                solarSystem =>
                {
                    var solarSystemCopy = solarSystem.DeepCopy();

                    // chaining .Select here ended up with new instances of moons for some reason
                    // so I have to loop it manually
                    foreach (var celestialBody in solarSystemCopy.CelestialBodies)
                    {
                        var lifeformResult = celestialBody.GetLifeformsWithResource(resource);
                        var found = lifeformResult.Item1;
                        var faunaList = lifeformResult.Item2;
                        var floraList = lifeformResult.Item3;

                        if (found)
                        {
                            celestialBody.Faunas = faunaList;
                            celestialBody.Floras = floraList;
                            celestialBody.Show = true;
                            celestialBody.GrayOut = false;
                        }
                        else
                        {
                            celestialBody.Show = false;
                        }
                    }

                    foreach (var celestialBody in solarSystemCopy.CelestialBodies)
                    {
                        if (!celestialBody.Show)
                        {
                            if (celestialBody.Moons?.Any(moon => moon.Show) ?? false)
                            {
                                // gray out the parent planet if its surface doesn't actually contain the resource
                                celestialBody.Show = true;
                                celestialBody.GrayOut = true;
                            }
                        }
                    }

                    solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Where(
                        celestialBody => celestialBody.Show
                    ).ToList();

                    return solarSystemCopy;
                }
            ).Where(solarSystem => solarSystem.CelestialBodies.Any());
        }
    }
}