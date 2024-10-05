using SimHub.Plugins.Styles;
using System.Windows.Controls;

namespace Afpineda.ESP32SimWheelPlugin
{
    /// <summary>
    /// Custom settings page
    /// </summary>
    public partial class CustomSettingsControl : UserControl
    {
        public CustomDataPlugin Plugin { get; }

        public CustomSettingsControl()
        {
            InitializeComponent();
        }

        public CustomSettingsControl(CustomDataPlugin plugin) : this()
        {
            this.Plugin = plugin;
        }

        private void RefreshButton_click(object sender, System.Windows.RoutedEventArgs e)
        {
            Plugin.Refresh();
        }
    }
}