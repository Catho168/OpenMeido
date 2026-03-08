﻿﻿// WPF窗口互操作功能，用于获取窗口句柄和处理Windows消息
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows;
using System;
// 泛型集合功能，用于存储和管理菜单项列表
using System.Collections.Generic;
// WPF控件功能，如按钮、画布等UI元素
using System.Windows.Controls;
// WPF输入处理功能，如鼠标、键盘事件和命令模式
using System.Windows.Input;
// WPF媒体功能，用于变换、动画和视觉效果
using System.Windows.Media;
// WPF动画功能，用于女仆动画效果
using System.Windows.Media.Animation;
// WPF图像功能，用于女仆图片显示
using System.Windows.Media.Imaging;
// LINQ查询功能，用于集合的筛选和操作
using System.Linq;
// 引入任务以便异步等待关闭动画完成
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.Helpers;
using OpenMeido.ViewModels;

// 定义OpenMeido命名空间，用于组织和封装项目中的所有类
namespace OpenMeido
{
    // 定义主窗口类，继承自WPF的Window类，partial关键字表示这是一个部分类
    // 部分类允许将类的定义分散在多个文件中（通常.xaml和.xaml.cs文件）
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly MainWindowMcpStatusCoordinator _mcpStatusCoordinator;
        private readonly MainWindowHotkeyCoordinator _hotkeyCoordinator;
        private readonly MainWindowCommandCoordinator _commandCoordinator;
        private readonly MainWindowMiniChatPopupCoordinator _miniChatPopupCoordinator;
        private readonly MainWindowVisualCoordinator _visualCoordinator;
        private readonly MainWindowInteractionCoordinator _interactionCoordinator;

        // 独立的径向菜单控件实例
        private readonly RadialMenuControl _radialMenu;

        // 内容平移变换与动画状态
        private readonly TranslateTransform _contentShift = new TranslateTransform();

        // 主窗口构造函数，在创建MainWindow实例时自动调用
        // public表示外部代码可以创建此类的实例
        public MainWindow() : this(UiDependencyResolver.ResolveMainViewModel(), UiDependencyResolver.ResolveSettingsService(), UiDependencyResolver.ResolveApiServiceFactory(), UiDependencyResolver.ResolveMcpServiceFactory())
        {
        }

        public MainWindow(MainViewModel viewModel)
            : this(viewModel, UiDependencyResolver.ResolveSettingsService(), UiDependencyResolver.ResolveApiServiceFactory(), UiDependencyResolver.ResolveMcpServiceFactory())
        {
        }

        public MainWindow(MainViewModel viewModel, ISettingsService settingsService, IApiServiceFactory apiServiceFactory)
            : this(viewModel, settingsService, apiServiceFactory, UiDependencyResolver.ResolveMcpServiceFactory())
        {
        }

        public MainWindow(MainViewModel viewModel, ISettingsService settingsService, IApiServiceFactory apiServiceFactory, IMcpServiceFactory mcpServiceFactory, IMainWindowHotkeyPlatform hotkeyPlatform = null, IMainWindowCommandPlatform commandPlatform = null, IMainWindowInteractionPlatform interactionPlatform = null)
        {
            _viewModel = viewModel ?? UiDependencyResolver.ResolveMainViewModel();

            var resolvedSettingsService = settingsService ?? UiDependencyResolver.ResolveSettingsService();
            var resolvedMcpServiceFactory = mcpServiceFactory ?? UiDependencyResolver.ResolveMcpServiceFactory();
            _mcpStatusCoordinator = new MainWindowMcpStatusCoordinator(_viewModel, resolvedSettingsService, resolvedMcpServiceFactory);
            _hotkeyCoordinator = new MainWindowHotkeyCoordinator(hotkeyPlatform);
            _commandCoordinator = new MainWindowCommandCoordinator(this, commandPlatform);

            // InitializeComponent() 由XAML编译器自动调用，无需手动处理
            InitializeComponent();
            DataContext = _viewModel;

            // 设置画布的平移变换，用于实现窗口随鼠标轻微漂移
            if (MainCanvas != null)
            {
                MainCanvas.RenderTransform = _contentShift;
            }

            // 订阅窗口加载完成事件，使用+=操作符添加事件处理器
            // 当窗口完全加载并显示时会触发此事件
            Loaded += MainWindow_Loaded;

            // 订阅窗口关闭事件，确保程序退出时能够正确清理资源
            // 这对于释放系统资源（如注册的热键）非常重要
            Closing += MainWindow_Closing;

            // 订阅鼠标移动事件，this关键字明确指向当前窗口实例
            // 当鼠标在窗口内移动时会持续触发此事件
            MouseMove += (_, e) => _interactionCoordinator.HandleMouseMove(e.GetPosition(this));

            // 订阅鼠标离开窗口事件，用于实现自动隐藏功能
            MouseLeave += (_, __) => _ = _interactionCoordinator.HandleMouseLeaveAsync();

            //创建独立的径向菜单控件
            _radialMenu = new RadialMenuControl
            {
                MenuItems = _viewModel.MenuItems.ToList(),
                OnMenuCommand = ExecuteCommand,
                IsMiniChatOpen = _viewModel.IsMiniChatOpen,
                IsHitTestVisible = true,
            };

            // 初始添加到画布，位置置于(0,0) 并位于妹抖酱之上
            if (MainCanvas != null)
            {
                MainCanvas.Children.Add(_radialMenu);
                Canvas.SetLeft(_radialMenu, 0);
                Canvas.SetTop(_radialMenu, 0);
                Canvas.SetZIndex(_radialMenu, 1);
            }

            _visualCoordinator = new MainWindowVisualCoordinator(
                _contentShift,
                MainCanvas,
                MeidoImage,
                _radialMenu);

            _miniChatPopupCoordinator = new MainWindowMiniChatPopupCoordinator(
                _viewModel,
                _contentShift,
                MiniChatPopup,
                MiniChatControl,
                MeidoImage,
                MainCanvas,
                _radialMenu,
                _visualCoordinator.ShowStandbyImage,
                _visualCoordinator.ShowChattingImage,
                _visualCoordinator.PositionMeidoInCenter);

            _visualCoordinator.ConnectMiniChat(
                () => _miniChatPopupCoordinator.IsOpen,
                _miniChatPopupCoordinator.Position,
                _miniChatPopupCoordinator.HidePopupContent,
                _miniChatPopupCoordinator.Hide);

            _interactionCoordinator = new MainWindowInteractionCoordinator(
                this,
                _contentShift,
                mousePosition => _radialMenu.UpdateButtonScales(mousePosition),
                RefreshRadialMenuLayout,
                () => _miniChatPopupCoordinator.IsOpen,
                () => _visualCoordinator.PlayCloseAnimationAsync(Hide),
                interactionPlatform);

            // 使用Lambda表达式订阅Loaded事件，当窗口加载完成后生成径向按钮
            // (s, e) => 是Lambda表达式语法，s代表sender，e代表事件参数
            Loaded += (s, e) => RefreshRadialMenuLayout();

            // 订阅窗口大小改变事件，确保按钮布局能够适应窗口尺寸变化
            // 这实现了响应式设计，保证用户界面在不同窗口大小下都能正常显示
            SizeChanged += (s, e) => RefreshRadialMenuLayout();

            // 订阅妹抖酱点击事件
            if (MeidoImage != null)
            {
                MeidoImage.MouseLeftButtonDown += (_, __) => _miniChatPopupCoordinator.Toggle();
            }

            if (MiniChatControl != null)
            {
                MiniChatControl.SizeChanged += (_, __) => _miniChatPopupCoordinator.Position();
            }

            _viewModel.MiniChat.EscalationRequested += MiniChat_EscalationRequested;

            // 初始化MCP状态监控
            InitializeMcpStatusMonitoring();
        }

