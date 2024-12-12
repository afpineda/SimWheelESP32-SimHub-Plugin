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
            RefreshButton_click(this, null);

            // Timer to poll selected device
            _updateTimer = new DispatcherTimer();
            _updateTimer.Tick += new EventHandler(OnTimer);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(POLLING_INTERVAL_MS);
            // The timer will be started when this control becomes visible
            IsVisibleChanged += OnVisibilityChange;
        }

        // --------------------------------------------------------
        // Bindings
        // --------------------------------------------------------

        private void CreateBindings()
        {
            // BindToGameCarCheckbox
            Binding binding = new Binding(nameof(Plugin.Settings.BindToGameAndCar));
            binding.Source = Plugin.Settings;
            binding.Mode = BindingMode.TwoWay;
            BindToGameCarCheckbox.SetBinding(ToggleButton.IsCheckedProperty, binding);

            // GameAndCarText
            binding = new Binding(nameof(Plugin.Settings.CurrentGameAndCar));
            binding.Source = Plugin.Settings;
            GameAndCarText.SetBinding(TextBlock.TextProperty, binding);

            // SaveButton
            binding = new Binding(nameof(Plugin.Settings.IsBindingAvailable));
            binding.Source = Plugin.Settings;
            SaveButton.SetBinding(Button.IsEnabledProperty, binding);
            SaveButton.Click += SaveButton_click;

            // RefreshButton
            RefreshButton.Click += RefreshButton_click;

            // SelectedDeviceCombo
            SelectDeviceCombo.ItemsSource = AvailableDevices;
            SelectDeviceCombo.SelectionChanged += OnSelectDevice;

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
        // Automatic UI update from device status
        // --------------------------------------------------------

        private void OnTimer(object sender, EventArgs e)
        {
            // if a device is selected, read device state and
            // update UI elements only if there are changes
            if (SelectedDevice?.Refresh() ?? false)
                UpdateUIFromDeviceState();
        }

        private void UpdateUIFromDeviceState()
        {
            if (SelectedDevice != null)
                try
                {
                    _updating = true;
                    UpdateUIFromBatteryState(SelectedDevice.Battery);
                    UpdateUIFromClutchState(SelectedDevice.Clutch);
                    UpdateUIFromSecurityLockState(SelectedDevice.SecurityLock);
                    UpdateUIFromDPad(SelectedDevice.DPad);
                    UpdateUIFromAltButtons(SelectedDevice.AltButtons);
                    _updating = false;
                }
                catch (Exception)
                {
                    _updating = false;
                    RefreshButton_click(null, null);
                }
        }

        private void UpdateUIFromSecurityLockState(ESP32SimWheel.ISecurityLock sLock)
        {
            ClutchPaddlesGroup.IsEnabled = !sLock?.IsLocked ?? false;
            AltButtonsGroup.IsEnabled = ClutchPaddlesGroup.IsEnabled;
            DPadGroup.IsEnabled = ClutchPaddlesGroup.IsEnabled;
            if (ClutchPaddlesGroup.IsEnabled)
                SecurityLockText.Text = "Disabled";
            else
                SecurityLockText.Text = "⚠ Enabled";
        }

        private void UpdateUIFromBatteryState(ESP32SimWheel.IBattery battery)
        {
            if (battery == null)
                BatteryText.Text = "Not available";
            else
                BatteryText.Text = string.Format("{0:D}%", battery.BatteryLevel);
        }

        private void UpdateUIFromClutchState(ESP32SimWheel.IClutch clutch)
        {
            if (clutch != null)
            {
                BitePointSlider.Value = clutch.BitePoint;
                ClutchWorkingModeListBox.UnselectAll();
                ClutchWorkingModeListBox.SelectedIndex =
                    (int)clutch.ClutchWorkingMode;
                BitePointSlider.IsEnabled =
                    (clutch.ClutchWorkingMode == ClutchWorkingModes.Clutch);
            }
        }

        private void UpdateUIFromDPad(ESP32SimWheel.IDpad dPad)
        {
            if (dPad != null)
            {
                DPadWorkingModeListBox.UnselectAll();
                if (dPad.DPadWorkingMode == DPadWorkingModes.Button)
                    DPadWorkingModeListBox.SelectedIndex = 1;
                else
                    DPadWorkingModeListBox.SelectedIndex = 0;
            }
        }
        private void UpdateUIFromAltButtons(ESP32SimWheel.IAltButtons altButtons)
        {
            if (altButtons != null)
            {
                AltButtonsWorkingModeListBox.UnselectAll();
                if (altButtons.AltButtonsWorkingMode == AltButtonWorkingModes.Button)
                    AltButtonsWorkingModeListBox.SelectedIndex = 1;
                else
                    AltButtonsWorkingModeListBox.SelectedIndex = 0;
            }
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
            if (visible)
            {
                BindToGameCarCheckbox.IsChecked = Plugin.Settings.BindToGameAndCar;
                _updateTimer.Start();
            }
            else
                _updateTimer.Stop();
        }

        private void OnSelectDevice(object sender, SelectionChangedEventArgs args)
        {
            if (SelectedDevice != null)
                try
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32 Sim-wheel] [UI] Device selected: {0}", SelectedDevice.HidInfo.DisplayName);

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

                    // Read device state and update dynamic UI elements
                    UpdateLedsDrivers();
                    SelectedDevice.Refresh();
                    UpdateUIFromDeviceState();
                }
                catch (Exception)
                {
                    // Device disconnected, reload device list
                    RefreshButton_click(null, null);
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

        private void OnBitePointSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            args.Handled = true;
            if (SelectedDevice?.Clutch == null)
                return;
            if (!_updating)
                // propagate UI -> device
                try
                {
                    SelectedDevice.Clutch.BitePoint = (byte)BitePointSlider.Value;
                }
                catch (Exception)
                {
                    RefreshButton_click(null, null);
                }
        }

        private void RefreshButton_click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Refresh();

            // Remember current device selection
            ESP32SimWheel.IDevice currentDevice = SelectedDevice;
            ESP32SimWheel.IDevice autoSelection = null;

            // Recreate device list
            AvailableDevices.Clear();
            foreach (var device in Devices.Enumerate())
            {
                AvailableDevices.Add(device);
                if ((currentDevice != null) && (currentDevice.UniqueID == device.UniqueID))
                    autoSelection = device;
            }

            // Trick to force UI update
            SelectDeviceCombo.ItemsSource = null;
            SelectDeviceCombo.ItemsSource = AvailableDevices;

            // Restore previous device selection, if any
            if (autoSelection != null)
                SelectDeviceCombo.SelectedItem = autoSelection;
            else if (SelectDeviceCombo.Items.Count > 0)
                // Or select the first available device, if any
                SelectDeviceCombo.SelectedIndex = 0;
            else
                // Disable user interface, since there are no devices
                OnSelectDevice(null, null);
            SelectDeviceCombo.IsEnabled = (SelectDeviceCombo.Items.Count > 0);
        }

        private void SaveButton_click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Save();
        }

        private void OnClutchWorkingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SelectedDevice?.Clutch != null) && !_updating)
                foreach (ClutchWorkingModes workingMode in Enum.GetValues(typeof(ClutchWorkingModes)))
                {
                    ListBoxItem item = (ListBoxItem)
                        ClutchWorkingModeListBox.
                            ItemContainerGenerator.
                                ContainerFromIndex((int)workingMode);
                    if (item?.IsSelected ?? false)
                        try
                        {
                            SelectedDevice.Clutch.ClutchWorkingMode = workingMode;
                            BitePointSlider.IsEnabled = (workingMode == ClutchWorkingModes.Clutch);
                            return;
                        }
                        catch (Exception)
                        {
                            RefreshButton_click(null, null);
                            return;
                        }
                }
        }

        private void OnBindToGameCarChanged(object sender, RoutedEventArgs e)
        {
            Plugin.Settings.BindToGameAndCar = BindToGameCarCheckbox.IsChecked ?? false;
            SaveButton.IsEnabled =
                Plugin.Settings.BindToGameAndCar &&
                Plugin.Settings.GameAndCarAvailable;
        }

        private void OnAltButtonsWorkingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SelectedDevice?.AltButtons != null) && !_updating)
                try
                {
                    if (AltButtonsButtonMode.IsSelected)
                        SelectedDevice.AltButtons.AltButtonsWorkingMode =
                            AltButtonWorkingModes.Button;
                    else
                        SelectedDevice.AltButtons.AltButtonsWorkingMode =
                            AltButtonWorkingModes.ALT;
                }
                catch
                {
                    RefreshButton_click(null, null);
                }
        }

        private void OnDPadWorkingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SelectedDevice?.DPad != null) && !_updating)
                try
                {
                    if (DPadButtonMode.IsSelected)
                        SelectedDevice.DPad.DPadWorkingMode =
                            DPadWorkingModes.Button;
                    else
                        SelectedDevice.DPad.DPadWorkingMode =
                            DPadWorkingModes.Navigation;
                }
                catch
                {
                    RefreshButton_click(null, null);
                }
        }

        private void LedsEditProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SelectedDevice != null)
            {
                RGBLedsDriver driver = null;
                string subtitle = null;
                if (sender == TelemetryLedsEditProfile)
                {
                    driver = _rgbLedsDriver[(int)PixelGroups.TelemetryLeds];
                    subtitle = TelemetryLedsGroup.Title;
                }
                if (sender == ButtonLedsEditProfile)
                {
                    driver = _rgbLedsDriver[(int)PixelGroups.ButtonsLighting];
                    subtitle = ButtonLedsGroup.Title;
                }
                if (sender == IndividualLedsEditProfile)
                {
                    driver = _rgbLedsDriver[(int)PixelGroups.IndividualLeds];
                    subtitle = ButtonLedsGroup.Title;
                }
                driver?.ShowEditorWindow(
                    this,
                    SelectedDevice.HidInfo.DisplayName,
                    subtitle);
            }
        }

        private void LedsImportProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RGBLedsDriver driver = null;
            if (sender == TelemetryLedsImportProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.TelemetryLeds];
            if (sender == ButtonLedsImportProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.ButtonsLighting];
            if (sender == IndividualLedsImportProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.IndividualLeds];
            if (driver != null)
                new ProfilesManager<Profile, LedsSettings>(
                    (IProfileSettings<Profile>)driver.Settings).
                        importProfile_Click((object)null, (RoutedEventArgs)null);
        }

        private void LedsLoadProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RGBLedsDriver driver = null;
            if (sender == TelemetryLedsLoadProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.TelemetryLeds];
            if (sender == ButtonLedsLoadProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.ButtonsLighting];
            if (sender == IndividualLedsLoadProfile)
                driver = _rgbLedsDriver[(int)PixelGroups.IndividualLeds];
            if (driver != null)
                new ProfilesManager<Profile, LedsSettings>(
                (IProfileSettings<Profile>)driver.Settings).
                    ShowDialogWindow((DependencyObject)this);
        }

        private void SaveLedProfilesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (PixelGroups group in Enum.GetValues(typeof(PixelGroups)))
                _rgbLedsDriver[(int)group]?.SaveSettings();
            if (SelectedDevice != null)
                Plugin.ReloadRGBLedDrivers(SelectedDevice.UniqueID);
        }

        private async void UndoLedProfilesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            var res = await SHMessageBox.Show(
                "Are you sure?",
                "Undo",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Question);
            if (res == SHDialogResult.OK)
                UpdateLedsDrivers();
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

        private void UpdateLedsDrivers()
        {
            TelemetryLedsProfileCombo.ProfileSettings = null;
            ButtonLedsProfileCombo.ProfileSettings = null;
            IndividualLedsProfileCombo.ProfileSettings = null;
            if (SelectedDevice != null)
            {
                foreach (PixelGroups group in Enum.GetValues(typeof(PixelGroups)))
                {
                    _rgbLedsDriver[(int)group] =
                        (SelectedDevice.Capabilities.GetPixelCount(group) > 0) ?
                            new RGBLedsDriver(
                                Utils.GetLedsSettingsFile(SelectedDevice.UniqueID, group))
                        :
                            null;
                }
            }
            else
            {
                _rgbLedsDriver[0] = null;
                _rgbLedsDriver[1] = null;
                _rgbLedsDriver[2] = null;
            }
            TelemetryLedsProfileCombo.ProfileSettings =
                _rgbLedsDriver[(int)PixelGroups.TelemetryLeds]?.Settings;
            ButtonLedsProfileCombo.ProfileSettings =
                _rgbLedsDriver[(int)PixelGroups.ButtonsLighting]?.Settings;
            IndividualLedsProfileCombo.ProfileSettings =
                _rgbLedsDriver[(int)PixelGroups.IndividualLeds]?.Settings;
        }

        //------------------------------------------------------------
        // INotifyPropertyChanged implementation
        //------------------------------------------------------------

        // public event PropertyChangedEventHandler PropertyChanged;

        // private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        // {
        //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // }

        // --------------------------------------------------------
        // Private Fields and properties
        // --------------------------------------------------------

        private bool _updating = false;
        private readonly DispatcherTimer _updateTimer;

        private ESP32SimWheel.IDevice SelectedDevice
        {
            get { return SelectDeviceCombo.SelectedItem as ESP32SimWheel.IDevice; }
        }

        private readonly List<ESP32SimWheel.IDevice> AvailableDevices = new List<ESP32SimWheel.IDevice>();
        public ESP32SimWheelPlugin Plugin { get; }
        private const int POLLING_INTERVAL_MS = 250;

        private readonly RGBLedsDriver[] _rgbLedsDriver = new RGBLedsDriver[3];


    } // classMainControl
} //namespace Afpineda.ESP32SimWheelPlugin