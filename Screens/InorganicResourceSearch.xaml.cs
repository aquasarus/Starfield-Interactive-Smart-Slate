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
    public partial class InorganicResourceSearch : UserControl
    {
        private List<SolarSystem>? discoveredSolarSystems;

        public InorganicResourceSearch()
        {
            InitializeComponent();
            var resources = DataRepository.GetResources();
            inorganicResourceListView.ItemsSource = resources.Where(r => r.GetType() == ResourceType.Inorganic);

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

        private void InorganicResourceFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterResources(inorganicResourceFilter.Text, inorganicResourceListView);
        }

        private void InorganicResourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedResource = (Resource)inorganicResourceListView.SelectedItem;

            if (selectedResource == null)
            {
                App.Current.PlayCancelSound();
            }
            else
            {
                AnalyticsUtil.TrackEvent("Search inorganic resource");
                App.Current.PlayClickSound();
            }

            inorganicSolarSystemResultsListView.ItemsSource = SearchCelestialBodiesForResource(selectedResource);
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

        private IEnumerable<SolarSystem> SearchCelestialBodiesForResource(Resource resource)
        {
            return discoveredSolarSystems.Select(
                solarSystem =>
                {
                    var solarSystemCopy = solarSystem.DeepCopy();
                    solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Select(
                        celestialBody =>
                        {
                            var surfaceHasResource = celestialBody.SurfaceContainsResource(resource);
                            var moonsHaveResource = celestialBody.Moons?.Any(moon => moon.SurfaceContainsResource(resource)) ?? false;

                            // if celestial body (or any of its moons) contains the resource, include it in the list
                            if (surfaceHasResource || moonsHaveResource)
                            {
                                if (!surfaceHasResource)
                                {
                                    // gray out the parent planet if its surface doesn't actually contain the resource
                                    celestialBody.GrayOut = true;
                                }

                                celestialBody.Show = true;
                            }
                            else
                            {
                                celestialBody.Show = false;
                            }

                            return celestialBody;
                        }
                    )
                    .Where(celestialBody => celestialBody.Show)
                    .ToList();
                    return solarSystemCopy;
                }
            ).Where(solarSystem => solarSystem.CelestialBodies.Any());
        }
    }
}