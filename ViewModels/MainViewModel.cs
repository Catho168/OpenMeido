using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _windowTitle = "OpenMeido";
        private bool _isMiniChatOpen;
        private string _mcpStatusText = "MCP: 0/0";
        private string _mcpStatusToolTip = "MCP服务器状态";
        private Visibility _mcpStatusVisibility = Visibility.Collapsed;
        private Brush _mcpStatusDotBrush = Brushes.Gray;

        public MainViewModel()
            : this(UiDependencyResolver.ResolveSettingsService(), UiDependencyResolver.ResolveApiServiceFactory())
        {
        }

        public MainViewModel(ISettingsService settingsService, IApiServiceFactory apiServiceFactory)
        {
            MenuItems = CreateDefaultMenuItems();
            MiniChat = new MiniChatViewModel(settingsService, apiServiceFactory);
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

        public string McpStatusToolTip
        {
            get => _mcpStatusToolTip;
            set => SetProperty(ref _mcpStatusToolTip, value);
        }

        public Visibility McpStatusVisibility
        {
            get => _mcpStatusVisibility;
            set => SetProperty(ref _mcpStatusVisibility, value);
        }

        public Brush McpStatusDotBrush
        {
            get => _mcpStatusDotBrush;
            set => SetProperty(ref _mcpStatusDotBrush, value);
        }

        public MiniChatViewModel MiniChat { get; }

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