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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Starfield_Interactive_Smart_Slate.Screens.PlanetaryData
{
    public partial class PlanetaryDataMain : UserControl
    {
        private PlanetaryDataViewModel viewModel = PlanetaryDataViewModel.Instance;
        private MainViewModel mainViewModel = MainViewModel.Instance;

        private Window? activePictureViewer = null;

        public PlanetaryDataMain()
        {
            InitializeComponent();
            viewModel.PropertyChanged += HandlePropertyChanged;
            solarSystemsListView.Loaded += InitializeSolarSystemsListView;
        }

        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(viewModel.DisplayedSolarSystems))
            {
                recoverCelestialBodySelection();
            }
        }

        #region CELESTIAL BODIES -----------------------------------------------------------------------------------------------

        private void AddFaunaClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(LifeformType.Fauna);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFauna = DataRepository.AddFauna(dialog.lifeformNameInput.Text, viewModel.DisplayedCelestialBody.BodyID);

                // update local data state
                viewModel.DisplayedCelestialBody.AddFauna(insertedFauna);

                // update UI state
                DisplayCelestialBodyDetails(viewModel.DisplayedCelestialBody);
                DisplayEntityDetails(insertedFauna);
                SetSelectedFaunaWithUI(insertedFauna);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
            }
        }

        private void AddFloraClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            AddLifeformDialog dialog = new AddLifeformDialog(LifeformType.Flora);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                var insertedFlora = DataRepository.AddFlora(dialog.lifeformNameInput.Text, viewModel.DisplayedCelestialBody.BodyID);

                // update local data state
                viewModel.DisplayedCelestialBody.AddFlora(insertedFlora);

                // update UI state
                DisplayCelestialBodyDetails(viewModel.DisplayedCelestialBody);
                DisplayEntityDetails(insertedFlora);
                SetSelectedFloraWithUI(insertedFlora);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
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
                var insertedOutpost = DataRepository.AddOutpost(dialog.outpostNameInput.Text, viewModel.DisplayedCelestialBody.BodyID);

                // update local data state
                viewModel.DisplayedCelestialBody.AddOutpost(insertedOutpost);

                // update UI state
                DisplayCelestialBodyDetails(viewModel.DisplayedCelestialBody);
                DisplayEntityDetails(insertedOutpost);
                SetSelectedOutpostWithUI(insertedOutpost);

                editEntityButton.Focus();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
            }
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
                if (viewModel.DisplayedCelestialBody != celestialBody)
                {
                    App.Current.PlayScrollSound();

                    if (viewModel.SelectedCelestialBody == null)
                    {
                        DisplayCelestialBodyDetails(celestialBody);
                        ClearAllSelectionsExcept();
                    }
                }
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
            viewModel.SelectedEntity = null;
            viewModel.DisplayedEntity = null;

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

            List<SolarSystem> alphabeticalSystems = mainViewModel.AllSolarSystems
                .Where(solarSystem => !solarSystem.Discovered)
                .OrderBy(solarSystem => solarSystem.SystemName).ToList();

            SolarSystemSelector dialog = new SolarSystemSelector(alphabeticalSystems);

            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                SolarSystem selectedSolarSystem = dialog.SelectedSolarSystem;
                DataRepository.DiscoverSolarSystem(selectedSolarSystem);

                // TODO: don't reload all data here
                mainViewModel.ReloadAllData();

                // reset current selections/filters
                viewModel.SelectedEntity = null;
                ResetFilters();

                // show first celestial body of new system
                var firstCelestialBodyOfSystem = selectedSolarSystem.CelestialBodies[0];
                DisplayCelestialBodyDetails(firstCelestialBodyOfSystem);

                // select first celestial body of new system
                solarSystemsListView.LayoutUpdated += SetCelestialBodyOnLayoutUpdate;
            }
        }

        private void DisplayCelestialBodyDetails(CelestialBody celestialBody)
        {
            viewModel.DisplayedEntity = null;
            viewModel.DisplayedCelestialBody = celestialBody;
        }

        private void DisplayEntityDetails(Entity entity)
        {
            viewModel.DisplayedEntity = entity;
            entityOverviewScrollViewer.ScrollToTop();
        }

        private void EditEntityClicked(object sender, RoutedEventArgs e)
        {
            App.Current.PlayClickSound();

            if (viewModel.DisplayedEntity is Fauna)
            {
                LifeformEditor dialog = new LifeformEditor(
                    viewModel.DisplayedEntity as Fauna
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFauna = dialog.GetResultingFauna();
                    DataRepository.EditFauna(viewModel.DisplayedEntity as Fauna, resultingFauna);
                    viewModel.DisplayedCelestialBody.EditFauna(resultingFauna);
                    DisplayEntityDetails(resultingFauna);
                    if (viewModel.SelectedEntity != null)
                    {
                        viewModel.SelectedEntity = resultingFauna;
                        faunasListView.SelectedItem = viewModel.SelectedEntity;
                    }

                    FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                }
            }
            else if (viewModel.DisplayedEntity is Flora)
            {
                LifeformEditor dialog = new LifeformEditor(
                    viewModel.DisplayedEntity as Flora
                );
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingFlora = dialog.GetResultingFlora();
                    DataRepository.EditFlora(viewModel.DisplayedEntity as Flora, resultingFlora);
                    viewModel.DisplayedCelestialBody.EditFlora(resultingFlora);
                    DisplayEntityDetails(resultingFlora);
                    if (viewModel.SelectedEntity != null)
                    {
                        viewModel.SelectedEntity = resultingFlora;
                        florasListView.SelectedItem = viewModel.SelectedEntity;
                    }

                    FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
                }
            }
            else // assume outpost
            {
                OutpostEditor dialog = new OutpostEditor(viewModel.DisplayedEntity as Outpost);
                dialog.Owner = Window.GetWindow(this);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (dialog.ShowDialog() == true)
                {
                    var resultingOutpost = dialog.GetResultingOutpost();
                    DataRepository.EditOutpost(resultingOutpost);
                    viewModel.DisplayedCelestialBody.EditOutpost(resultingOutpost);
                    DisplayEntityDetails(resultingOutpost);
                    if (viewModel.SelectedEntity != null)
                    {
                        viewModel.SelectedEntity = resultingOutpost;
                        outpostsListView.SelectedItem = viewModel.SelectedEntity;
                    }
                }
            }
        }

        private void FaunaListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Fauna)
            {
                var fauna = listViewItem.DataContext as Fauna;
                if (viewModel.DisplayedEntity != fauna)
                {
                    App.Current.PlayScrollSound();

                    if (viewModel.SelectedEntity == null)
                    {
                        DisplayEntityDetails(fauna);
                    }
                }
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
            // find and update original celestial body inside mainViewModel.DiscoveredSolarSystems,
            // because viewModel.DisplayedCelestialBody may be a copy from an applied filter
            foreach (var solarSystem in mainViewModel.DiscoveredSolarSystems)
            {
                foreach (var celestialBody in solarSystem.CelestialBodies)
                {
                    if (celestialBody != viewModel.DisplayedCelestialBody && celestialBody.Equals(viewModel.DisplayedCelestialBody))
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
                if (viewModel.DisplayedEntity != flora)
                {
                    App.Current.PlayScrollSound();

                    if (viewModel.SelectedEntity == null)
                    {
                        DisplayEntityDetails(flora);
                    }
                }
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
                    foreach (var solarSystem in mainViewModel.DiscoveredSolarSystems)
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
                    Settings.Default.Reset();
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

                viewModel.FilterSolarSystemsByLifeforms();
            }
            else
            {
                App.Current.PlayCancelSound();
                viewModel.ResetAllFilters();
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
                viewModel.DisplayedCelestialBody.DeleteOutpost(outpost);
                ClearAllSelectionsExcept();
                ResetEntityOverview();

                FindOriginalDisplayedCelestialBody()?.NotifyLayoutUpdate();
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

                viewModel.FilterSolarSystemsByOutposts();
            }
            else
            {
                App.Current.PlayCancelSound();
                viewModel.ResetAllFilters();
                solarSystemFilterTextBox.Text = "";
                solarSystemFilterTextBox.IsEnabled = true;
            }
        }

        private void OutpostListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ListViewItem listViewItem && listViewItem.DataContext is Outpost)
            {
                var outpost = listViewItem.DataContext as Outpost;
                if (viewModel.DisplayedEntity != outpost)
                {
                    App.Current.PlayScrollSound();

                    if (viewModel.SelectedEntity == null)
                    {
                        DisplayEntityDetails(outpost);
                    }
                }
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

        private void recoverCelestialBodySelection()
        {
            // wait for UI to load, then attempt to recover selected item
            solarSystemsListView.UpdateLayout();

            // find the solar system based on selectedCelestialBody
            var containerGenerator = solarSystemsListView.ItemContainerGenerator;
            var selectedSolarSystem = containerGenerator.Items.FirstOrDefault(
                solarSystem => ((SolarSystem)solarSystem).CelestialBodies.Contains(viewModel.SelectedCelestialBody)
            );

            if (selectedSolarSystem != null)
            {
                var solarSystemListViewItem = containerGenerator.ContainerFromItem(selectedSolarSystem);
                ListView celestialBodyListView = FindNestedListView(solarSystemListViewItem);
                celestialBodyListView.SelectedItem = viewModel.SelectedCelestialBody;
            }
        }

        private void ResetEntityOverview()
        {
            viewModel.DisplayedEntity = null;
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
            viewModel.ResetAllFilters();
            solarSystemFilterTextBox.Text = "";
            solarSystemFilterTextBox.IsEnabled = true;
        }

        private void SetCelestialBodyOnLayoutUpdate(object sender, EventArgs e)
        {
            SetSelectedCelestialBodyWithUI(viewModel.DisplayedCelestialBody);
            solarSystemsListView.LayoutUpdated -= SetCelestialBodyOnLayoutUpdate;
        }

        private void SetSelectedCelestialBody(CelestialBody celestialBody)
        {
            viewModel.SelectedCelestialBody = celestialBody;
            Settings.Default.SelectedCelestialBodyID = viewModel.SelectedCelestialBody?.BodyID ?? -1;
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
            viewModel.SelectedEntity = fauna;
            faunasListView.SelectedItem = fauna;
            DisplayEntityDetails(fauna);

            Settings.Default.SelectedFaunaID = fauna.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (viewModel.SelectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(viewModel.DisplayedCelestialBody);
            }
        }

        private void SetSelectedFloraWithUI(Flora flora)
        {
            if (flora == null)
            {
                throw new Exception("SetSelectedFlora() is only for non-null values. Use ClearFloraSelection() otherwise.");
            }

            ClearAllSelectionsExcept(flora);
            viewModel.SelectedEntity = flora;
            florasListView.SelectedItem = flora;
            DisplayEntityDetails(flora);

            Settings.Default.SelectedFloraID = flora.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (viewModel.SelectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(viewModel.DisplayedCelestialBody);
            }
        }

        private void SetSelectedOutpostWithUI(Outpost outpost)
        {
            if (outpost == null)
            {
                throw new Exception("SetSelectedOutpost() is only for non-null values. Use ClearOutpostSelection() otherwise.");
            }

            ClearAllSelectionsExcept(outpost);
            viewModel.SelectedEntity = outpost;
            outpostsListView.SelectedItem = outpost;
            DisplayEntityDetails(outpost);

            Settings.Default.SelectedOutpostID = outpost.ID;
            Settings.Default.Save();

            // persist celestial body selection if not already persisted
            if (viewModel.SelectedCelestialBody == null)
            {
                SetSelectedCelestialBodyWithUI(viewModel.DisplayedCelestialBody);
            }
        }

        private void SolarSystemFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (!outpostFilter_MenuItem.IsChecked)
            {
                viewModel.SearchSolarSystemsByText(solarSystemFilterTextBox.Text);
            }
        }

        private void ToggleSelectCelestialBody(CelestialBody celestialBody, ListView parent, ListViewItem clickedItem)
        {
            if (celestialBody.Equals(viewModel.SelectedCelestialBody))
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
            if (viewModel.SelectedEntity == fauna)
            {
                App.Current.PlayCancelSound();

                // like ClearFaunaSelection but keep displayedFauna
                viewModel.SelectedEntity = null;
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
            if (viewModel.SelectedEntity == flora)
            {
                App.Current.PlayCancelSound();

                // like ClearFloraSelection but keep displayedFlora
                viewModel.SelectedEntity = null;
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
            if (viewModel.SelectedEntity == outpost)
            {
                App.Current.PlayCancelSound();

                // like ClearOutpostSelection but keep displayedOutpost
                viewModel.SelectedEntity = null;
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

            if (viewModel.DisplayedEntity is Fauna)
            {
                pictureID = DataRepository.AddFaunaPicture(viewModel.DisplayedEntity as Fauna, importedPictureUri.LocalPath);
            }
            else if (viewModel.DisplayedEntity is Flora)
            {
                pictureID = DataRepository.AddFloraPicture(viewModel.DisplayedEntity as Flora, importedPictureUri.LocalPath);
            }
            else if (viewModel.DisplayedEntity is Outpost)
            {
                pictureID = DataRepository.AddOutpostPicture(viewModel.DisplayedEntity as Outpost, importedPictureUri.LocalPath);
            }
            else
            {
                return;
            }

            viewModel.DisplayedEntity.AddPicture(new Picture(pictureID, importedPictureUri));
        }

        private void AddPictureDragEnter(object sender, DragEventArgs e)
        {
            if (viewModel.DisplayedEntity != null)
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
            if (viewModel.DisplayedEntity == null)
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
                        var importedPictureUri = Picture.ImportPicture(viewModel.DisplayedCelestialBody, viewModel.DisplayedEntity, picture: new Uri(file));
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
                    var importedPictureUri = Picture.ImportPicture(viewModel.DisplayedCelestialBody, viewModel.DisplayedEntity, picture: newPictureUri);
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
            if (viewModel.DisplayedEntity is Fauna)
            {
                DataRepository.DeleteFaunaPicture(picture);
            }
            else if (viewModel.DisplayedEntity is Flora)
            {
                DataRepository.DeleteFloraPicture(picture);
            }
            else // assume outpost
            {
                DataRepository.DeleteOutpostPicture(picture);
            }
            viewModel.DisplayedEntity.RemovePicture(picture);
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

            viewModel.PictureGridColumns = columnsInt;
        }

        #endregion PICTURES -----------------------------------------------------------------------------------------------

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {

            Debug.WriteLine($"KEY:{e.Key}   MOD:{Keyboard.Modifiers}");
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && viewModel.DisplayedEntity != null)
            {
                if (Clipboard.ContainsImage())
                {
                    BitmapSource clipboardImage = Clipboard.GetImage();
                    var importedPictureUri = Picture.ImportPicture(viewModel.DisplayedCelestialBody, viewModel.DisplayedEntity, directSource: clipboardImage);
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