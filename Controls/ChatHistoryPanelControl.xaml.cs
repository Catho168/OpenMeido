using System.Windows;
using System.Windows.Controls;

namespace OpenMeido.Controls
{
    public partial class ChatHistoryPanelControl : UserControl
    {
        public ChatHistoryPanelControl()
        {
            InitializeComponent();
        }

        public Border PanelBorder => HistoryPanelBorder;

        public Panel ItemsPanel => HistoryItemsPanel;

        public event RoutedEventHandler NewChatRequested;

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            NewChatRequested?.Invoke(this, e);
        }
    }
}