using Microsoft.Win32;
using Starfield_Interactive_Smart_Slate.Dialogs;
using Starfield_Interactive_Smart_Slate.Models;
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

        private Fauna selectedFauna;
        private Fauna displayedFauna;

        private Flora selectedFlora;
        private Flora displayedFlora;

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
            selectedFauna = null;
            selectedFlora = null;
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
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource clipboardImage = Clipboard.GetImage();
                    var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedFauna, displayedFlora, directSource: clipboardImage);

                    if (displayedFauna != null)
                    {
                        var pictureID = DataRepository.AddFaunaPicture(displayedFauna, importedPictureUri.LocalPath);
                        displayedFauna.AddPicture(new Picture(pictureID, importedPictureUri));
                    }
                    else if (displayedFlora != null)
                    {
                        var pictureID = DataRepository.AddFloraPicture(displayedFlora, importedPictureUri.LocalPath);
                        displayedFlora.AddPicture(new Picture(pictureID, importedPictureUri));
                    }

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
                App.Current.UserSettings.HasShownAnalyticsPopup = true;
            }


            if (ActualHeight >= SystemParameters.PrimaryScreenHeight ||
                ActualWidth >= SystemParameters.PrimaryScreenWidth)
            {
                WindowState = WindowState.Maximized;
            }
        }

        // -----------------------------------------------------------------------------------------------
        // CELESTIAL BODIES
        // -----------------------------------------------------------------------------------------------
        #region Celestial Body Stuff
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
            }
            catch (Exception exception)
            {
                AnalyticsUtil.TrackError(exception);

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
            if (selectedFauna == fauna)
            {
                App.Current.PlayCancelSound();

                // like ClearFaunaSelection but keep displayedFauna
                selectedFauna = null;
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

            pictureGrid.ItemsSource = fauna.Pictures;
            lifeformOverviewScrollViewer.ScrollToTop();
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

                editLifeformButton.Focus();
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
            if (selectedFlora == flora)
            {
                App.Current.PlayCancelSound();

                // like ClearFloraSelection but keep displayedFlora
                selectedFlora = null;
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

            pictureGrid.ItemsSource = flora.Pictures;
            lifeformOverviewScrollViewer.ScrollToTop();
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

                editLifeformButton.Focus();
            }
        }

        private void EditLifeformClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            if (displayedFauna != null)
            {
                LifeformEditor dialog = new LifeformEditor(
                    displayedFauna,
                    selectableOrganicResources,
                    lifeformNames[LifeformType.Fauna]
                );

                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFauna = dialog.GetResultingFauna();
                    DataRepository.EditFauna(displayedFauna, resultingFauna);
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
                    selectableOrganicResources,
                    lifeformNames[LifeformType.Flora]
                );

                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFlora = dialog.GetResultingFlora();
                    DataRepository.EditFlora(displayedFlora, resultingFlora);
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

        private void PictureGridLoaded(object sender, RoutedEventArgs e)
        {
            ResizePictureGridColumns();
        }

        private void ResizePictureGridColumns()
        {
            // calculate the number of columns to use
            double availableWidth = (double)lifeformOverviewGrid.ActualWidth;
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
                    var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedFauna, displayedFlora, picture: newPictureUri);

                    if (displayedFauna != null)
                    {
                        var pictureID = DataRepository.AddFaunaPicture(displayedFauna, importedPictureUri.LocalPath);
                        displayedFauna.AddPicture(new Picture(pictureID, importedPictureUri));
                    }
                    else if (displayedFlora != null)
                    {
                        var pictureID = DataRepository.AddFloraPicture(displayedFlora, importedPictureUri.LocalPath);
                        displayedFlora.AddPicture(new Picture(pictureID, importedPictureUri));
                    }

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
            if (displayedFauna != null)
            {
                DataRepository.DeleteFaunaPicture(picture);
                displayedFauna.Pictures.Remove(picture);
            }
            else
            {
                DataRepository.DeleteFloraPicture(picture);
                displayedFlora.Pictures.Remove(picture);
            }
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
            lifeformOverviewGrid.Opacity = 0.2;
            dragDropOverlay.Visibility = Visibility.Visible;
        }

        private void AddPictureDragLeave(object sender, DragEventArgs e)
        {
            lifeformOverviewGrid.Opacity = 1;
            dragDropOverlay.Visibility = Visibility.Hidden;
        }

        private void AddPictureOnDrop(object sender, DragEventArgs e)
        {
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
                        var importedPictureUri = Picture.ImportPicture(displayedCelestialBody, displayedFauna, displayedFlora, picture: new Uri(file));

                        if (displayedFauna != null)
                        {
                            var pictureID = DataRepository.AddFaunaPicture(displayedFauna, importedPictureUri.LocalPath);
                            displayedFauna.AddPicture(new Picture(pictureID, importedPictureUri));
                        }
                        else if (displayedFlora != null)
                        {
                            var pictureID = DataRepository.AddFloraPicture(displayedFlora, importedPictureUri.LocalPath);
                            displayedFlora.AddPicture(new Picture(pictureID, importedPictureUri));
                        }

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

            lifeformOverviewGrid.Opacity = 1;
            dragDropOverlay.Visibility = Visibility.Hidden;
        }
        #endregion

        // -----------------------------------------------------------------------------------------------
        // RESOURCE SEARCH
        // -----------------------------------------------------------------------------------------------
        #region Organic/Inorganic Resource Stuff
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
                                $"There is a new version available on GitHub!\n" +
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
            catch (Exception e)
            {
                AnalyticsUtil.TrackError(e);
                if (Debugger.IsAttached) { throw; }
            }
        }
    }
}
