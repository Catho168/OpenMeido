using System.Windows.Controls;

namespace OpenMeido.Controls
{
    public partial class ChatMessageListControl : UserControl
    {
        public ChatMessageListControl()
        {
            InitializeComponent();
        }

        public ScrollViewer ScrollViewer => MessageScrollViewer;

        public ItemsControl ItemsHost => MessagesItemsControl;
    }
}