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
using SimHub.Plugins.Styles;
using ESP32SimWheel;

namespace Afpineda.ESP32SimWheelPlugin
{
    /// <summary>
    /// Custom settings page
    /// </summary>
    ///
    public partial class MainControl : UserControl
    {

        // --------------------------------------------------------
        // Constructor
        // --------------------------------------------------------

        public MainControl()
        {
            InitializeComponent();
        }

        public MainControl(CustomDataPlugin plugin) : this()
        {
            this.Plugin = plugin;
            SelectDeviceCombo.ItemsSource = AvailableDevices;
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
            _updating = true;
            try
            {
                if ((SelectedDevice != null) && SelectedDevice.Refresh())
                {
                    UpdateBatteryState(SelectedDevice.Battery);
                    UpdateClutchState(SelectedDevice.Clutch);
                    UpdateSecurityLockState(SelectedDevice.SecurityLock);
                }
            }
            catch (Exception)
            {
                _updating = false;
                SimHub.Logging.Current.Info("[ESP32SimWheel] [UI] Current device unavailable");
                RefreshButton_click(null, null);
            }
            _updating = false;
        }

        private void UpdateSecurityLockState(ESP32SimWheel.ISecurityLock sLock)
        {
            ClutchPaddlesGroup.IsEnabled = (sLock == null) || !sLock.IsLocked;
            if (ClutchPaddlesGroup.IsEnabled)
                SecurityLockText.Text = "Disabled";
            else
                SecurityLockText.Text = "⚠ Enabled";
        }

        private void UpdateBatteryState(ESP32SimWheel.IBattery battery)
        {
            if (battery == null)
                BatteryText.Text = "Not available";
            else
                BatteryText.Text = string.Format("{0:D}%", battery.BatteryLevel);
        }

        private void UpdateClutchState(ESP32SimWheel.IClutch clutch)
        {
            if (clutch != null)
            {
                BitePointSlider.Value = SelectedDevice.Clutch.BitePoint;
                ClutchWorkingModeListBox.UnselectAll();
                ListBoxItem item = (ListBoxItem)
                    ClutchWorkingModeListBox.
                        ItemContainerGenerator.
                            ContainerFromIndex((int)clutch.ClutchWorkingMode);
                item.IsSelected = true;
            }
        }

        // --------------------------------------------------------
        // UI Event callbacks
        // --------------------------------------------------------

        private void OnSelectDevice(object sender, SelectionChangedEventArgs args)
        {
            var device = ((sender as ComboBox).SelectedItem as ESP32SimWheel.IDevice);
            if (device != null)
                try
                {
                    SimHub.Logging.Current.InfoFormat("[ESP32Simwheel] [UI] Device selected: {0}", device.HidInfo.DisplayName);
                    HidInfoText.Text = string.Format("{0,4:X4} / {1,4:X4}",
                        device.HidInfo.VendorID,
                        device.HidInfo.ProductID);
                    DataVersionText.Text = string.Format("{0}.{1}",
                        device.DataVersion.Major,
                        device.DataVersion.Minor);
                    DeviceIDText.Text = string.Format("{0,16:X16}",
                        device.UniqueID);
                    if (device.Capabilities.UsesTelemetryData)
                        TelemetryDataText.Text = string.Format("Yes ({0} frames per second)", device.Capabilities.FramesPerSecond);
                    else
                        TelemetryDataText.Text = "No";
                    UpdateBatteryState(device.Battery);
                    UpdateSecurityLockState(device.SecurityLock);
                    TabVisible(ClutchPage, device.Capabilities.HasClutch);
                    MainPages.SelectedIndex = 0;
                }
                catch (Exception)
                {
                    RefreshButton_click(null, null);
                }
        }

        private void OnBitePointSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            args.Handled = true;
            if ((SelectedDevice != null) && (SelectedDevice.Clutch != null) && !_updating)
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
            ESP32SimWheel.IDevice currentDevice = SelectDeviceCombo.SelectedItem as ESP32SimWheel.IDevice;
            ESP32SimWheel.IDevice autoSelection = null;
            AvailableDevices.Clear();
            foreach (var device in Devices.Enumerate())
            {
                AvailableDevices.Add(device);
                if ((currentDevice != null) && (currentDevice.UniqueID == device.UniqueID))
                    autoSelection = device;
            }
            SelectDeviceCombo.ItemsSource = null;
            SelectDeviceCombo.ItemsSource = AvailableDevices;
            if (autoSelection != null)
                SelectDeviceCombo.SelectedItem = autoSelection;
            else if (SelectDeviceCombo.Items.Count > 0)
                SelectDeviceCombo.SelectedIndex = 0;
        }

        private void ClutchWorkingModeListBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if ((SelectedDevice != null) && (SelectedDevice.Clutch != null) && !_updating)
                foreach (ClutchWorkingModes workingMode in Enum.GetValues(typeof(ClutchWorkingModes)))
                {
                    ListBoxItem item = (ListBoxItem)
                        ClutchWorkingModeListBox.
                            ItemContainerGenerator.
                                ContainerFromIndex((int)workingMode);
                    if (item.IsSelected)
                        try
                        {
                            SelectedDevice.Clutch.ClutchWorkingMode = workingMode;
                            return;
                        }
                        catch (Exception)
                        {
                            RefreshButton_click(null, null);
                            return;
                        }
                }
        }

        // --------------------------------------------------------
        // UI Bindings
        // --------------------------------------------------------

        // --------------------------------------------------------
        // Auxiliary methods
        // --------------------------------------------------------

        private void TabVisible(SimHub.Plugins.Styles.SHTabItem tabPage, bool visible)
        {
            if (tabPage.Parent == null)
            {
                if (visible)
                {
                    int idx = 0;
                    if (tabPage == ClutchPage)
                        idx = 1;
                    else if (tabPage == LedsPage)
                        idx = 2;
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
        public CustomDataPlugin Plugin { get; }

    } // classMainControl
} //namespace Afpineda.ESP32SimWheelPlugin