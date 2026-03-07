using System.Collections.Generic;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _windowTitle = "OpenMeido";
        private bool _isMiniChatOpen;
        private string _mcpStatusText = "MCP: 0/0";

        public MainViewModel()
        {
            MenuItems = CreateDefaultMenuItems();
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public bool IsMiniChatOpen
        {
            get => _isMiniChatOpen;
            set => SetProperty(ref _isMiniChatOpen, value);
        }

        public string McpStatusText
        {
            get => _mcpStatusText;
            set => SetProperty(ref _mcpStatusText, value);
        }

        public IReadOnlyList<RadialMenuItem> MenuItems { get; }

        private static IReadOnlyList<RadialMenuItem> CreateDefaultMenuItems()
        {
            return new List<RadialMenuItem>
            {
                new RadialMenuItem { Icon = "📝", Command = MenuCommands.OpenNotepad, ToolTip = "打开记事本" },
                new RadialMenuItem { Icon = "🔒", Command = MenuCommands.LockWorkstation, ToolTip = "锁定电脑" },
                new RadialMenuItem { Icon = "💬", Command = MenuCommands.OpenAiChat, ToolTip = "窗口对话" },
                new RadialMenuItem { Icon = "⚙️", Command = MenuCommands.OpenSettings, ToolTip = "设置妹抖酱" }
            };
        }
    }
}