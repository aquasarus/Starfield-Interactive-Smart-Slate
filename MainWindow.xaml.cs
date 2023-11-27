using Microsoft.Win32;
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
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Starfield_Interactive_Smart_Slate
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string DisplayedCelestialBodyName { get { return displayedCelestialBody?.BodyName; } }
        public string DisplayedSolarSystemName { get { return displayedCelestialBody?.SystemName; } }
        public int PictureGridColumns { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        private List<SolarSystem> allSolarSystems;
        private List<SolarSystem> discoveredSolarSystems;
        private Dictionary<LifeformType, Dictionary<string, string>> lifeformNames;
        private List<Resource> selectableOrganicResources;

        private CelestialBody selectedCelestialBody;
        private CelestialBody displayedCelestialBody;

        private Entity? selectedEntity;
        private Entity? displayedEntity;

        private CelestialBody selectedOrganicResultCelestialBody;
        private CelestialBody displayedOrganicResultCelestialBody;

        private Window? activePictureViewer = null;

        private DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            EnableSoundsCheckBox.DataContext = App.Current.UserSettings;
            EnableAnalyticsCheckBox.DataContext = App.Current.UserSettings;
            EnableUpdateNotificationCheckBox.DataContext = App.Current.UserSettings;

            // show version number
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            string versionNumber = $"Version {version.Major}.{version.Minor}.{version.Build}";
            VersionNumberLabel.Content = versionNumber;

            var resources = DataRepository.GetResources();

            inorganicResourceListView.ItemsSource = resources.Where(r => r.GetType() == ResourceType.Inorganic);
            selectableOrganicResources = resources.Where(r =>
            {
                return r.GetType() == ResourceType.Organic || r.GetType() == ResourceType.Placeholders;
            }).ToList();
            organicResourceListView.ItemsSource = resources.Where(r => r.GetType() == ResourceType.Organic);

            lifeformNames = DataRepository.GetLifeformNames();
            celestialBodyTitleLabel.DataContext = this;
            celestialBodyMiniTitleLabel.DataContext = this;

            RefreshData();

            solarSystemsListView.Loaded += InitializeSolarSystemsListView;

            pictureGrid.DataContext = this;

            CheckForUpdate();

            // check for updates once a day in case the user never closes the app
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromDays(1);
            updateTimer.Tick += Timer_Tick;
            updateTimer.Start();

            // initialize user ID label
            UserIDLabel.Content = $"User ID: {DataRepository.UserID}";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckForUpdate();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizePictureGridColumns();
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

        private void TabClicked(object sender, MouseButtonEventArgs e)
        {
            if (!((TabItem)sender).IsSelected)
            {
                App.Current.PlayClickSound();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.Current.UserSettings.HasShownAnalyticsPopup)
            {
                var analyticsPermissionPopup = new BasicYesNoDialog("Analytics",
                    "To continue improving this app, I'd love to know how many people are actually using it. " +
                        "Will you allow me to collect some anonymous analytics data from this app?\n\n" +
                        $"You will be identified as:\n{DataRepository.UserID}",
                    "Okay",
                    "Opt Out");
                analyticsPermissionPopup.Owner = this;
                analyticsPermissionPopup.ShowDialog();
                if (analyticsPermissionPopup.ExplicitNo)
                {
                    App.Current.UserSettings.EnableAnalytics = false;
                }

                // also show first launch tutorial
                var welcomeDialog = new BasicYesNoDialog("Quick Start",
                    "Welcome to Starfield ISS!\n\n" +
                    "This is an exploration compendium for you to catalog your own survey data. " +
                    "To get started, click Discover New System to reveal your first solar system.",
                    "Got it");
                welcomeDialog.Owner = this;
                welcomeDialog.ShowDialog();

                App.Current.UserSettings.HasShownAnalyticsPopup = true;
            }


            if (ActualHeight >= SystemParameters.PrimaryScreenHeight ||
                ActualWidth >= SystemParameters.PrimaryScreenWidth)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!((TabItem)sender).IsSelected)
            {
                App.Current.PlayScrollSound();
            }
        }

        // -----------------------------------------------------------------------------------------------
        // CELESTIAL BODIES
        // -----------------------------------------------------------------------------------------------
        #region
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

        private void SolarSystemFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (!outpostFilter_MenuItem.IsChecked)
            {
                FilterSolarSystems(solarSystemFilterTextBox.Text);
            }
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

            recoverCelestialBodySelection();
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
                    ClearAllSelectionsExcept();
                }

                App.Current.PlayScrollSound();
            }
        }

        private void CelestialBodyListItem_Select(object sender, MouseEventArgs e)
        {
            ListViewItem clickedItem = sender as ListViewItem;
            CelestialBody celestialBody = clickedItem.DataContext as CelestialBody;
            ListView parent = FindParentListView(clickedItem);
            ToggleSelectCelestialBody(celestialBody, parent, clickedItem);
            e.Handled = true;
            AnalyticsUtil.TrackEvent("Select celestial body");
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
                ClearInnerListViews(solarSystemsListView, parent);
                DisplayCelestialBodyDetails(celestialBody);
                ClearAllSelectionsExcept();
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

        private void DiscoverNewSystemClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            List<SolarSystem> alphabeticalSystems = allSolarSystems
                .Where(solarSystem => !solarSystem.Discovered)
                .OrderBy(solarSystem => solarSystem.SystemName).ToList();

            SolarSystemSelector dialog = new SolarSystemSelector(alphabeticalSystems);

            dialog.Owner = this;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                SolarSystem selectedSolarSystem = dialog.SelectedSolarSystem;
                DataRepository.DiscoverSolarSystem(selectedSolarSystem);
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

        private void celestialBodiesFilterButton_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void celestialBodiesFilterButton_ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
        }

        private void outpostFilter_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem clickedMenuItem = sender as MenuItem;

            // toggle
            clickedMenuItem.IsChecked = !clickedMenuItem.IsChecked;

            if (clickedMenuItem.IsChecked)
            {
                // disable search box
                solarSystemFilterTextBox.Text = "(Filter for Outposts)";
                solarSystemFilterTextBox.IsEnabled = false;

                var outpostSolarSystems = discoveredSolarSystems.Select(
                    solarSystem =>
                    {
                        var solarSystemCopy = new SolarSystem
                        {
                            SystemID = solarSystem.SystemID,
                            SystemName = solarSystem.SystemName,
                            SystemLevel = solarSystem.SystemLevel,
                            Discovered = solarSystem.Discovered,
                            // if celestial body (or any of its moons) has an outpost, include it in the list
                            CelestialBodies = solarSystem.CelestialBodies.Select(
                                celestialBody =>
                                {
                                    var surfaceHasOutpost = celestialBody.HasOutpost;
                                    var moonsHaveOutposts = celestialBody.Moons?.Any(moon => moon.HasOutpost) ?? false;

                                    if (surfaceHasOutpost || moonsHaveOutposts)
                                    {
                                        if (!surfaceHasOutpost)
                                        {
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
                            ).Where(celestialBody => celestialBody.Show)
                            .ToList()
                        };

                        return solarSystemCopy;
                    }
                ).Where(solarSystem => solarSystem.CelestialBodies.Any());
                solarSystemsListView.ItemsSource = outpostSolarSystems;

                recoverCelestialBodySelection();
            }
            else
            {
                solarSystemsListView.ItemsSource = discoveredSolarSystems;
                solarSystemFilterTextBox.Text = "";
                solarSystemFilterTextBox.IsEnabled = true;
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
        #endregion

        // -----------------------------------------------------------------------------------------------
        // FLORA / FAUNA
        // -----------------------------------------------------------------------------------------------
        #region
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

        private void ClearFaunaSelection()
        {
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

        private void DisplayFaunaDetails(Fauna fauna)
        {
            entitySubtitleLabel.Content = "· Fauna";

            lifeformResourceTitleLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Content = fauna.ResourceString;

            DisplayEntityDetails(fauna);
        }

        private void AddFaunaClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Fauna], LifeformType.Fauna);

            dialog.Owner = this;
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
            }
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

        private void ClearFloraSelection()
        {
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

        private void DisplayFloraDetails(Flora flora)
        {
            entitySubtitleLabel.Content = "· Flora";

            lifeformResourceTitleLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Visibility = Visibility.Visible;
            lifeformResourceLabel.Content = flora.ResourceString;

            DisplayEntityDetails(flora);
        }

        private void AddFloraClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(lifeformNames[LifeformType.Flora], LifeformType.Flora);

            dialog.Owner = this;
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
            }
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
                dialog.Owner = this;
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
                }
            }
            else if (displayedEntity is Flora)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedEntity as Flora,
                    selectableOrganicResources,
                    lifeformNames[LifeformType.Flora]
                );
                dialog.Owner = this;
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
                }
            }
            else // assume outpost
            {
                OutpostEditor dialog = new OutpostEditor(displayedEntity as Outpost);
                dialog.Owner = this;
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
        #endregion

        // -----------------------------------------------------------------------------------------------
        // OUTPOSTS
        // -----------------------------------------------------------------------------------------------
        #region
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

        private void AddOutpostClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddOutpostDialog dialog = new AddOutpostDialog();

            dialog.Owner = this;
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
            }
        }

        private void DisplayOutpostDetails(Outpost outpost)
        {
            entitySubtitleLabel.Content = "· Outpost";

            // outposts have no resource drops
            lifeformResourceTitleLabel.Visibility = Visibility.Collapsed;
            lifeformResourceLabel.Visibility = Visibility.Collapsed;

            DisplayEntityDetails(outpost);
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

        private void ResetEntityOverview()
        {
            displayedEntity = null;
            entityOverviewGrid.Visibility = Visibility.Hidden;
            editEntityButton.IsEnabled = false;
        }

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

        private void ClearOutpostSelection()
        {
            outpostsListView.UnselectAll();
            Settings.Default.SelectedOutpostID = -1;
            Settings.Default.Save();
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
            confirmDialog.Owner = this;

            if (confirmDialog.ShowDialog() == true)
            {
                DataRepository.DeleteOutpost(outpost.ID);
                displayedCelestialBody.DeleteOutpost(outpost);
                ClearAllSelectionsExcept();
                ResetEntityOverview();
            }
        }
        #endregion

        // -----------------------------------------------------------------------------------------------
        // PICTURES
        // -----------------------------------------------------------------------------------------------
        #region
        private void PictureGridLoaded(object sender, RoutedEventArgs e)
        {
            ResizePictureGridColumns();
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
                viewer.Owner = this;

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
                    viewer.Width = imageWidth * scaleRatio + widthBuffer;
                }

                viewer.Show();
                activePictureViewer = viewer;
                activePictureViewer.Closed += (s, args) => { activePictureViewer = null; };
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (activePictureViewer != null)
            {
                activePictureViewer.Close();
                activePictureViewer = null;
                e.Handled = true;
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
            else
            {
                DataRepository.DeleteFloraPicture(picture);
            }
            displayedEntity.Pictures.Remove(picture);
            picture.MoveToDeletedFolder();
        }

        private void PictureOpenFolderClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            var picture = ((MenuItem)sender).DataContext as Picture;
            var folder = Path.GetDirectoryName(picture.PictureUri.LocalPath);
            if (Directory.Exists(folder))
            {
                Process.Start("explorer.exe", folder);
            }
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
        #endregion

        // -----------------------------------------------------------------------------------------------
        // RESOURCE SEARCH
        // -----------------------------------------------------------------------------------------------
        #region
        private void ResourceSearchListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            App.Current.PlayScrollSound();
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
                    var solarSystemCopy = solarSystem.DeepCopy(true);

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
                App.Current.PlayScrollSound();
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
        // SETTINGS PAGE
        // -----------------------------------------------------------------------------------------------
        #region
        private void EnableSoundsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableSounds)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void EnableAnalyticsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableAnalytics)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }

        private void EnableUpdateNotificationCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.UserSettings.EnableUpdateNotification)
            {
                App.Current.PlayClickSound();
            }
            else
            {
                App.Current.PlayCancelSound();
            }
        }
        #endregion

        // -----------------------------------------------------------------------------------------------
        // ABOUT PAGE
        // -----------------------------------------------------------------------------------------------
        #region
        private void NavigateToHyperlink(object sender, RequestNavigateEventArgs e)
        {
            App.Current.PlayClickSound();
            LaunchHyperlink(e.Uri.ToString());
            e.Handled = true;
        }

        private void DataFolderLinkClick(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
            Process.Start("explorer.exe", DatabaseInitializer.UserDatabaseFolder());
        }

        private void CopyUserID(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();
            Clipboard.SetText(DataRepository.UserID);
            MessageBox.Show($"User ID {DataRepository.UserID} has been copied to clipboard!", "Copied");
        }

        private void LaunchHyperlink(string hyperlink)
        {
            Process.Start(new ProcessStartInfo(hyperlink)
            {
                UseShellExecute = true // need to set this to get web links to work here
            });
        }

        private async void CheckForUpdate()
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response =
                    await client.GetAsync("https://github.com/aquasarus/Starfield-Interactive-Smart-Slate");
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync();

                var pattern = "(Releases).*?tag\\/v(\\d+\\.\\d+\\.\\d+).*?(Label: Latest)";
                Match match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline);
                if (match.Success)
                {
                    Version latestVersion = Version.Parse(match.Groups[2].Value);
                    Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                    if (latestVersion.CompareTo(currentVersion) > 0)
                    {
                        AnalyticsUtil.TrackEvent("Found app update");
                        NewVersionAvailableHyperlink.Inlines.Clear();
                        NewVersionAvailableHyperlink.Inlines.Add($"(New Version v{latestVersion} Available)");
                        NewVersionAvailableLabel.Visibility = Visibility.Visible;

                        NewVersionAvailableSettingsHyperlink.Inlines.Clear();
                        NewVersionAvailableSettingsHyperlink.Inlines.Add($"> New Version v{latestVersion} Available");
                        NewVersionAvailableSettingsLabel.Visibility = Visibility.Visible;

                        // don't clash with the analytics popup
                        if (App.Current.UserSettings.EnableUpdateNotification
                            && App.Current.UserSettings.HasShownAnalyticsPopup)
                        {
                            var latestVersionString = $"v{latestVersion.Major}.{latestVersion.Minor}.{latestVersion.Build}";
                            var currentVersionString = $"v{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}";
                            var newVersionNotification = new BasicYesNoDialog("New Version Available",
                                $"There is a newer version available on GitHub!\n" +
                                $"Wanna check it out?\n\n" +
                                $"Your current version is: {currentVersionString}\n" +
                                $"The latest version is: {latestVersionString}\n\n" +
                                $"This dialog can be disabled in the Settings tab.",
                                "Yes",
                                "No");
                            newVersionNotification.Owner = this;
                            if (newVersionNotification.ShowDialog() == true)
                            {
                                LaunchHyperlink("https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/releases");
                            }
                        }
                    }
                }
                else
                {
                    AnalyticsUtil.TrackEvent("Did not find latest release on GitHub");
                }
            }
            catch (Exception ex)
            {
                AnalyticsUtil.TrackError(ex);
                if (Debugger.IsAttached) { throw; }
            }
        }
        #endregion
    }
}
