using System.Windows;
using System.Windows.Controls;

namespace OpenMeido.Controls
{
    public partial class McpStatusPanelControl : UserControl
    {
        public McpStatusPanelControl()
        {
            InitializeComponent();
        }

        public Border PanelBorder => McpStatusPanelBorder;

        public Panel ServersPanel => McpServersPanel;

        public Panel ToolsPanel => McpToolsPanel;

        public Panel ActivityPanel => McpActivityPanel;

        public event RoutedEventHandler RefreshRequested;

        public event RoutedEventHandler ClearLogRequested;

        private void RefreshMcpButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshRequested?.Invoke(this, e);
        }

        private void ClearMcpLogButton_Click(object sender, RoutedEventArgs e)
        {
            ClearLogRequested?.Invoke(this, e);
        }
    }
}