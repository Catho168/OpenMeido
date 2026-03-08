using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenMeido.Controls
{
    public partial class ChatTitleBarControl : UserControl
    {
        public ChatTitleBarControl()
        {
            InitializeComponent();
        }

        public TextBlock StatusTextBlock => StatusTextBlockElement;

        public event MouseButtonEventHandler DragRequested;

        public event RoutedEventHandler MinimizeRequested;

        public event RoutedEventHandler CloseRequested;

        private void TitleBarBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragRequested?.Invoke(this, e);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeRequested?.Invoke(this, e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, e);
        }
    }
}