using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string DisplayedCelestialBodyName { get { return displayedCelestialBody?.BodyName; } }
        public string DisplayedSolarSystemName { get { return displayedCelestialBody?.SystemName; } }
        public event PropertyChangedEventHandler PropertyChanged;

        private DataRepository dataRepository = new DataRepository();
        private List<SolarSystem> allSolarSystems;
        private List<SolarSystem> discoveredSolarSystems;
        private Dictionary<LifeformType, Dictionary<string, string>> lifeformNames;
        private List<Resource> allOrganicResources;

        private CelestialBody selectedCelestialBody;
        private CelestialBody displayedCelestialBody;

        private Fauna selectedFauna;
        private Fauna displayedFauna;

        private Flora selectedFlora;
        private Flora displayedFlora;

        private CelestialBody selectedOrganicResultCelestialBody;
        private CelestialBody displayedOrganicResultCelestialBody;

        public MainWindow()
        {
            InitializeComponent();

            // show version number
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            string versionNumber = $"Version {version.Major}.{version.Minor}.{version.Build}";
            VersionNumberLabel.Content = versionNumber;

            var resources = dataRepository.GetResources();

            inorganicResourceListView.ItemsSource = resources.Where(r => r.GetType() == ResourceType.Inorganic);
            allOrganicResources = resources.Where(r => r.GetType() == ResourceType.Organic).ToList();
            organicResourceListView.ItemsSource = allOrganicResources;

            lifeformNames = dataRepository.GetLifeformNames();
            celestialBodyTitleLabel.DataContext = this;
            celestialBodyMiniTitleLabel.DataContext = this;

            RefreshData();

            solarSystemsListView.Loaded += InitializeSolarSystemsListView;
        }

        private void RefreshData()
        {
            selectedFauna = null;
            selectedFlora = null;
            var solarSystems = dataRepository.GetSolarSystems();
            allSolarSystems = solarSystems;
            discoveredSolarSystems = solarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();

            solarSystemsListView.ItemsSource = discoveredSolarSystems;
        }

        // -----------------------------------------------------------------------------------------------
        // CELESTIAL BODIES
        // -----------------------------------------------------------------------------------------------
        #region Celestial Body Stuff
        private void InitializeSolarSystemsListView(object sender, RoutedEventArgs e)
        {
            // restore last session's celestial body selection
            var selectedCelestialBodyID = Settings.Default.SelectedCelestialBodyID;
            if (selectedCelestialBodyID != -1)
            {
                foreach (var solarSystem in discoveredSolarSystems)
                {
                    foreach (var celestialBody in solarSystem.CelestialBodies)
                    {
                        if (celestialBody.BodyID == selectedCelestialBodyID)
                        {
                            SetSelectedCelestialBodyWithUI(celestialBody, solarSystem);
                            DisplayCelestialBodyDetails(celestialBody);

                            // restore last session's fauna/flora selection
                            var selectedFaunaID = Settings.Default.SelectedFaunaID;
                            var selectedFloraID = Settings.Default.SelectedFloraID;
                            if (selectedFaunaID != -1)
                            {
                                var fauna = celestialBody.Faunas.First(f => f.FaunaID == selectedFaunaID);
                                SetSelectedFaunaWithUI(fauna);
                            }
                            else if (selectedFloraID != -1)
                            {
                                var flora = celestialBody.Floras.First(f => f.FloraID == selectedFloraID);
                                SetSelectedFloraWithUI(flora);
                            }

                            break;
                        }
                    }
                }
            }

            // only need to run once
            solarSystemsListView.Loaded -= InitializeSolarSystemsListView;
        }

        private void SolarSystemFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterSolarSystems(SolarSystemFilterTextBox.Text);
        }
        private void FilterSolarSystems(string filterText)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(solarSystemsListView.ItemsSource);
            view.Filter = item =>
            {
                if (item is SolarSystem solarSystem)
                {
                    return solarSystem.SystemName.ToLower().Contains(filterText.ToLower())
                    || solarSystem.CelestialBodies.Where(cb => cb.BodyName.ToLower().Contains(filterText.ToLower())).Any();
                }
                return false;
            };
        }

        private void SetSelectedCelestialBody(CelestialBody celestialBody)
        {
            selectedCelestialBody = celestialBody;
            Settings.Default.SelectedCelestialBodyID = selectedCelestialBody?.BodyID ?? -1;
            Settings.Default.Save();
        }

        private void SetSelectedCelestialBodyWithUI(CelestialBody celestialBody, SolarSystem? parentSolarSystem = null)
        {
            // find parent solar system if necessary
            SolarSystem selectedSolarSystem = null;
            if (parentSolarSystem != null)
            {
                selectedSolarSystem = parentSolarSystem;
            }
            else
            {
                foreach (var solarSystem in discoveredSolarSystems)
                {
                    if (solarSystem.CelestialBodies.Contains(celestialBody))
                    {
                        selectedSolarSystem = solarSystem;
                        break;
                    }
                }
            }

            // update data state
            SetSelectedCelestialBody(celestialBody);

            // set UI to selected state
            var solarSystemListViewItem = solarSystemsListView
                .ItemContainerGenerator.ContainerFromItem(selectedSolarSystem);
            ListView celestialBodyListView = FindNestedListView(solarSystemListViewItem);
            celestialBodyListView.SelectedItem = celestialBody;

            // scroll parent solar system into view
            solarSystemsListView.ScrollIntoView(selectedSolarSystem);
        }

        private ListView FindNestedListView(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is ListView nestedListView)
                {
                    return nestedListView;
                }
                else
                {
                    ListView result = FindNestedListView(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
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

        private void CelestialBodyListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is CelestialBody)
            {
                var celestialBody = listViewItem.DataContext as CelestialBody;
                if (selectedCelestialBody == null && displayedCelestialBody != celestialBody)
                {
                    DisplayCelestialBodyDetails(celestialBody);
                    ClearFaunaSelection();
                    ClearFloraSelection();
                }
            }
        }

        private void CelestialBodyListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = FindParentListView(clickedItem);
            ToggleSelectCelestialBody(celestialBody, parent, clickedItem);
            e.Handled = true;
        }

        private ListView FindParentListView(ListViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);

            while (parent != null && !(parent is ListView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ListView;
        }

        private void ClearInnerListViews(DependencyObject parent, ListView exceptThis)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListView listView && child != exceptThis)
                {
                    listView.SelectedItem = null;
                    continue;
                }

                // Recursively search for inner ListViews in child elements
                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    ClearInnerListViews(child, exceptThis);
                }
            }
        }

        private void ToggleSelectCelestialBody(CelestialBody celestialBody, ListView parent, ListViewItem clickedItem)
        {
            if (celestialBody.Equals(selectedCelestialBody))
            {
                SetSelectedCelestialBody(null);
                parent.UnselectAll();
                clickedItem.IsSelected = false;
            }
            else
            {
                SetSelectedCelestialBody(celestialBody);
                parent.SelectedItem = celestialBody;
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                ClearInnerListViews(solarSystemsListView, parent);
                DisplayCelestialBodyDetails(celestialBody);
                ClearFaunaSelection();
                ClearFloraSelection();
            }
        }

        private void DisplayCelestialBodyDetails(CelestialBody celestialBody)
        {
            lifeformOverviewGrid.Visibility = Visibility.Hidden;
            editLifeformButton.IsEnabled = false;

            displayedCelestialBody = celestialBody;

            celestialBodyOverview.Text = celestialBody.ToString();
            celestialBodyResourcesLabel.Text = celestialBody.ResourcesString;

            faunasListView.ItemsSource = celestialBody.Faunas;
            florasListView.ItemsSource = celestialBody.Floras;
            addFaunaButton.IsEnabled = ((celestialBody.Faunas?.Count ?? 0) == celestialBody.TotalFauna) ? false : true;
            addFloraButton.IsEnabled = ((celestialBody.Floras?.Count ?? 0) == celestialBody.TotalFlora) ? false : true;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedCelestialBodyName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedSolarSystemName)));

            celestialBodyOverviewGrid.Visibility = Visibility.Visible;
        }

        private void DiscoverNewSystemClicked(object sender, RoutedEventArgs e)
        {
            List<SolarSystem> alphabeticalSystems = allSolarSystems
                .Where(solarSystem => !solarSystem.Discovered)
                .OrderBy(solarSystem => solarSystem.SystemName).ToList();

            SolarSystemSelector dialog = new SolarSystemSelector(alphabeticalSystems);

            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                SolarSystem selectedSolarSystem = dialog.SelectedSolarSystem;
                dataRepository.DiscoverSolarSystem(selectedSolarSystem);
                RefreshData();

                var firstCelestialBodyOfSystem = selectedSolarSystem.CelestialBodies[0];
                DisplayCelestialBodyDetails(firstCelestialBodyOfSystem);

                solarSystemsListView.LayoutUpdated += SetCelestialBodyOnLayoutUpdate;
            }
        }

        private void SetCelestialBodyOnLayoutUpdate(object sender, EventArgs e)
        {
            SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            solarSystemsListView.LayoutUpdated -= SetCelestialBodyOnLayoutUpdate;
        }
        #endregion

        // -----------------------------------------------------------------------------------------------
        // FLORA / FAUNA
        // -----------------------------------------------------------------------------------------------
        #region Fauna/Flora Lifeform stuff
        private void FaunaListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Fauna)
            {
                var fauna = listViewItem.DataContext as Fauna;
                if (selectedFauna == null && selectedFlora == null)
                {
                    DisplayFaunaDetails(fauna);
                }
            }
        }

        private void FaunaListView_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            Fauna fauna = clickedItem.DataContext as Fauna;
            ToggleSelectFauna(fauna, clickedItem);
            e.Handled = true;
        }

        private void ToggleSelectFauna(Fauna fauna, ListViewItem clickedItem)
        {
            if (selectedFauna == fauna)
            {
                // like ClearFaunaSelection but keep displayedFauna
                selectedFauna = null;
                faunasListView.UnselectAll();
                Settings.Default.SelectedFaunaID = -1;
                Settings.Default.Save();
                clickedItem.IsSelected = false;
            }
            else
            {
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                SetSelectedFaunaWithUI(fauna);
            }
        }

        private void ClearFaunaSelection()
        {
            selectedFauna = null;
            displayedFauna = null;
            faunasListView.UnselectAll();
            Settings.Default.SelectedFaunaID = -1;
            Settings.Default.Save();
        }

        private void SetSelectedFaunaWithUI(Fauna fauna)
        {
            if (fauna == null)
            {
                throw new Exception("SetSelectedFauna() is only for non-null values. Use ClearFaunaSelection() otherwise.");
            }

            ClearFloraSelection();
            selectedFauna = fauna;
            faunasListView.SelectedItem = fauna;
            DisplayFaunaDetails(fauna);

            Settings.Default.SelectedFaunaID = fauna.FaunaID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (selectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            }
        }

        private void DisplayFaunaDetails(Fauna fauna)
        {
            displayedFauna = fauna;

            lifeformTitleLabel.Content = fauna.FaunaName;
            lifeformSubtitleLabel.Content = "· Fauna";
            lifeformResourceLabel.Content = fauna.ResourceString;
            lifeformNotesTextBlock.Text = fauna.NotesString;

            lifeformOverviewGrid.Visibility = Visibility.Visible;
            editLifeformButton.IsEnabled = true;
        }

        private void AddFaunaClicked(object sender, RoutedEventArgs e)
        {
            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Fauna], LifeformType.Fauna);

            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFauna = dataRepository.AddFauna(dialog.lifeformNameInput.Text, displayedCelestialBody.BodyID);

                // update local data state
                displayedCelestialBody.AddFauna(insertedFauna);

                // update UI state
                DisplayCelestialBodyDetails(displayedCelestialBody);
                DisplayFaunaDetails(insertedFauna);
                SetSelectedFaunaWithUI(insertedFauna);
            }
        }

        private void FloraListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Flora)
            {
                var flora = listViewItem.DataContext as Flora;
                if (selectedFauna == null && selectedFlora == null)
                {
                    DisplayFloraDetails(flora);
                }
            }
        }

        private void FloraListView_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            Flora flora = clickedItem.DataContext as Flora;
            ToggleSelectFlora(flora, clickedItem);
            e.Handled = true;
        }

        private void ToggleSelectFlora(Flora flora, ListViewItem clickedItem)
        {
            if (selectedFlora == flora)
            {
                // like ClearFloraSelection but keep displayedFlora
                selectedFlora = null;
                florasListView.UnselectAll();
                Settings.Default.SelectedFloraID = -1;
                Settings.Default.Save();
                clickedItem.IsSelected = false;
            }
            else
            {
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                SetSelectedFloraWithUI(flora);
            }
        }

        private void ClearFloraSelection()
        {
            selectedFlora = null;
            displayedFlora = null;
            florasListView.UnselectAll();
            Settings.Default.SelectedFloraID = -1;
            Settings.Default.Save();
        }

        private void SetSelectedFloraWithUI(Flora flora)
        {
            if (flora == null)
            {
                throw new Exception("SetSelectedFlora() is only for non-null values. Use ClearFloraSelection() otherwise.");
            }

            ClearFaunaSelection();
            selectedFlora = flora;
            florasListView.SelectedItem = flora;
            DisplayFloraDetails(flora);

            Settings.Default.SelectedFloraID = flora.FloraID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (selectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            }
        }

        private void DisplayFloraDetails(Flora flora)
        {
            displayedFlora = flora;

            lifeformTitleLabel.Content = flora.FloraName;
            lifeformSubtitleLabel.Content = "· Flora";
            lifeformResourceLabel.Content = flora.ResourceString;
            lifeformNotesTextBlock.Text = flora.NotesString;

            lifeformOverviewGrid.Visibility = Visibility.Visible;
            editLifeformButton.IsEnabled = true;
        }

        private void AddFloraClicked(object sender, RoutedEventArgs e)
        {
            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Flora], LifeformType.Flora);

            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFlora = dataRepository.AddFlora(dialog.lifeformNameInput.Text, displayedCelestialBody.BodyID);

                // update local data state
                displayedCelestialBody.AddFlora(insertedFlora);

                // update UI state
                DisplayCelestialBodyDetails(displayedCelestialBody);
                DisplayFloraDetails(insertedFlora);
                SetSelectedFloraWithUI(insertedFlora);
            }
        }

        private void EditLifeformClicked(object sender, RoutedEventArgs e)
        {
            if (displayedFauna != null)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedFauna,
                    allOrganicResources,
                    lifeformNames[LifeformType.Fauna]
                );

                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFauna = dialog.GetResultingFauna();
                    dataRepository.EditFauna(displayedFauna, resultingFauna);
                    displayedCelestialBody.EditFauna(resultingFauna);
                    DisplayFaunaDetails(resultingFauna);
                    if (selectedFauna != null)
                    {
                        selectedFauna = resultingFauna;
                        faunasListView.SelectedItem = selectedFauna;
                    }
                }
            }
            else if (displayedFlora != null)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedFlora,
                    allOrganicResources,
                    lifeformNames[LifeformType.Flora]
                );

                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFlora = dialog.GetResultingFlora();
                    dataRepository.EditFlora(displayedFlora, resultingFlora);
                    displayedCelestialBody.EditFlora(resultingFlora);
                    DisplayFloraDetails(resultingFlora);
                    if (selectedFlora != null)
                    {
                        selectedFlora = resultingFlora;
                        florasListView.SelectedItem = selectedFlora;
                    }
                }
            }
        }
        #endregion

        // -----------------------------------------------------------------------------------------------
        // RESOURCE SEARCH
        // -----------------------------------------------------------------------------------------------
        #region Organic/Inorganic Resource Stuff
        private void InorganicResourceFilterChanged(object sender, TextChangedEventArgs e)
        {
            FilterResources(inorganicResourceFilter.Text, inorganicResourceListView);
        }

        private void InorganicResourceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedResource = (Resource)inorganicResourceListView.SelectedItem;
            inorganicSolarSystemResultsListView.ItemsSource = SearchCelestialBodiesForResource(selectedResource);
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
            organicSolarSystemResultsListView.ItemsSource = SearchCelestialBodiesAndLifeformsForResource(selectedResource);
        }

        private IEnumerable<SolarSystem> SearchCelestialBodiesForResource(Resource resource)
        {
            return discoveredSolarSystems.Select(
                solarSystem =>
                {
                    var solarSystemCopy = new SolarSystem
                    {
                        SystemID = solarSystem.SystemID,
                        SystemName = solarSystem.SystemName,
                        SystemLevel = solarSystem.SystemLevel,
                        Discovered = solarSystem.Discovered,
                        // if celestial body (or any of its moons) contains the resource, include it in the list
                        CelestialBodies = solarSystem.CelestialBodies.Select(
                            celestialBody =>
                            {
                                var surfaceHasResource = celestialBody.SurfaceContainsResource(resource);
                                var moonsHaveResource = celestialBody.Moons?.Any(moon => moon.SurfaceContainsResource(resource)) ?? false;

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
                        .ToList()
                    };
                    return solarSystemCopy;
                }
            ).Where(solarSystem => solarSystem.CelestialBodies.Any());
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

        private void OrganicResultsListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is CelestialBody)
            {
                var celestialBody = listViewItem.DataContext as CelestialBody;
                if (selectedOrganicResultCelestialBody == null && displayedOrganicResultCelestialBody != celestialBody)
                {
                    DisplayOrganicResultCelestialBody(celestialBody);
                }
            }
        }

        private void DisplayOrganicResultCelestialBody(CelestialBody celestialBody)
        {
            faunaResultsListView.ItemsSource = celestialBody.Faunas;
            floraResultsListView.ItemsSource = celestialBody.Floras;
        }

        private void OrganicResultsListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = FindParentListView(clickedItem);

            if (celestialBody.Equals(selectedOrganicResultCelestialBody))
            {
                selectedOrganicResultCelestialBody = null;
                parent.UnselectAll();
                clickedItem.IsSelected = false;
            }
            else
            {
                selectedOrganicResultCelestialBody = celestialBody;
                parent.SelectedItem = celestialBody;
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                ClearInnerListViews(organicSolarSystemResultsListView, parent);
                DisplayOrganicResultCelestialBody(celestialBody);
            }

            e.Handled = true;
        }

        private void FilterResources(string filterText, ListView listView)
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
        #endregion

        // -----------------------------------------------------------------------------------------------
        // ABOUT PAGE
        // -----------------------------------------------------------------------------------------------
        private void NavigateToHyperlink(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true // need to set this to get web links to work here
            });
            e.Handled = true;
        }
        private void DataFolderLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", DatabaseInitializer.UserDatabaseFolder());
        }
    }
}
