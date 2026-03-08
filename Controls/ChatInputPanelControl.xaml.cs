using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenMeido.Controls
{
    public partial class ChatInputPanelControl : UserControl
    {
        public ChatInputPanelControl()
        {
            InitializeComponent();
        }

        public TextBox InputTextBox => InputTextBoxElement;

        public TextBlock PlaceholderTextBlock => PlaceholderTextBlockElement;

        public event RoutedEventHandler SendRequested;

        public event KeyEventHandler InputKeyDown;

        public event TextChangedEventHandler InputTextChanged;

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendRequested?.Invoke(this, e);
        }

        private void InputTextBoxElement_KeyDown(object sender, KeyEventArgs e)
        {
            InputKeyDown?.Invoke(this, e);
        }

        private void InputTextBoxElement_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputTextChanged?.Invoke(this, e);
        }
    }
}