#region License
/******************************************************************************
 * @author Ángel Fernández Pineda. Madrid. Spain.
 * @date 2024-12-04
 *
 * @copyright Licensed under the EUPL
 *****************************************************************************/
#endregion License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Threading;
using SHDialogResult = System.Windows.Forms.DialogResult;

using SimHub.Plugins.Styles;
using SimHub.Plugins.UI;
using SimHub.Plugins.DataPlugins.RGBDriver;
using SimHub.Plugins.DataPlugins.RGBDriver.Settings;
using SimHub.Plugins.OutputPlugins.GraphicalDash.UI;
using SimHub.Plugins.ProfilesCommon;
using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    /// <summary>
    /// Custom settings page
    /// </summary>
    ///
    public partial class MainControl : System.Windows.Controls.UserControl
    {

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public MainControl()
        {
            InitializeComponent();
        }

        public MainControl(ESP32SimWheelPlugin plugin) : this()
        {
            // Initialization
            this.Plugin = plugin;
            CreateBindings();
            IsVisibleChanged += OnVisibilityChange;
            OnSelectDevice(null, null);
        }

        // --------------------------------------------------------
        // Bindings
        // --------------------------------------------------------

        private void CreateBindings()
        {
            Binding binding = new Binding(nameof(Plugin.Settings.BindToGameAndCar));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.TwoWay;
            BindToGameCarCheckbox.SetBinding(ToggleButton.IsCheckedProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.CurrentGameAndCar));
            binding.Source = Plugin.Settings;
            GameAndCarText.SetBinding(TextBlock.TextProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.IsBindingAvailable));
            binding.Source = Plugin.Settings;
            SaveButton.SetBinding(Button.IsEnabledProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.Devices));
            binding.Source = Plugin.Settings;
            SelectDeviceCombo.SetBinding(ComboBox.ItemsSourceProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.FrameSkip));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.TwoWay;
            binding.FallbackValue = 50;
            FrameSkipSlider.SetBinding(TitledSlider.ValueProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.SelectedDevice));
            binding.Source = Plugin.Settings;
            SelectDeviceCombo.SetBinding(ComboBox.SelectedItemProperty, binding);
            SelectDeviceCombo.SelectionChanged += OnSelectDevice;

            PropertyPath isLockedPropertyPath = new PropertyPath("SelectedDevice.SecurityLock.IsLocked", null);
            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = isLockedPropertyPath;
            binding.Converter = new ESP32SimWheel.Utils.SecurityLockToStringConverter();
            binding.FallbackValue = false;
            binding.Mode = BindingMode.OneWay;
            SecurityLockText.SetBinding(TextBlock.TextProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = isLockedPropertyPath;
            binding.Converter = new ESP32SimWheel.Utils.InvertBooleanConverter();
            binding.FallbackValue = true;
            binding.Mode = BindingMode.OneWay;
            ClutchPaddlesGroup.SetBinding(StackPanel.IsEnabledProperty, binding);
            AltButtonsGroup.SetBinding(StackPanel.IsEnabledProperty, binding);
            DPadGroup.SetBinding(StackPanel.IsEnabledProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Battery.BatteryLevel", null);
            binding.StringFormat = "{0:D}%";
            binding.Mode = BindingMode.OneWay;
            binding.FallbackValue = "Not available";
            BatteryText.SetBinding(TextBlock.TextProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Clutch.ClutchWorkingMode", null);
            binding.Mode = BindingMode.TwoWay;
            binding.Converter = new ESP32SimWheel.Utils.ClutchWorkingModeConverter();
            binding.FallbackValue = 0;
            ClutchWorkingModeListBox.SetBinding(ListBox.SelectedIndexProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Clutch.BitePoint", null);
            binding.Mode = BindingMode.TwoWay;
            binding.FallbackValue = 0;
            BitePointSlider.SetBinding(Slider.ValueProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.DPad.DPadWorkingMode", null);
            binding.Mode = BindingMode.TwoWay;
            binding.Converter = new ESP32SimWheel.Utils.DPadWorkingModeConverter();
            binding.FallbackValue = 0;
            DPadWorkingModeListBox.SetBinding(ListBox.SelectedIndexProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.AltButtons.AltButtonsWorkingMode", null);
            binding.Mode = BindingMode.TwoWay;
            binding.Converter = new ESP32SimWheel.Utils.AltButtonsWorkingModeConverter();
            binding.FallbackValue = 0;
            AltButtonsWorkingModeListBox.SetBinding(ListBox.SelectedIndexProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Capabilities.TelemetryLedsCount", null);
            binding.Mode = BindingMode.OneWay;
            binding.Converter = new ESP32SimWheel.Utils.PixelCountToVisibilityConverter();
            binding.FallbackValue = System.Windows.Visibility.Collapsed;
            TelemetryLedsGroup.SetBinding(UIElement.VisibilityProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Capabilities.ButtonsLightingCount", null);
            binding.Mode = BindingMode.OneWay;
            binding.Converter = new ESP32SimWheel.Utils.PixelCountToVisibilityConverter();
            binding.FallbackValue = System.Windows.Visibility.Collapsed;
            ButtonLedsGroup.SetBinding(UIElement.VisibilityProperty, binding);

            binding = new Binding();
            binding.Source = Plugin.Settings;
            binding.Path = new PropertyPath("SelectedDevice.Capabilities.IndividualLedsCount", null);
            binding.Mode = BindingMode.OneWay;
            binding.Converter = new ESP32SimWheel.Utils.PixelCountToVisibilityConverter();
            binding.FallbackValue = System.Windows.Visibility.Collapsed;
            IndividualLedsGroup.SetBinding(UIElement.VisibilityProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.TelemetryLedsSettings));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.OneWay;
            TelemetryLedsProfileCombo.SetBinding(ProfileCombobox.ProfileSettingsProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.BackLightLedsSettings));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.OneWay;
            ButtonLedsProfileCombo.SetBinding(ProfileCombobox.ProfileSettingsProperty, binding);

            binding = new Binding(nameof(Plugin.Settings.IndividualLedsSettings));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.OneWay;
            IndividualLedsProfileCombo.SetBinding(ProfileCombobox.ProfileSettingsProperty, binding);


            // Save car/game bindings and refresh device list
            SaveButton.Click += SaveButton_click;
            RefreshButton.Click += RefreshButton_click;

            // LEDs driver: TelemetryLedsGroup
            TelemetryLedsEditProfile.Click += LedsEditProfile_Click;
            TelemetryLedsImportProfile.Click += LedsImportProfile_Click;
            TelemetryLedsLoadProfile.Click += LedsLoadProfile_Click;

            // LEDs driver: ButtonLedsGroup
            ButtonLedsEditProfile.Click += LedsEditProfile_Click;
            ButtonLedsImportProfile.Click += LedsImportProfile_Click;
            ButtonLedsLoadProfile.Click += LedsLoadProfile_Click;

            // LEDs driver: IndividualLedsGroup
            IndividualLedsEditProfile.Click += LedsEditProfile_Click;
            IndividualLedsImportProfile.Click += LedsImportProfile_Click;
            IndividualLedsLoadProfile.Click += LedsLoadProfile_Click;

            // LEDs driver: Save / Undo
            SaveLedProfilesButton.Click += SaveLedProfilesButton_Click;
            UndoLedProfilesButton.Click += UndoLedProfilesButton_Click;
        }

        // --------------------------------------------------------
        // UI Event callbacks
        // (triggered by user interaction)
        // --------------------------------------------------------

        // UI updates are disabled when this control is not visible
        // (should avoid CPU usage)
        void OnVisibilityChange(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool visible = (bool)e.NewValue;
            SimHub.Logging.Current.InfoFormat(
                   "[ESP32 Sim-Wheel] [UI] Visibility = {0}",
                   visible);
            Plugin.Settings.IsUIVisible = visible;
        }

        private void OnSelectDevice(object sender, SelectionChangedEventArgs args)
        {
            var SelectedDevice = Plugin.Settings.SelectedDevice;
            if (SelectedDevice != null)
            {
                // Update static UI elements (not dependant on device state)
                TabVisible(InfoPage, true);
                HidInfoText.Text = string.Format("{0,4:X4} / {1,4:X4}",
                    SelectedDevice.HidInfo.VendorID,
                    SelectedDevice.HidInfo.ProductID);
                DataVersionText.Text = string.Format("{0}.{1}",
                    SelectedDevice.DataVersion.Major,
                    SelectedDevice.DataVersion.Minor);
                DeviceIDText.Text = string.Format("{0,16:X16}",
                    SelectedDevice.UniqueID);
                if (SelectedDevice.Capabilities.UsesTelemetryData)
                    TelemetryDataText.Text = string.Format("Yes ({0} frames per second)", SelectedDevice.Capabilities.FramesPerSecond);
                else
                    TelemetryDataText.Text = "No";
                TelemetryLedsCount.Text = SelectedDevice.Capabilities.TelemetryLedsCount.ToString();
                ButtonsLightingCount.Text = SelectedDevice.Capabilities.ButtonsLightingCount.ToString();
                IndividualLedsCount.Text = SelectedDevice.Capabilities.IndividualLedsCount.ToString();

                TelemetryLedsGroup.IsEnabled = (SelectedDevice.Capabilities.TelemetryLedsCount > 0);
                ButtonLedsGroup.IsEnabled = (SelectedDevice.Capabilities.ButtonsLightingCount > 0);
                IndividualLedsGroup.IsEnabled = (SelectedDevice.Capabilities.IndividualLedsCount > 0);
                TelemetryLedsGroup.Visibility =
                    (TelemetryLedsGroup.IsEnabled) ? Visibility.Visible : Visibility.Collapsed;
                ButtonLedsGroup.Visibility =
                    (ButtonLedsGroup.IsEnabled) ? Visibility.Visible : Visibility.Collapsed;
                IndividualLedsGroup.Visibility =
                    (IndividualLedsGroup.IsEnabled) ? Visibility.Visible : Visibility.Collapsed;

                TabVisible(ClutchPage, SelectedDevice.Capabilities.HasClutch);
                TabVisible(AltButtonsPage, SelectedDevice.Capabilities.HasAltButtons);
                TabVisible(DPadPage, SelectedDevice.Capabilities.HasDPad);
                TabVisible(LedsPage, SelectedDevice.Capabilities.HasPixelControl);
                MainPages.SelectedIndex = 0;
            }
            else
            {
                // No device is selected (or no devices are available)
                SimHub.Logging.Current.Info("[ESP32 Sim-wheel] [UI] No device selected");
                TabVisible(InfoPage, false);
                TabVisible(ClutchPage, false);
                TabVisible(AltButtonsPage, false);
                TabVisible(DPadPage, false);
                TabVisible(LedsPage, false);
            }
        }


        private void RefreshButton_click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Refresh();
        }

        private void SaveButton_click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void LedsEditProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((Plugin.Settings.SelectedDevice != null) && (Plugin.Settings.SelectedDevice.Pixels != null))
            {
                RGBLedsDriver driver = null;
                string subtitle = "";
                if (sender == TelemetryLedsEditProfile)
                {
                    driver = Plugin.Settings.SelectedDevice.Pixels.TelemetryLedsDriver;
                    subtitle = TelemetryLedsGroup.Title;
                }
                if (sender == ButtonLedsEditProfile)
                {
                    driver = Plugin.Settings.SelectedDevice.Pixels.BacklightLedsDriver;
                    subtitle = ButtonLedsGroup.Title;
                }
                if (sender == IndividualLedsEditProfile)
                {
                    driver = Plugin.Settings.SelectedDevice.Pixels.IndividualLedsDriver;
                    subtitle = ButtonLedsGroup.Title;
                }
                driver?.ShowEditorWindow(
                    this,
                    Plugin.Settings.SelectedDevice.HidInfo.DisplayName,
                    subtitle);
            }
        }

        private void LedsImportProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RGBLedsDriver driver = null;
            if (sender == TelemetryLedsImportProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.TelemetryLedsDriver ?? null;
            if (sender == ButtonLedsImportProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.BacklightLedsDriver ?? null;
            if (sender == IndividualLedsImportProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.IndividualLedsDriver ?? null;
            if (driver != null)
                new ProfilesManager<Profile, LedsSettings>(
                    (IProfileSettings<Profile>)driver.Settings).
                        importProfile_Click((object)null, (RoutedEventArgs)null);
        }

        private void LedsLoadProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RGBLedsDriver driver = null;
            if (sender == TelemetryLedsLoadProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.TelemetryLedsDriver ?? null;
            if (sender == ButtonLedsLoadProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.BacklightLedsDriver ?? null;
            if (sender == IndividualLedsLoadProfile)
                driver = Plugin.Settings.SelectedDevice?.Pixels?.IndividualLedsDriver ?? null;
            if (driver != null)
                new ProfilesManager<Profile, LedsSettings>(
                (IProfileSettings<Profile>)driver.Settings).
                    ShowDialogWindow((DependencyObject)this);
        }

        private void SaveLedProfilesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RGBLedsDriver driver = Plugin.Settings.SelectedDevice?.Pixels?.TelemetryLedsDriver ?? null;
            driver?.SaveSettings();
            driver = Plugin.Settings.SelectedDevice?.Pixels?.BacklightLedsDriver ?? null;
            driver?.SaveSettings();
            driver = Plugin.Settings.SelectedDevice?.Pixels?.IndividualLedsDriver ?? null;
            driver?.SaveSettings();
        }

        private async void UndoLedProfilesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((Plugin.Settings.SelectedDevice == null) ||
                (Plugin.Settings.SelectedDevice.Pixels == null))
                return;
            var res = await SHMessageBox.Show(
                "Are you sure?",
                "Undo",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Question);
            if (res == SHDialogResult.OK)
                Plugin.Settings.SelectedDevice?.Pixels?.ReloadLedsDriver();
        }

        // --------------------------------------------------------
        // Auxiliary methods
        // --------------------------------------------------------

        // Show or hide tab pages
        private void TabVisible(SimHub.Plugins.Styles.SHTabItem tabPage, bool visible)
        {
            if (tabPage.Parent == null)
            {
                if (visible)
                {
                    int idx = 0; // tabPage == InfoPage
                    if (tabPage == ClutchPage)
                        idx = 1;
                    else if (tabPage == AltButtonsPage)
                        idx = 2;
                    else if (tabPage == DPadPage)
                        idx = 3;
                    else if (tabPage == LedsPage)
                        idx = 4;

                    if (idx >= MainPages.Items.Count)
                        MainPages.Items.Add(tabPage);
                    else
                        MainPages.Items.Insert(idx, tabPage);
                }
            }
            else
            {
                if (!visible)
                    MainPages.Items.Remove(tabPage);
            }
        }

        // --------------------------------------------------------
        // Owner
        // --------------------------------------------------------

        public ESP32SimWheelPlugin Plugin { get; }

    } // classMainControl
} //namespace Afpineda.ESP32SimWheelPlugin