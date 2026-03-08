using System.Windows;
using System.Windows.Controls;

namespace OpenMeido.Controls
{
    public partial class ChatToolbarControl : UserControl
    {
        public ChatToolbarControl()
        {
            InitializeComponent();
        }

        public TextBlock HistoryToggleIcon => HistoryToggleIconElement;

        public TextBlock CurrentSessionTitle => CurrentSessionTitleElement;

        public event RoutedEventHandler HistoryToggleRequested;

        public event RoutedEventHandler McpStatusRequested;

        public event RoutedEventHandler ClearRequested;

        public event RoutedEventHandler SettingsRequested;

        private void HistoryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryToggleRequested?.Invoke(this, e);
        }

        private void McpStatusButton_Click(object sender, RoutedEventArgs e)
        {
            McpStatusRequested?.Invoke(this, e);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearRequested?.Invoke(this, e);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsRequested?.Invoke(this, e);
        }
    }
}