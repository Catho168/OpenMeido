using System.Windows.Controls;
using System.Windows.Input;

namespace OpenMeido.Controls
{
    public partial class MiniChatControl : UserControl
    {
        public MiniChatControl()
        {
            InitializeComponent();
        }

        public void FocusInput()
        {
            InputTextBox.Focus();
            Keyboard.Focus(InputTextBox);
            InputTextBox.CaretIndex = InputTextBox.Text?.Length ?? 0;
        }
    }
}