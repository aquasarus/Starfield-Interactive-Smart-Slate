using Microsoft.Win32;
using Starfield_Interactive_Smart_Slate.Common;
using Starfield_Interactive_Smart_Slate.Dialogs;
using Starfield_Interactive_Smart_Slate.Models;
using Starfield_Interactive_Smart_Slate.Models.Entities;
using Starfield_Interactive_Smart_Slate.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Starfield_Interactive_Smart_Slate.Screens.PlanetData
{
    public partial class PlanetDataMain : UserControl, INotifyPropertyChanged
    {
        private Window? activePictureViewer = null;
        private List<SolarSystem> allSolarSystems;
        private List<SolarSystem> discoveredSolarSystems;
        private CelestialBody displayedCelestialBody;
        private Entity? displayedEntity;
        private Dictionary<LifeformType, Dictionary<string, string>> lifeformNames;
        private List<Resource> selectableOrganicResources;
        private CelestialBody selectedCelestialBody;
        private Entity? selectedEntity;

        public PlanetDataMain()
        {
            InitializeComponent();

            var resources = DataRepository.GetResources();

            selectableOrganicResources = resources.Where(r =>
            {
                return r.GetType() == ResourceType.Organic || r.GetType() == ResourceType.Placeholders;
            }).ToList();

            lifeformNames = DataRepository.GetLifeformNames();
            celestialBodyTitleLabel.DataContext = this;
            celestialBodyMiniTitleLabel.DataContext = this;

            RefreshData();

            solarSystemsListView.Loaded += InitializeSolarSystemsListView;

            pictureGrid.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string DisplayedCelestialBodyName
        { get { return displayedCelestialBody?.BodyName; } }

        public string DisplayedSolarSystemName
        { get { return displayedCelestialBody?.SystemName; } }

        public int PictureGridColumns { get; set; }

        #region CELESTIAL BODIES -----------------------------------------------------------------------------------------------

        private void AddFaunaClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Fauna], LifeformType.Fauna);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFauna = DataRepository.AddFauna(dialog.lifeformNameInput.Text, displayedCelestialBody.BodyID);

                // update local data state
                displayedCelestialBody.AddFauna(insertedFauna);

                // update UI state
                DisplayCelestialBodyDetails(displayedCelestialBody);
                DisplayFaunaDetails(insertedFauna);
                SetSelectedFaunaWithUI(insertedFauna);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                ReapplyFilters();
            }
        }

        private void AddFloraClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Flora], LifeformType.Flora);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFlora = DataRepository.AddFlora(dialog.lifeformNameInput.Text, displayedCelestialBody.BodyID);

                // update local data state
                displayedCelestialBody.AddFlora(insertedFlora);

                // update UI state
                DisplayCelestialBodyDetails(displayedCelestialBody);
                DisplayFloraDetails(insertedFlora);
                SetSelectedFloraWithUI(insertedFlora);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                ReapplyFilters();
            }
        }

        private void AddOutpostClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddOutpostDialog dialog = new AddOutpostDialog();

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedOutpost = DataRepository.AddOutpost(dialog.outpostNameInput.Text, displayedCelestialBody.BodyID);

                // update local data state
                displayedCelestialBody.AddOutpost(insertedOutpost);

                // update UI state
                DisplayCelestialBodyDetails(displayedCelestialBody);
                DisplayOutpostDetails(insertedOutpost);
                SetSelectedOutpostWithUI(insertedOutpost);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                ReapplyFilters();
            }
        }

        private void ApplyLifeformFilter()
        {
            var outpostSolarSystems = discoveredSolarSystems.Select(
                    solarSystem =>
                    {
                        var solarSystemCopy = solarSystem.DeepCopy();
                        solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Select(
                            celestialBody =>
                            {
                                var surfaceHasLifeform = celestialBody.HasLifeform;
                                var moonsHaveOutposts = celestialBody.Moons?.Any(moon => moon.HasLifeform) ?? false;

                                // if celestial body (or any of its moons) has lifeform, include it in the list
                                if (surfaceHasLifeform || moonsHaveOutposts)
                                {
                                    celestialBody.GrayOut = !surfaceHasLifeform;
                                    celestialBody.Show = true;
                                }
                                else
                                {
                                    celestialBody.Show = false;
                                }

                                return celestialBody;
                            }
                        ).Where(celestialBody => celestialBody.Show)
                        .ToList();

                        return solarSystemCopy;
                    }
                ).Where(solarSystem => solarSystem.CelestialBodies.Any());
            solarSystemsListView.ItemsSource = outpostSolarSystems;

            recoverCelestialBodySelection();
        }

        private void ApplyOutpostsFilter()
        {
            var outpostSolarSystems = discoveredSolarSystems.Select(
                    solarSystem =>
                    {
                        var solarSystemCopy = solarSystem.DeepCopy();
                        solarSystemCopy.CelestialBodies = solarSystemCopy.CelestialBodies.Select(
                            celestialBody =>
                            {
                                var surfaceHasOutpost = celestialBody.HasOutpost;
                                var moonsHaveOutposts = celestialBody.Moons?.Any(moon => moon.HasOutpost) ?? false;

                                // if celestial body (or any of its moons) has an outpost, include it in the list
                                if (surfaceHasOutpost || moonsHaveOutposts)
                                {
                                    celestialBody.GrayOut = !surfaceHasOutpost;
                                    celestialBody.Show = true;
                                }
                                else
                                {
                                    celestialBody.Show = false;
                                }

                                return celestialBody;
                            }
                        ).Where(celestialBody => celestialBody.Show)
                        .ToList();

                        return solarSystemCopy;
                    }
                ).Where(solarSystem => solarSystem.CelestialBodies.Any());
            solarSystemsListView.ItemsSource = outpostSolarSystems;

            recoverCelestialBodySelection();
        }

        private void celestialBodiesFilterButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void celestialBodiesFilterButton_ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
        }

        private void CelestialBodyListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is CelestialBody)
            {
                var celestialBody = listViewItem.DataContext as CelestialBody;
                if (selectedCelestialBody == null && displayedCelestialBody != celestialBody)
                {
                    DisplayCelestialBodyDetails(celestialBody);
                    ClearAllSelectionsExcept();
                }

                App.Current.PlayScrollSound();
            }
        }

        private void CelestialBodyListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = Framework.FindParentListView(clickedItem);
            ToggleSelectCelestialBody(celestialBody, parent, clickedItem);
            e.Handled = true;
            AnalyticsUtil.TrackEvent("Select celestial body");
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

        // clears all list selections except specified type
        private void ClearAllSelectionsExcept(Entity? exception = null)
        {
            selectedEntity = null;
            displayedEntity = null;

            if (exception is Fauna)
            {
                ClearFloraSelection();
                ClearOutpostSelection();
            }
            else if (exception is Flora)
            {
                ClearFaunaSelection();
                ClearOutpostSelection();
            }
            else if (exception is Outpost)
            {
                ClearFaunaSelection();
                ClearFloraSelection();
            }
            else if (exception == null)
            {
                ClearFaunaSelection();
                ClearFloraSelection();
                ClearOutpostSelection();
            }
        }

        // TODO: delete later if unused
        private void ClearCelestialBodyDetails()
        {
            displayedCelestialBody = null;
            selectedCelestialBody = null;

            celestialBodyOverview.Text = null;
            celestialBodyResourcesLabel.Text = null;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedCelestialBodyName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedSolarSystemName)));

            celestialBodyOverviewGrid.Visibility = Visibility.Hidden;

            ResetEntityOverview();
            ClearAllSelectionsExcept();

            faunasListView.ItemsSource = null;
            florasListView.ItemsSource = null;
            outpostsListView.ItemsSource = null;
        }

        private void ClearFaunaSelection()
        {
            faunasListView.UnselectAll();
            Settings.Default.SelectedFaunaID = -1;
            Settings.Default.Save();
        }

        private void ClearFloraSelection()
        {
            florasListView.UnselectAll();
            Settings.Default.SelectedFloraID = -1;
            Settings.Default.Save();
        }

        private void ClearOutpostSelection()
        {
            outpostsListView.UnselectAll();
            Settings.Default.SelectedOutpostID = -1;
            Settings.Default.Save();
        }

        private void DiscoverNewSystemClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            List<SolarSystem> alphabeticalSystems = allSolarSystems
                .Where(solarSystem => !solarSystem.Discovered)
                .OrderBy(solarSystem => solarSystem.SystemName).ToList();

            SolarSystemSelector dialog = new SolarSystemSelector(alphabeticalSystems);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                SolarSystem selectedSolarSystem = dialog.SelectedSolarSystem;
                DataRepository.DiscoverSolarSystem(selectedSolarSystem);
                RefreshData();

                var firstCelestialBodyOfSystem = selectedSolarSystem.CelestialBodies[0];
                DisplayCelestialBodyDetails(firstCelestialBodyOfSystem);

                ResetFilters();

                solarSystemsListView.LayoutUpdated += SetCelestialBodyOnLayoutUpdate;
            }
        }

        private void DisplayCelestialBodyDetails(CelestialBody celestialBody)
        {
            entityOverviewGrid.Visibility = Visibility.Hidden;
            editEntityButton.IsEnabled = false;

            displayedCelestialBody = celestialBody;

            celestialBodyOverview.Text = celestialBody.ToString();
            celestialBodyResourcesLabel.Text = celestialBody.ResourcesString;

            faunasListView.ItemsSource = celestialBody.Faunas;
            addFaunaButton.IsEnabled = ((celestialBody.Faunas?.Count ?? 0) == celestialBody.TotalFauna) ? false : true;

            florasListView.ItemsSource = celestialBody.Floras;
            addFloraButton.IsEnabled = ((celestialBody.Floras?.Count ?? 0) == celestialBody.TotalFlora) ? false : true;

            outpostsListView.ItemsSource = celestialBody.Outposts;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedCelestialBodyName)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedSolarSystemName)));

            celestialBodyOverviewGrid.Visibility = Visibility.Visible;
        }

        private void DisplayEntityDetails(Entity entity)
        {
            displayedEntity = entity;

            entityTitleLabel.Content = entity.Name;
            entityNotesTextBlock.Text = entity.Notes;
            entityOverviewGrid.Visibility = Visibility.Visible;
            editEntityButton.IsEnabled = true;
            pictureGrid.ItemsSource = entity.Pictures;

            entityOverviewScrollViewer.ScrollToTop();
        }

        private void DisplayFaunaDetails(Fauna fauna)
        {
            entitySubtitleLabel.Content = "· Fauna";

            lifeformResourceTitleLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Content = fauna.ResourceString;

            DisplayEntityDetails(fauna);
        }

        private void DisplayFloraDetails(Flora flora)
        {
            entitySubtitleLabel.Content = "· Flora";

            lifeformResourceTitleLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Content = flora.ResourceString;

            DisplayEntityDetails(flora);
        }

        private void DisplayOutpostDetails(Outpost outpost)
        {
            entitySubtitleLabel.Content = "· Outpost";

            // outposts have no resource drops
            lifeformResourceTitleLabel.Visibility = Visibility.Collapsed;
            lifeformResourceLabel.Visibility = Visibility.Collapsed;

            DisplayEntityDetails(outpost);
        }

        private void EditEntityClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            if (displayedEntity is Fauna)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedEntity as Fauna,
                    selectableOrganicResources,
                    lifeformNames[LifeformType.Fauna]
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFauna = dialog.GetResultingFauna();
                    DataRepository.EditFauna(displayedEntity as Fauna, resultingFauna);
                    displayedCelestialBody.EditFauna(resultingFauna);
                    DisplayFaunaDetails(resultingFauna);
                    if (selectedEntity != null)
                    {
                        selectedEntity = resultingFauna;
                        faunasListView.SelectedItem = selectedEntity;
                    }

                    FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                    ReapplyFilters();
                }
            }
            else if (displayedEntity is Flora)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedEntity as Flora,
                    selectableOrganicResources,
                    lifeformNames[LifeformType.Flora]
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFlora = dialog.GetResultingFlora();
                    DataRepository.EditFlora(displayedEntity as Flora, resultingFlora);
                    displayedCelestialBody.EditFlora(resultingFlora);
                    DisplayFloraDetails(resultingFlora);
                    if (selectedEntity != null)
                    {
                        selectedEntity = resultingFlora;
                        florasListView.SelectedItem = selectedEntity;
                    }

                    FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                    ReapplyFilters();
                }
            }
            else // assume outpost
            {
                OutpostEditor dialog = new OutpostEditor(displayedEntity as Outpost);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingOutpost = dialog.GetResultingOutpost();
                    DataRepository.EditOutpost(resultingOutpost);
                    displayedCelestialBody.EditOutpost(resultingOutpost);
                    DisplayOutpostDetails(resultingOutpost);
                    if (selectedEntity != null)
                    {
                        selectedEntity = resultingOutpost;
                        outpostsListView.SelectedItem = selectedEntity;
                    }
                }
            }
        }

        private void FaunaListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Fauna)
            {
                var fauna = listViewItem.DataContext as Fauna;
                if (selectedEntity == null)
                {
                    DisplayFaunaDetails(fauna);
                }

                App.Current.PlayScrollSound();
            }
        }

        private void FaunaListView_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            Fauna fauna = clickedItem.DataContext as Fauna;
            ToggleSelectFauna(fauna, clickedItem);
            e.Handled = true;
            AnalyticsUtil.TrackEvent("Select fauna");
        }

        // find solar systems where solar system name or a child celestial body name matches filterText
        // ignores case, whitespace, symbols
        private void FilterSolarSystems(string filterText)
        {
            filterText = new string(filterText.Where(char.IsLetter).ToArray()).ToLower();
            ICollectionView view = CollectionViewSource.GetDefaultView(solarSystemsListView.ItemsSource);
            view.Filter = item =>
            {
                if (item is SolarSystem solarSystem)
                {
                    return new string(solarSystem.SystemName.Where(char.IsLetter).ToArray()).ToLower().Contains(filterText)
                    || solarSystem.CelestialBodies.Where(
                        cb => new string(cb.BodyName.Where(char.IsLetter).ToArray()).ToLower().Contains(filterText)).Any();
                }
                return false;
            };

            recoverCelestialBodySelection();
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

        private CelestialBody? FindOriginalDisplayedCelestialBody()
        {
            // TODO: this is a hack. need to find a better way to manage current view models
            // find and update original celestial body inside discoveredSolarSystems,
            // because displayedCelestialBody may be a copy from an applied filter
            foreach (var solarSystem in discoveredSolarSystems)
            {
                foreach (var celestialBody in solarSystem.CelestialBodies)
                {
                    if (celestialBody != displayedCelestialBody && celestialBody.Equals(displayedCelestialBody))
                    {
                        return celestialBody;
                    }
                }
            }

            return null;
        }

        private void FloraListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Flora)
            {
                var flora = listViewItem.DataContext as Flora;
                if (selectedEntity == null)
                {
                    DisplayFloraDetails(flora);
                }

                App.Current.PlayScrollSound();
            }
        }

        private void FloraListView_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            Flora flora = clickedItem.DataContext as Flora;
            ToggleSelectFlora(flora, clickedItem);
            e.Handled = true;
            AnalyticsUtil.TrackEvent("Select flora");
        }

        private void InitializeSolarSystemsListView(object sender, RoutedEventArgs e)
        {
            // restore last session's celestial body selection
            try
            {
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
                                var selectedOutpostID = Settings.Default.SelectedOutpostID;
                                if (selectedFaunaID != -1)
                                {
                                    var fauna = celestialBody.Faunas.First(f => f.ID == selectedFaunaID);
                                    SetSelectedFaunaWithUI(fauna);
                                }
                                else if (selectedFloraID != -1)
                                {
                                    var flora = celestialBody.Floras.First(f => f.ID == selectedFloraID);
                                    SetSelectedFloraWithUI(flora);
                                }
                                else if (selectedOutpostID != -1)
                                {
                                    var outpost = celestialBody.Outposts.First(f => f.ID == selectedOutpostID);
                                    SetSelectedOutpostWithUI(outpost);
                                }

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnalyticsUtil.TrackError(ex);

                // fail silently if initial UI cached state fails to load
                if (Debugger.IsAttached)
                {
                    throw;
                }
                else
                {
                    Settings.Default.Reset();
                }
            }

            // only need to run once
            solarSystemsListView.Loaded -= InitializeSolarSystemsListView;
        }

        private void lifeformFilter_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;

            // toggle
            clickedMenuItem.IsChecked = !clickedMenuItem.IsChecked;

            if (clickedMenuItem.IsChecked)
            {
                App.Current.PlayClickSound();

                // remove other filters
                outpostFilter_MenuItem.IsChecked = false;

                // disable search box
                solarSystemFilterTextBox.Text = "(Filter for Lifeforms)";
                solarSystemFilterTextBox.IsEnabled = false;

                ApplyLifeformFilter();
            }
            else
            {
                App.Current.PlayCancelSound();
                solarSystemsListView.ItemsSource = discoveredSolarSystems;
                solarSystemFilterTextBox.Text = "";
                solarSystemFilterTextBox.IsEnabled = true;
            }
        }

        private void OutpostDeleteClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            var outpost = ((MenuItem)sender).DataContext as Outpost;

            var confirmDialog = new BasicYesNoDialog(
                "Delete Outpost",
                "You are about to delete this outpost:\n\n" +
                $"{outpost.Name}\n\n" +
                "Are you sure? (This is reversible. Check the GitHub Wiki for instructions.)",
                "Delete",
                "Cancel"
            );
            confirmDialog.Owner = Window.GetWindow(this);

            if (confirmDialog.ShowDialog() == true)
            {
                DataRepository.DeleteOutpost(outpost.ID);
                displayedCelestialBody.DeleteOutpost(outpost);
                ClearAllSelectionsExcept();
                ResetEntityOverview();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                ReapplyFilters();
            }
        }

        private void outpostFilter_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;

            // toggle
            clickedMenuItem.IsChecked = !clickedMenuItem.IsChecked;

            if (clickedMenuItem.IsChecked)
            {
                App.Current.PlayClickSound();

                // remove other filters
                lifeformFilter_MenuItem.IsChecked = false;

                // disable search box
                solarSystemFilterTextBox.Text = "(Filter for Outposts)";
                solarSystemFilterTextBox.IsEnabled = false;

                ApplyOutpostsFilter();
            }
            else
            {
                App.Current.PlayCancelSound();
                solarSystemsListView.ItemsSource = discoveredSolarSystems;
                solarSystemFilterTextBox.Text = "";
                solarSystemFilterTextBox.IsEnabled = true;
            }
        }

        private void OutpostListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Outpost)
            {
                var outpost = listViewItem.DataContext as Outpost;
                if (selectedEntity == null)
                {
                    DisplayOutpostDetails(outpost);
                }

                App.Current.PlayScrollSound();
            }
        }

        private void OutpostListView_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            Outpost outpost = clickedItem.DataContext as Outpost;
            ToggleSelectOutpost(outpost, clickedItem);
            e.Handled = true;
            AnalyticsUtil.TrackEvent("Select outpost");
        }

        private void ReapplyFilters()
        {
            if (outpostFilter_MenuItem.IsChecked)
            {
                ApplyOutpostsFilter();
            }
            else if (lifeformFilter_MenuItem.IsChecked)
            {
                ApplyLifeformFilter();
            }
        }

        private void recoverCelestialBodySelection()
        {
            // wait for UI to load, then attempt to recover selected item
            solarSystemsListView.UpdateLayout();

            // find the solar system based on selectedCelestialBody
            var containerGenerator = solarSystemsListView.ItemContainerGenerator;
            var selectedSolarSystem = containerGenerator.Items.FirstOrDefault(
                solarSystem => ((SolarSystem)solarSystem).CelestialBodies.Contains(selectedCelestialBody)
            );

            if (selectedSolarSystem != null)
            {
                var solarSystemListViewItem = containerGenerator.ContainerFromItem(selectedSolarSystem);
                ListView celestialBodyListView = FindNestedListView(solarSystemListViewItem);
                celestialBodyListView.SelectedItem = selectedCelestialBody;
            }
        }

        private void RefreshData()
        {
            selectedEntity = null;
            var solarSystems = DataRepository.GetSolarSystems();
            allSolarSystems = solarSystems;
            discoveredSolarSystems = solarSystems
                .Where(solarSystem => solarSystem.Discovered)
                .ToList();

            solarSystemsListView.ItemsSource = discoveredSolarSystems;
        }

        private void ResetEntityOverview()
        {
            displayedEntity = null;
            entityOverviewGrid.Visibility = Visibility.Hidden;
            editEntityButton.IsEnabled = false;
        }

        private void resetFilter_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.Current.PlayCancelSound();
            ResetFilters();
        }

        private void ResetFilters()
        {
            lifeformFilter_MenuItem.IsChecked = false;
            outpostFilter_MenuItem.IsChecked = false;
            solarSystemsListView.ItemsSource = discoveredSolarSystems;
            solarSystemFilterTextBox.Text = "";
            solarSystemFilterTextBox.IsEnabled = true;
        }

        private void SetCelestialBodyOnLayoutUpdate(object sender, EventArgs e)
        {
            SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            solarSystemsListView.LayoutUpdated -= SetCelestialBodyOnLayoutUpdate;
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
                foreach (var item in solarSystemsListView.ItemContainerGenerator.Items)
                {
                    var solarSystem = (SolarSystem)item;
                    if (solarSystem.CelestialBodies.Contains(celestialBody))
                    {
                        selectedSolarSystem = solarSystem;
                        break;
                    }
                }
            }

            // only proceed if the displayed list contains the selected celestial body
            if (selectedSolarSystem == null)
            {
                return;
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

        private void SetSelectedFaunaWithUI(Fauna fauna)
        {
            if (fauna == null)
            {
                throw new Exception("SetSelectedFauna() is only for non-null values. Use ClearFaunaSelection() otherwise.");
            }

            ClearAllSelectionsExcept(fauna);
            selectedEntity = fauna;
            faunasListView.SelectedItem = fauna;
            DisplayFaunaDetails(fauna);

            Settings.Default.SelectedFaunaID = fauna.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (selectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            }
        }

        private void SetSelectedFloraWithUI(Flora flora)
        {
            if (flora == null)
            {
                throw new Exception("SetSelectedFlora() is only for non-null values. Use ClearFloraSelection() otherwise.");
            }

            ClearAllSelectionsExcept(flora);
            selectedEntity = flora;
            florasListView.SelectedItem = flora;
            DisplayFloraDetails(flora);

            Settings.Default.SelectedFloraID = flora.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (selectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            }
        }

        private void SetSelectedOutpostWithUI(Outpost outpost)
        {
            if (outpost == null)
            {
                throw new Exception("SetSelectedOutpost() is only for non-null values. Use ClearOutpostSelection() otherwise.");
            }

            ClearAllSelectionsExcept(outpost);
            selectedEntity = outpost;
            outpostsListView.SelectedItem = outpost;
            DisplayOutpostDetails(outpost);

            Settings.Default.SelectedOutpostID = outpost.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (selectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(displayedCelestialBody);
            }
        }

        private void SolarSystemFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (!outpostFilter_MenuItem.IsChecked)
            {
                FilterSolarSystems(solarSystemFilterTextBox.Text);
            }
        }

        private void ToggleSelectCelestialBody(CelestialBody celestialBody, ListView parent, ListViewItem clickedItem)
        {
            if (celestialBody.Equals(selectedCelestialBody))
            {
                App.Current.PlayCancelSound();

                SetSelectedCelestialBody(null);
                parent.UnselectAll();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                SetSelectedCelestialBody(celestialBody);
                parent.SelectedItem = celestialBody;
                clickedItem.IsSelected = true;
                clickedItem.Focus();
                Framework.ClearInnerListViews(solarSystemsListView, parent);
                DisplayCelestialBodyDetails(celestialBody);
                ClearAllSelectionsExcept();
            }
        }

        #endregion CELESTIAL BODIES -----------------------------------------------------------------------------------------------

        #region FLORA / FAUNA -----------------------------------------------------------------------------------------------

        private void ToggleSelectFauna(Fauna fauna, ListViewItem clickedItem)
        {
            if (selectedEntity == fauna)
            {
                App.Current.PlayCancelSound();

                // like ClearFaunaSelection but keep displayedFauna
                selectedEntity = null;
                faunasListView.UnselectAll();
                Settings.Default.SelectedFaunaID = -1;
                Settings.Default.Save();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                clickedItem.IsSelected = true;
                clickedItem.Focus();
                SetSelectedFaunaWithUI(fauna);
            }
        }

        private void ToggleSelectFlora(Flora flora, ListViewItem clickedItem)
        {
            if (selectedEntity == flora)
            {
                App.Current.PlayCancelSound();

                // like ClearFloraSelection but keep displayedFlora
                selectedEntity = null;
                florasListView.UnselectAll();
                Settings.Default.SelectedFloraID = -1;
                Settings.Default.Save();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                clickedItem.IsSelected = true;
                clickedItem.Focus();
                SetSelectedFloraWithUI(flora);
            }
        }

        #endregion FLORA / FAUNA -----------------------------------------------------------------------------------------------

        #region OUTPOSTS -----------------------------------------------------------------------------------------------

        private void ToggleSelectOutpost(Outpost outpost, ListViewItem clickedItem)
        {
            if (selectedEntity == outpost)
            {
                App.Current.PlayCancelSound();

                // like ClearOutpostSelection but keep displayedOutpost
                selectedEntity = null;
                outpostsListView.UnselectAll();
                Settings.Default.SelectedOutpostID = -1;
                Settings.Default.Save();
                clickedItem.IsSelected = false;
            }
            else
            {
                App.Current.PlayClickSound();

                clickedItem.IsSelected = true;
                clickedItem.Focus();
                SetSelectedOutpostWithUI(outpost);
            }
        }

        #endregion OUTPOSTS -----------------------------------------------------------------------------------------------

        #region PICTURES -----------------------------------------------------------------------------------------------

        private void AddPicture(Uri importedPictureUri)
        {
            int pictureID;

            if (displayedEntity is Fauna)
            {
                pictureID = DataRepository.AddFaunaPicture(displayedEntity as Fauna, importedPictureUri.LocalPath);
            }
            else if (displayedEntity is Flora)
            {
                pictureID = DataRepository.AddFloraPicture(displayedEntity as Flora, importedPictureUri.LocalPath);
            }
            else if (displayedEntity is Outpost)
            {
                pictureID = DataRepository.AddOutpostPicture(displayedEntity as Outpost, importedPictureUri.LocalPath);
            }
            else
            {
                return;
            }

            displayedEntity.AddPicture(new Picture(pictureID, importedPictureUri));
        }

        private void AddPictureDragEnter(object sender, DragEventArgs e)
        {
            if (displayedEntity != null)
            {
                entityOverviewGrid.Opacity = 0.2;
                dragDropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void AddPictureDragLeave(object sender, DragEventArgs e)
        {
            entityOverviewGrid.Opacity = 1;
            dragDropOverlay.Visibility = Visibility.Hidden;
        }

        private void AddPictureOnDrop(object sender, DragEventArgs e)
        {
            if (displayedEntity == null)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> allowedExtensions = new List<string> { ".jpg", ".png", ".jpeg", ".bmp", ".tiff", ".tif" };
                var importSuccess = false;

                foreach (string file in files)
                {
                    string fileExtension = Path.GetExtension(file).ToLower();

                    if (allowedExtensions.Contains(fileExtension))
                    {
                        var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedEntity, picture: new Uri(file));
                        AddPicture(importedPictureUri);
                        importSuccess = true;
                    }
                    else
                    {
                        MessageBox.Show($"File '{file}' has an unsupported extension!");
                    }
                }

                if (importSuccess)
                {
                    App.Current.PlayClickSound();
                }
            }

            entityOverviewGrid.Opacity = 1;
            dragDropOverlay.Visibility = Visibility.Hidden;
        }

        private void PictureClicked(object sender, RoutedEventArgs e)
        {
            var picture = ((Border)sender).DataContext as Picture;
            if (picture.IsPlaceholder)
            {
                App.Current.PlayClickSound();

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Images | *.jpg; *.png; *.jpeg; *.bmp; *.tiff; *.tif";
                if (openFileDialog.ShowDialog() == true)
                {
                    var newPictureUri = new Uri(openFileDialog.FileName);
                    var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedEntity, picture: newPictureUri);
                    AddPicture(importedPictureUri);

                    App.Current.PlayClickSound();
                }
                else
                {
                    App.Current.PlayCancelSound();
                }
            }
            else if (!picture.Corrupted)
            {
                AnalyticsUtil.TrackEvent("View picture");

                App.Current.PlayClickSound();

                var viewer = new PictureViewer(picture);
                viewer.Owner = Window.GetWindow(this);

                // account for height of title bar
                double heightBuffer = SystemParameters.WindowCaptionHeight + 20;
                double widthBuffer = 20;

                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                // picture width/height should not take up more than 80% of screen
                double maxRatio = 0.8;
                double maxWidth = screenWidth * maxRatio;
                double maxHeight = screenHeight * maxRatio;

                double imageWidth = picture.PictureBitmap.Width;
                double imageHeight = picture.PictureBitmap.Height;

                double imageWidthToScreenRatio = imageWidth / screenWidth;
                double imageHeightToScreenRatio = imageHeight / screenHeight;

                if (imageWidthToScreenRatio <= maxRatio && imageHeightToScreenRatio <= maxRatio)
                {
                    viewer.Width = picture.PictureBitmap.Width + widthBuffer;
                    viewer.Height = picture.PictureBitmap.Height + heightBuffer;
                }
                else if (imageWidthToScreenRatio > imageHeightToScreenRatio)
                {
                    viewer.Width = maxWidth + widthBuffer;
                    double scaleRatio = maxWidth / imageWidth;
                    viewer.Height = (imageHeight * scaleRatio) + heightBuffer + 10;
                }
                else
                {
                    viewer.Height = maxHeight + heightBuffer - 10;
                    double scaleRatio = maxHeight / imageHeight;
                    viewer.Width = (imageWidth * scaleRatio) + widthBuffer;
                }

                viewer.Show();
                activePictureViewer = viewer;
                activePictureViewer.Closed += (s, args) => { activePictureViewer = null; };
            }
        }

        private void PictureDeleteClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            var picture = ((MenuItem)sender).DataContext as Picture;
            if (displayedEntity is Fauna)
            {
                DataRepository.DeleteFaunaPicture(picture);
            }
            else if (displayedEntity is Flora)
            {
                DataRepository.DeleteFloraPicture(picture);
            }
            else // assume outpost
            {
                DataRepository.DeleteOutpostPicture(picture);
            }
            displayedEntity.RemovePicture(picture);
            picture.MoveToDeletedFolder();
        }

        private void PictureGridLoaded(object sender, RoutedEventArgs e)
        {
            ResizePictureGridColumns();
        }

        private void PictureOpenFolderClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            var picture = ((MenuItem)sender).DataContext as Picture;
            var folder = System.IO.Path.GetDirectoryName(picture.PictureUri.LocalPath);
            if (Directory.Exists(folder))
            {
                Process.Start("explorer.exe", folder);
            }
        }

        private void ResizePictureGridColumns()
        {
            // calculate the number of columns to use
            double availableWidth = (double)entityOverviewGrid.ActualWidth;
            double itemWidth = 110; // fixed width + margin of each thumbnail
            double columns = availableWidth / itemWidth;
            int columnsInt = (int)Math.Floor(columns);
            columnsInt = Math.Max(columnsInt, 1);

            PictureGridColumns = columnsInt;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PictureGridColumns)));
        }

        #endregion PICTURES -----------------------------------------------------------------------------------------------

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {

            Debug.WriteLine($"KEY:{e.Key}   MOD:{Keyboard.Modifiers}");
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && displayedEntity != null)
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource clipboardImage = Clipboard.GetImage();
                    var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedEntity, directSource: clipboardImage);
                    AddPicture(importedPictureUri);
                    App.Current.PlayClickSound();
                }
            }
        }

        private void UserControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (activePictureViewer != null)
            {
                activePictureViewer.Close();
                activePictureViewer = null;
                e.Handled = true;
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizePictureGridColumns();
        }
    }
}