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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
// using System.Windows.Forms;
using SimHub.Plugins.Styles;
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
            this.Plugin = plugin;
            SelectDeviceCombo.ItemsSource = AvailableDevices;
            BindToGameCarCheckbox.IsChecked = Plugin.Settings.BindToGameAndCar;
            OnGameCarChange("", "");
            RefreshButton_click(this, null);
            _updateTimer = new DispatcherTimer();
            _updateTimer.Tick += new EventHandler(OnTimer);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(250);
            _updateTimer.Start();
        }

        // --------------------------------------------------------
        // Automatic update
        // --------------------------------------------------------

        private void OnTimer(object sender, EventArgs e)
        {
            if (SelectDeviceCombo.ItemsSource == null)
                return;

            // if a device is selected, read device state and
            // update UI elements only if there are changes
            if ((SelectedDevice != null) && SelectedDevice.Refresh())
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
            ClutchPaddlesGroup.IsEnabled = (sLock == null) || !sLock.IsLocked;
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
                ListBoxItem item = (ListBoxItem)
                    ClutchWorkingModeListBox.
                        ItemContainerGenerator.
                            ContainerFromIndex((int)clutch.ClutchWorkingMode);
                if (item != null)
                    item.IsSelected = true;
                BitePointSlider.IsEnabled =
                    (clutch.ClutchWorkingMode == ClutchWorkingModes.Clutch);
            }
        }

        // --------------------------------------------------------
        // UI Event callbacks
        // (triggered by user interaction)
        // --------------------------------------------------------

        private void OnSelectDevice(object sender, SelectionChangedEventArgs args)
        {
            if (SelectedDevice != null)
                try
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [UI] Device selected: {0}", SelectedDevice.HidInfo.DisplayName);

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
                    TabVisible(ClutchPage, SelectedDevice.Capabilities.HasClutch);
                    TabVisible(AltButtonsPage, SelectedDevice.Capabilities.HasAltButtons);
                    TabVisible(DPadPage, SelectedDevice.Capabilities.HasDPad);
                    MainPages.SelectedIndex = 0;

                    // Read device state and update dynamic UI elements
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
                SimHub.Logging.Current.Info("[ESP32Simwheel] [UI] No device selected");
                TabVisible(InfoPage, false);
                TabVisible(ClutchPage, false);
                TabVisible(AltButtonsPage, false);
                TabVisible(DPadPage, false);
            }
        }

        private void OnBitePointSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            args.Handled = true;
            if ((SelectedDevice == null) || (SelectedDevice.Clutch == null))
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

        private void OnClutchWorkingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SelectedDevice != null) && (SelectedDevice.Clutch != null) && !_updating)
                foreach (ClutchWorkingModes workingMode in Enum.GetValues(typeof(ClutchWorkingModes)))
                {
                    ListBoxItem item = (ListBoxItem)
                        ClutchWorkingModeListBox.
                            ItemContainerGenerator.
                                ContainerFromIndex((int)workingMode);
                    if ((item != null) && item.IsSelected)
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
        }

        private void OnAltButtonsWorkingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SelectedDevice != null) &&
                (SelectedDevice.AltButtons != null) &&
                !_updating)
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
            if ((SelectedDevice != null) &&
                (SelectedDevice.DPad != null) &&
                !_updating)
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

        // --------------------------------------------------------
        // Plugin callbacks (called from CustomDataPlugin)
        // --------------------------------------------------------

        public void OnGameCarChange(string game, string car)
        {
            if ((game.Length == 0) || (car.Length == 0))
                GameAndCarText.Text = "none";
            else
                GameAndCarText.Text = string.Format(
                    "{0} / {1}",
                    game,
                    car);
        }

        // --------------------------------------------------------
        // Auxiliary methods
        // --------------------------------------------------------

        // Show or hid tab pages
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
                    // else if (tabPage == LedsPage)
                    //     idx = 4;
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

    } // classMainControl
} //namespace Afpineda.ESP32SimWheelPlugin