        private void MiniChat_EscalationRequested(object sender, MiniChatEscalationRequestedEventArgs e)
        {
            _commandCoordinator.Execute(
                MenuCommands.OpenAiChat,
                e.History.ToList(),
                HideMainWindowContent,
                RestoreMainWindowContent,
                () => { });
            _miniChatPopupCoordinator.Hide();
        }

        // 主窗口加载完成事件处理器，在窗口完全初始化后执行
        // 这里主要负责设置全局热键和Windows消息钩子
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 创建窗口互操作助手，用于获取WPF窗口的Win32句柄
            // WindowInteropHelper是WPF提供的桥梁类，连接WPF窗口和Win32窗口系统
            var helper = new WindowInteropHelper(this);

            // 获取窗口的Win32句柄（HWND），这是Windows系统中窗口的唯一标识符
            // 句柄是一个指针，指向Windows内核中的窗口对象
            var hwnd = helper.Handle;

            _hotkeyCoordinator.Attach(hwnd, _interactionCoordinator.ShowAtMouse);
        }

        // 主窗口关闭事件处理器，负责清理系统资源
        // CancelEventArgs允许取消关闭操作，但这里我们只是清理资源
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _hotkeyCoordinator.Dispose();

            // 清理迷你聊天 API 资源
            CleanupMiniChatResources();

            // 清理MCP资源
            CleanupMcpResources();
        }

        // 刷新径向菜单布局，主窗口仅负责宿主级协调。
        private void RefreshRadialMenuLayout()
        {
            var width = GetActualOrConfiguredSize(ActualWidth, Width);
            var height = GetActualOrConfiguredSize(ActualHeight, Height);
            _visualCoordinator.RefreshLayout(width, height);
        }

        // 命令执行方法，根据不同的命令类型执行相应的操作
        // 这里使用了命令模式，将操作封装为命令对象
        private void ExecuteCommand(ICommand command)
        {
            _commandCoordinator.Execute(
                command,
                _viewModel.MiniChat.GetHistorySnapshot(),
                HideMainWindowContent,
                RestoreMainWindowContent,
                Hide);
        }

        /// 隐藏主窗口内容（女仆、按钮、迷你聊天等）
        private void HideMainWindowContent()
        {
            _visualCoordinator.HideContent();
        }

        /// 恢复主窗口内容显示
        private void RestoreMainWindowContent()
        {
            _visualCoordinator.RestoreContent();
        }

        private static double GetActualOrConfiguredSize(double actualSize, double configuredSize)
        {
            if (actualSize > 0)
            {
                return actualSize;
            }

            if (!double.IsNaN(configuredSize) && configuredSize > 0)
            {
                return configuredSize;
            }

            return 0;
        }

        #region MCP状态监控

        /// 初始化MCP状态监控
        private void InitializeMcpStatusMonitoring()
        {
            _ = _mcpStatusCoordinator.StartAsync();
        }

        /// 清理迷你聊天API资源
        private void CleanupMiniChatResources()
        {
            try
            {
                _viewModel.MiniChat.EscalationRequested -= MiniChat_EscalationRequested;
                _viewModel.MiniChat.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理迷你聊天API资源失败: {ex.Message}");
            }
        }

        /// 清理MCP资源
        private void CleanupMcpResources()
        {
            _mcpStatusCoordinator.Dispose();
        }

        #endregion
    }
}
