// 系统运行时互操作服务，用于调用Windows API函数
using System.Runtime.InteropServices;
// WPF窗口互操作功能，用于获取窗口句柄和处理Windows消息
using System.Windows.Interop;
// 3D媒体功能
using System.Windows.Media.Media3D;
// WPF核心
using System.Windows;
using System;
// 进程管理功能，用于启动外部程序
using System.Diagnostics;
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

// 定义OpenMeido命名空间，用于组织和封装项目中的所有类
namespace OpenMeido
{
    // 定义主窗口类，继承自WPF的Window类，partial关键字表示这是一个部分类
    // 部分类允许将类的定义分散在多个文件中（通常.xaml和.xaml.cs文件）
    public partial class MainWindow : Window
    {
        // 定义全局热键的唯一标识符，用于在系统中注册和识别我们的热键
        // const表示编译时常量，值在编译后不可更改
        const int HOTKEY_ID = 9000;

        // 定义Alt键的修饰符常量，0x0001是Windows API中Alt键的十六进制值
        // uint表示无符号32位整数，与Windows API的参数类型保持一致
        const uint MOD_ALT = 0x0001;

        // 定义R键的虚拟键码，0x52是字母R在Windows虚拟键码表中的十六进制值
        const uint VK_R = 0x52;

        // 使用P/Invoke技术声明Windows API函数RegisterHotKey
        // DllImport特性告诉.NET运行时从user32.dll动态链接库中导入此函数
        // static extern表示这是一个外部静态方法，由操作系统提供实现
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        // 声明取消注册热键的Windows API函数，用于程序退出时清理资源
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 声明私有字段存储径向菜单项列表，使用泛型List提供动态数组功能
        // private确保只有当前类可以访问此字段，实现封装原则
        private List<RadialMenuItem> menuItems = new List<RadialMenuItem>();

        // 独立的径向菜单控件实例
        private RadialMenuControl _radialMenu;

        // 内容平移变换与动画状态
        private readonly TranslateTransform _contentShift = new TranslateTransform();
        private const double MAX_WINDOW_SHIFT = 7; // 窗口随鼠标漂移的最大像素
        private bool _isClosingAnimationRunning = false;

        //迷你聊天相关字段
        private bool _isMiniChatOpen = false;          // 迷你聊天栏是否打开
        private int _miniChatRoundCount = 0;           // 对话轮次计数
        private Border _miniChatContainer;             // 聊天UI容器
        private TextBox _miniChatInput;                // 输入框
        private StackPanel _miniChatPanel;             // 用于显示问答气泡
        private ApiService _miniApiService;            // 复用 ApiService
        private AppSettings _miniSettings;             // 设置
        private List<ChatMessage> _miniChatHistory = new List<ChatMessage>();

        // 妹抖酱待机/聊天图片路径常量
        private const string MeidoStandbyImagePath = "Assets/Meido/Meido_standby.png";
        private const string MeidoChattingImagePath = "Assets/Meido/Meido_chatting.png";

        // 设置妹抖酱图片辅助方法
        private void SetMeidoImage(string relativePath)
        {
            if (MeidoImage == null) return;
            try
            {
                // 使用 Pack URI 格式加载程序集内嵌资源，避免路径解析问题
                var packUri = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = packUri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                MeidoImage.Source = bitmap;
            }
            catch
            {
                // Fallback：尝试以站点路径相对方式加载
                try
                {
                    MeidoImage.Source = new BitmapImage(new Uri(relativePath, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                    // 忽略错误
                }
            }
        }

        // 主窗口构造函数，在创建MainWindow实例时自动调用
        // public表示外部代码可以创建此类的实例
        public MainWindow()
        {
            // InitializeComponent() 由XAML编译器自动调用，无需手动处理
            InitializeComponent();

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
            this.MouseMove += GlobalMouseTracker;

            // 订阅鼠标离开窗口事件，用于实现自动隐藏功能
            this.MouseLeave += WindowHider;

            // 使用集合初始化语法创建并初始化菜单项列表
            // 这种语法是C# 3.0引入的语法糖，使代码更简洁易读
            menuItems = new List<RadialMenuItem>
            {
                // 创建记事本菜单项，使用对象初始化语法设置属性
                new RadialMenuItem {
                    Icon = "📝",
                    Command = MenuCommands.OpenNotepad,  // 引用预定义的命令对象
                    ToolTip = "打开记事本"
                },
                new RadialMenuItem {
                    Icon = "🔒",
                    Command = MenuCommands.LockWorkstation,
                    ToolTip = "锁定电脑"
                },

                new RadialMenuItem
                {
                    Icon = "💬",
                    Command = MenuCommands.OpenAiChat,
                    ToolTip = "窗口对话"
                },

                new RadialMenuItem
                {
                    Icon = "⚙️",
                    Command = MenuCommands.OpenSettings,
                    ToolTip = "设置妹抖酱"
                }
            };

            //创建独立的径向菜单控件
            _radialMenu = new RadialMenuControl
            {
                MenuItems = menuItems,
                OnMenuCommand = ExecuteCommand,
                IsMiniChatOpen = _isMiniChatOpen,
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

            // 使用Lambda表达式订阅Loaded事件，当窗口加载完成后生成径向按钮
            // (s, e) => 是Lambda表达式语法，s代表sender，e代表事件参数
            Loaded += (s, e) => GenerateRadialButtons();

            // 订阅窗口大小改变事件，确保按钮布局能够适应窗口尺寸变化
            // 这实现了响应式设计，保证用户界面在不同窗口大小下都能正常显示
            SizeChanged += (s, e) => GenerateRadialButtons();

            // 订阅妹抖酱点击事件
            if (MeidoImage != null)
            {
                MeidoImage.MouseLeftButtonDown += (_, __) => ToggleMiniChat();
            }
        }

        // 窗口隐藏事件处理器，当鼠标离开窗口时自动隐藏窗口
        // private访问修饰符确保只有当前类可以调用此方法
        private void WindowHider(object sender, MouseEventArgs e)
        {
            // 如果迷你聊天栏已打开，则不自动关闭
            if (!_isMiniChatOpen)
            {
                // 触发关闭动画，而不是立即隐藏
                StartCloseAnimation();
            }
        }

        // 全局鼠标跟踪事件处理器，实现鼠标悬停时按钮的动态缩放效果
        private void GlobalMouseTracker(object sender, MouseEventArgs e)
        {
            // 获取鼠标在窗口中的逻辑坐标
            Point windowMousePos = e.GetPosition(this);

            // 如果迷你聊天栏打开，则不处理按钮缩放，防止覆盖位移
            if (_isMiniChatOpen) return;

            // 根据鼠标位置计算窗口需要的轻微偏移量
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;
            double offsetX = (windowMousePos.X - centerX) / centerX * MAX_WINDOW_SHIFT;
            double offsetY = (windowMousePos.Y - centerY) / centerY * MAX_WINDOW_SHIFT;
            _contentShift.X = offsetX;
            _contentShift.Y = offsetY;

            // 让径向菜单控件自行处理按钮缩放
            if (_radialMenu != null)
            {
                _radialMenu.UpdateButtonScales(windowMousePos);
            }
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

            // 从窗口句柄创建HwndSource对象，用于处理Windows消息
            // HwndSource是WPF中处理底层Windows消息的核心类
            HwndSource source = HwndSource.FromHwnd(hwnd);

            // 添加消息钩子，将我们的消息处理函数注册到Windows消息循环中
            // 这样就能接收到系统发送给窗口的所有消息，包括热键消息
            source.AddHook(HwndHook);

            // 调用Windows API注册全局热键 Alt+R
            // 参数说明：hwnd-窗口句柄，HOTKEY_ID-热键标识符，MOD_ALT-Alt修饰键，VK_R-R键
            RegisterHotKey(hwnd, HOTKEY_ID, MOD_ALT, VK_R);
        }

        // 主窗口关闭事件处理器，负责清理系统资源
        // CancelEventArgs允许取消关闭操作，但这里我们只是清理资源
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 获取窗口句柄，使用链式调用简化代码
            var hwnd = new WindowInteropHelper(this).Handle;

            // 取消注册热键，释放系统资源
            // 全局热键是系统级资源，不释放会导致资源泄漏
            UnregisterHotKey(hwnd, HOTKEY_ID);
        }

        // Windows消息钩子处理函数，处理系统发送给窗口的消息
        // 这是一个底层的消息处理机制，直接与Windows消息循环交互
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 定义热键消息的常量值，0x0312是Windows系统中WM_HOTKEY消息的标识符
            const int WM_HOTKEY = 0x0312;

            // 检查是否收到热键消息，并且是我们注册的热键
            // msg是消息类型，wParam包含热键ID
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // 调用显示窗口的方法
                ShowAtMouse();

                // 设置handled为true，告诉系统我们已经处理了这个消息
                // 这防止消息继续传播到其他处理器
                handled = true;
            }

            // 返回IntPtr.Zero表示消息处理完成
            // 这是Windows消息处理的标准返回值
            return IntPtr.Zero;
        }

        // 在鼠标当前位置显示窗口的核心方法
        // 这个方法处理了DPI缩放、坐标转换等复杂的显示逻辑
        private void ShowAtMouse()
        {
            // 获取当前窗口的呈现源，用于DPI缩放计算
            // PresentationSource是WPF中连接逻辑坐标和物理坐标的桥梁
            PresentationSource source = PresentationSource.FromVisual(this);

            // 初始化DPI缩放因子，默认值1.0表示100%缩放（96 DPI）
            double dpiX = 1.0, dpiY = 1.0;

            // 检查呈现源和合成目标是否存在，使用空条件运算符?.避免空引用异常
            if (source?.CompositionTarget != null)
            {
                // 获取设备变换矩阵的缩放因子
                // M11和M22分别是X轴和Y轴的缩放比例，用于DPI感知计算
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            // 获取鼠标在屏幕上的物理像素位置
            // System.Windows.Forms.Cursor.Position返回的是物理像素坐标
            var screenPos = System.Windows.Forms.Cursor.Position;

            // 将物理像素坐标转换为WPF的逻辑像素坐标
            double logicalX = screenPos.X / dpiX;
            double logicalY = screenPos.Y / dpiY;

            // 计算窗口位置，使窗口中心对准鼠标位置
            // ActualWidth和ActualHeight是窗口的实际渲染尺寸
            Left = logicalX - ActualWidth / 2;
            Top = logicalY - ActualHeight / 2;

            // 显示窗口，从隐藏状态变为可见状态
            Show();

            // 重新生成径向按钮，防止关闭后按钮停留在中心
            GenerateRadialButtons();

            // 激活窗口，使其获得焦点并置于最前端
            Activate();
        }

        // 生成径向分布按钮的主要方法，实现圆形菜单布局
        // 这个方法在窗口加载和尺寸改变时被调用
        private void GenerateRadialButtons()
        {
            if (MainCanvas != null)
            {
                MainCanvas.Children.Clear();

                // 始终保持妹抖酱在中心
                if (MeidoImage != null)
                {
                    MainCanvas.Children.Add(MeidoImage);
                    PositionMeidoInCenter();
                }

                // 确保径向菜单控件已添加
                if (_radialMenu != null && !MainCanvas.Children.Contains(_radialMenu))
                {
                    MainCanvas.Children.Add(_radialMenu);
                    Canvas.SetLeft(_radialMenu, 0);
                    Canvas.SetTop(_radialMenu, 0);
                    Canvas.SetZIndex(_radialMenu, 1);
                }
            }

            // 更新并重新生成按钮
            if (_radialMenu != null)
            {
                _radialMenu.Width = ActualWidth;
                _radialMenu.Height = ActualHeight;
                _radialMenu.IsMiniChatOpen = _isMiniChatOpen;
                _radialMenu.Regenerate();
            }

            // 在 MainCanvas 清空并重新添加中心妹抖图像后，若迷你聊天栏开启则一并重新添加
            if (_isMiniChatOpen && _miniChatContainer != null)
            {
                if (!MainCanvas.Children.Contains(_miniChatContainer))
                {
                    MainCanvas.Children.Add(_miniChatContainer);
                }

                // 重新计算聊天栏位置，以防窗口大小或布局变化
                PositionMiniChat();
            }
        }

        // 计算按钮在圆周上位置的数学方法
        // 使用极坐标系统将按钮均匀分布在圆周上
        private Point CalculateButtonPosition(int index, int total, double radius, double startAngle = 0, double angleRange = 2 * Math.PI)
        {
            double angle;
            if (total == 1)
            {
                // 仅有一个按钮时，直接放在圆弧中点
                angle = startAngle + angleRange / 2;
            }
            else
            {
                // 当覆盖整圆（360°）时避免首尾重叠
                if (Math.Abs(angleRange - 2 * Math.PI) < 0.0001)
                {
                    angle = startAngle + angleRange * index / total;
                }
                else
                {
                    // 非整圆时保持端点对齐，使两端按钮位于起始角和结束角
                    angle = startAngle + angleRange * index / (total - 1);
                }
            }

            // 计算窗口的中心点坐标，作为圆形布局的圆心
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            // 使用三角函数将极坐标转换为直角坐标
            // Math.Cos(angle)计算X轴分量，Math.Sin(angle)计算Y轴分量
            return new Point(
                centerX + radius * Math.Cos(angle),  // X坐标 = 圆心X + 半径 * cos(角度)
                centerY + radius * Math.Sin(angle)   // Y坐标 = 圆心Y + 半径 * sin(角度)
            );
        }

        // 创建径向菜单按钮的工厂方法
        // 根据菜单项数据创建配置好的按钮控件
        private Button CreateRadialButton(RadialMenuItem item)
        {
            var button = new Button
            {
                Content = item.Icon,
                ToolTip = item.ToolTip,
                Width = 50,
                Height = 50,
                FontSize = 24,
                RenderTransformOrigin = new Point(0.5, 0.5) // 设置变换原点为中心
            };

            // 从App.xaml中查找样式
            Style radialButtonStyle = Application.Current.FindResource("RadialButtonStyle") as Style;
            if (radialButtonStyle != null)
            {
                button.Style = radialButtonStyle;
            }

            // 关联点击事件以执行命令
            button.Click += (sender, e) => ExecuteCommand(item.Command);

            return button;
        }

        // 命令执行方法，根据不同的命令类型执行相应的操作
        // 这里使用了命令模式，将操作封装为命令对象
        private void ExecuteCommand(ICommand command)
        {
            // 使用if-else链检查命令类型并执行相应操作
            // 这种方式简单直接，适合命令数量较少的情况
            if (command == MenuCommands.OpenNotepad)
            {
                // 启动Windows记事本程序
                // Process.Start是.NET中启动外部程序的标准方法
                Process.Start("notepad.exe");
            }
            else if (command == MenuCommands.LockWorkstation)
            {
                LockComputer();
            }
            else if (command == MenuCommands.OpenAiChat)
            {
                OpenAiChatWindow(new List<ChatMessage>(_miniChatHistory));
            }
            else if (command == MenuCommands.OpenSettings)
            {
                OpenSettingsWindow();
            }
            else
            {
                // 对于其他类型的命令，直接调用命令的Execute方法
                // 这提供了扩展性，允许添加实现ICommand接口的自定义命令
                command.Execute(null);
            }

            // 执行命令后隐藏窗口，提供良好的用户体验
            // 用户选择操作后菜单自动消失，避免界面混乱
            Hide();
        }

        // 声明Windows API函数LockWorkStation，用于锁定工作站
        // DllImport特性指定从user32.dll导入此函数
        // SetLastError = true允许我们获取详细的错误信息
        [DllImport("user32.dll", SetLastError = true)]
        static extern void LockWorkStation();

        private void LockComputer()
        {
            // 调用Windows API锁定工作站
            LockWorkStation();
        }

        /// 打开聊天窗口
        /// 如果窗口已存在则激活，否则创建新窗口
        private void OpenAiChatWindow(List<ChatMessage> initialMessages)
        {
            try
            {
                // 在打开聊天窗口前，先隐藏主窗口内容
                HideMainWindowContent();
                
                var chatWindow = new ChatWindow();
                chatWindow.Show();

                // 追加历史
                chatWindow.AppendMiniChatHistory(initialMessages);

                chatWindow.Activate();
                
                // 监听聊天窗口关闭事件，恢复主窗口状态
                chatWindow.Closed += (s, e) => RestoreMainWindowContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开妹抖酱的聊天窗口: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 如果打开失败，也要恢复主窗口状态
                RestoreMainWindowContent();
            }
        }

        /// 打开设置
        private void OpenSettingsWindow()
        {
            try
            {
                // 在打开设置窗口前，先隐藏主窗口内容
                HideMainWindowContent();
                
                // 创建窗口
                var settingsWindow = new SettingsWindow();

                // 以模态对话框形式显示
                // 确保用户必须完成设置操作后才能继续
                settingsWindow.ShowDialog();
                
                // 设置窗口关闭后，恢复主窗口状态
                RestoreMainWindowContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开设置窗口: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 隐藏主窗口内容（女仆、按钮、迷你聊天等）
        private void HideMainWindowContent()
        {
            // 隐藏女仆图片
            if (MeidoImage != null)
            {
                MeidoImage.Visibility = Visibility.Hidden;
            }
            
            // 隐藏径向菜单
            if (_radialMenu != null)
            {
                _radialMenu.Visibility = Visibility.Hidden;
            }
            
            // 隐藏所有径向按钮
            foreach (Button btn in MainCanvas.Children.OfType<Button>())
            {
                btn.Visibility = Visibility.Hidden;
            }
            
            // 隐藏迷你聊天容器
            if (_miniChatContainer != null)
            {
                _miniChatContainer.Visibility = Visibility.Hidden;
            }
        }

        /// 恢复主窗口内容显示
        private void RestoreMainWindowContent()
        {
            // 恢复女仆图片显示
            if (MeidoImage != null)
            {
                MeidoImage.Visibility = Visibility.Visible;
            }
            
            // 恢复径向菜单显示
            if (_radialMenu != null)
            {
                _radialMenu.Visibility = Visibility.Visible;
            }
            
            // 恢复所有径向按钮显示
            foreach (Button btn in MainCanvas.Children.OfType<Button>())
            {
                btn.Visibility = Visibility.Visible;
            }
            
            // 如果迷你聊天是打开状态，强制关闭并重置为待机状态
            if (_isMiniChatOpen)
            {
                HideMiniChat();
            }
            
            // 确保女仆图片显示为待机状态
            SetMeidoImage(MeidoStandbyImagePath);
        }

        /// 将女仆定位到圆盘中心
        private void PositionMeidoInCenter()
        {
            if (MeidoImage != null)
            {
                // 计算窗口中心位置
                double centerX = ActualWidth / 2;
                double centerY = ActualHeight / 2;

                // 将女仆图片定位到中心
                Canvas.SetLeft(MeidoImage, centerX - MeidoImage.Width / 2);
                Canvas.SetTop(MeidoImage, centerY - MeidoImage.Height / 2);

                // 添加入场动画
                AnimateMeidoEntrance();
            }
        }

        /// 入场动画效果
        private void AnimateMeidoEntrance()
        {
            if (MeidoImage != null)
            {
                // 创建缩放动画
                var scaleTransform = new ScaleTransform(0.1, 0.1);
                MeidoImage.RenderTransform = scaleTransform;

                // 缩放动画
                var scaleAnimation = new DoubleAnimation
                {
                    From = 0.1,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };

                // 透明度动画
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 0.9,
                    Duration = TimeSpan.FromMilliseconds(400)
                };

                // 开始动画
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                MeidoImage.BeginAnimation(OpacityProperty, opacityAnimation);
            }
        }

        /// 女仆悬停动画效果
        private void AnimateMeidoHover(bool isHovering)
        {
            if (MeidoImage != null)
            {
                var scaleTransform = MeidoImage.RenderTransform as ScaleTransform ?? new ScaleTransform();
                MeidoImage.RenderTransform = scaleTransform;

                double targetScale = isHovering ? 1.1 : 1.0;
                double targetOpacity = isHovering ? 1.0 : 0.9;

                var scaleAnimation = new DoubleAnimation
                {
                    To = targetScale,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var opacityAnimation = new DoubleAnimation
                {
                    To = targetOpacity,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                MeidoImage.BeginAnimation(OpacityProperty, opacityAnimation);
            }
        }

        // 关闭动画
        private async void StartCloseAnimation()
        {
            if (_isClosingAnimationRunning) return;
            _isClosingAnimationRunning = true;

            const int durationMs = 120;
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            // 为每个径向按钮创建动画
            foreach (Button btn in MainCanvas.Children.OfType<Button>())
            {
                // 组合变换：保留现有的缩放，再加入旋转与收缩
                TransformGroup group = btn.RenderTransform as TransformGroup;
                ScaleTransform scale = null;
                if (group == null)
                {
                    group = new TransformGroup();
                    if (btn.RenderTransform is ScaleTransform existingScale)
                    {
                        scale = existingScale;
                        group.Children.Add(scale);
                    }
                    else
                    {
                        scale = new ScaleTransform(1, 1);
                        group.Children.Add(scale);
                    }
                    var rotate = new RotateTransform(0);
                    group.Children.Add(rotate);
                    btn.RenderTransform = group;
                    btn.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                else
                {
                    // 查找/补充Scale与Rotate
                    scale = group.Children.OfType<ScaleTransform>().FirstOrDefault() ?? new ScaleTransform(1, 1);
                    if (!group.Children.Contains(scale)) group.Children.Insert(0, scale);
                    if (!group.Children.OfType<RotateTransform>().Any())
                    {
                        group.Children.Add(new RotateTransform(0));
                    }
                }

                RotateTransform rotateTransform = group.Children.OfType<RotateTransform>().First();

                Storyboard sb = new Storyboard { Duration = TimeSpan.FromMilliseconds(durationMs) };

                // 旋转动画
                var rotateAnim = new DoubleAnimation(360, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(rotateAnim, rotateTransform);
                Storyboard.SetTargetProperty(rotateAnim, new PropertyPath(RotateTransform.AngleProperty));
                sb.Children.Add(rotateAnim);

                // 缩放动画
                var scaleAnim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(scaleAnim, scale);
                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                sb.Children.Add(scaleAnim);
                var scaleAnimY = scaleAnim.Clone();
                Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath(ScaleTransform.ScaleYProperty));
                sb.Children.Add(scaleAnimY);

                // 位移动画（向中心收缩）
                double targetLeft = centerX - btn.Width / 2;
                double targetTop = centerY - btn.Height / 2;
                var moveX = new DoubleAnimation(targetLeft, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(moveX, btn);
                Storyboard.SetTargetProperty(moveX, new PropertyPath("(Canvas.Left)"));
                sb.Children.Add(moveX);
                var moveY = new DoubleAnimation(targetTop, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(moveY, btn);
                Storyboard.SetTargetProperty(moveY, new PropertyPath("(Canvas.Top)"));
                sb.Children.Add(moveY);

                sb.Begin();
            }

            // MeidoImage 收缩淡出
            if (MeidoImage != null)
            {
                var meidoScale = MeidoImage.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                MeidoImage.RenderTransform = meidoScale;
                var meidoSb = new Storyboard { Duration = TimeSpan.FromMilliseconds(durationMs) };

                var meidoScaleAnim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(meidoScaleAnim, meidoScale);
                Storyboard.SetTargetProperty(meidoScaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                meidoSb.Children.Add(meidoScaleAnim);
                var meidoScaleAnimY = meidoScaleAnim.Clone();
                Storyboard.SetTargetProperty(meidoScaleAnimY, new PropertyPath(ScaleTransform.ScaleYProperty));
                meidoSb.Children.Add(meidoScaleAnimY);

                var opacityAnim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(durationMs));
                Storyboard.SetTarget(opacityAnim, MeidoImage);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
                meidoSb.Children.Add(opacityAnim);

                meidoSb.Begin();
            }

            // 使用 FillBehavior.Stop 使动画结束后不再冻结属性值
            var shiftAnimX = new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs))
            {
                FillBehavior = FillBehavior.Stop
            };
            var shiftAnimY = new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs))
            {
                FillBehavior = FillBehavior.Stop
            };

            _contentShift.BeginAnimation(TranslateTransform.XProperty, shiftAnimX);
            _contentShift.BeginAnimation(TranslateTransform.YProperty, shiftAnimY);

            // 等待动画完成后清除动画并重置位移，确保下次打开正常
            await Task.Delay(durationMs + 20);
            _contentShift.BeginAnimation(TranslateTransform.XProperty, null);
            _contentShift.BeginAnimation(TranslateTransform.YProperty, null);
            _contentShift.X = 0;
            _contentShift.Y = 0;

            // 隐藏窗口
            Hide();
            _isClosingAnimationRunning = false;
        }

        #region 迷你聊天实现

        /// 切换迷你聊天栏显示/隐藏
        private void ToggleMiniChat()
        {
            if (_isMiniChatOpen)
            {
                HideMiniChat();
            }
            else
            {
                ShowMiniChat();
            }
        }

        /// 创建并显示迷你聊天栏
        private void ShowMiniChat()
        {
            if (_miniChatContainer == null)
            {
                // 初始化 UI 组件
                _miniChatContainer = new Border
                {
                    // 让迷你聊天栏本身完全透明，只留下内部的气泡
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    Effect = null
                };

                var root = new StackPanel { Margin = new Thickness(10) };
                _miniChatPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                _miniChatInput = new TextBox
                {
                    MinWidth = 140,
                    Height = 26,
                    FontSize = 12,
                    Padding = new Thickness(4),
                    Style = Application.Current.TryFindResource("MiniChatTextBoxStyle") as Style
                };
                _miniChatInput.KeyDown += async (s, e) =>
                {
                    if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        await MiniChatSend();
                        e.Handled = true;
                    }
                };

                root.Children.Add(_miniChatPanel);
                root.Children.Add(_miniChatInput);
                _miniChatContainer.Child = root;

                _miniChatContainer.SizeChanged += (_, __) => PositionMiniChat();
            }

            PositionMiniChat();

            if (!MainCanvas.Children.Contains(_miniChatContainer))
            {
                MainCanvas.Children.Add(_miniChatContainer);
            }

            _miniChatInput.Focus();

            // 确保输入框为空，开始新的对话
            _miniChatInput.Clear();

            _isMiniChatOpen = true;

            SetMeidoImage(MeidoChattingImagePath);

            // 重新排布按钮到左半圆
            GenerateRadialButtons();

            // 初始化 ApiService
            if (_miniApiService == null)
            {
                _miniSettings = AppSettings.Load();
                if (_miniSettings.IsValid())
                {
                    _miniApiService = new ApiService(_miniSettings);
                }
            }
        }

        /// 隐藏并清理迷你聊天栏
        private void HideMiniChat()
        {
            if (_miniChatContainer != null && MainCanvas.Children.Contains(_miniChatContainer))
            {
                MainCanvas.Children.Remove(_miniChatContainer);
            }

            _isMiniChatOpen = false;
            _miniChatRoundCount = 0;

            // 清空迷你聊天历史与界面，避免下次打开时显示旧对话
            if (_miniChatPanel != null)
            {
                _miniChatPanel.Children.Clear();
            }

            _miniChatHistory.Clear();

            SetMeidoImage(MeidoStandbyImagePath);

            GenerateRadialButtons(); // 恢复完整圆形布局
        }

        /// 根据妹抖酱位置计算聊天栏放置位置
        private void PositionMiniChat()
        {
            if (MeidoImage == null || _miniChatContainer == null) return;

            double meidoLeft = Canvas.GetLeft(MeidoImage);
            double meidoTop = Canvas.GetTop(MeidoImage);

            double chatLeft = meidoLeft + MeidoImage.Width + 12; // 默认右侧偏移
            double chatTop = meidoTop + (MeidoImage.Height - _miniChatContainer.ActualHeight) / 2;

            // 计算所需窗口大小，确保聊天框完整可见
            double requiredRight = chatLeft + _miniChatContainer.ActualWidth + 10; // 额外 10px 边距
            if (requiredRight > Width)
            {
                Width = requiredRight;
            }

            // 防止顶部和底部超出窗口可视区域
            if (chatTop < 10) chatTop = 10;
            if (chatTop + _miniChatContainer.ActualHeight > ActualHeight)
            {
                chatTop = ActualHeight - _miniChatContainer.ActualHeight - 10;
            }

            Canvas.SetLeft(_miniChatContainer, chatLeft);
            Canvas.SetTop(_miniChatContainer, chatTop);
        }

        /// 发送迷你聊天消息
        private async Task MiniChatSend()
        {
            string text = _miniChatInput?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // 显示用户气泡
            AddMiniBubble(text, true);
            _miniChatHistory.Add(new ChatMessage("user", text));

            _miniChatInput.Clear();

            if (_miniApiService == null)
            {
                MessageBox.Show("需要先配置API，才能与妹抖酱聊天哦~", "配置缺失", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 发送完整历史（含本次用户消息）
            string reply = await _miniApiService.SendMessageAsync(new List<ChatMessage>(_miniChatHistory));

            // 显示 AI 回复 ( /// 分句)
            var sentences = SplitAiMessage(reply);
            foreach (var s in sentences)
            {
                AddMiniBubble(s.Trim(), false);
                _miniChatHistory.Add(new ChatMessage("assistant", s.Trim()));
            }

            _miniChatRoundCount++;

            // 超过3轮转到窗口聊天
            if (_miniChatRoundCount >= 3)
            {
                OpenAiChatWindow(new List<ChatMessage>(_miniChatHistory));
                HideMiniChat();
                _miniChatHistory.Clear();
            }
        }

        /// 在迷你聊天面板添加气泡
        private void AddMiniBubble(string msg, bool isUser)
        {
            if (_miniChatPanel == null) return;

            var bubble = new Border
            {
                Background = isUser ? ((Brush)Application.Current.TryFindResource("MeidoThemeColor") ?? new SolidColorBrush(Color.FromRgb(0xE8, 0x74, 0x75)))
                                   : new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(isUser ? 40 : 0, 2, isUser ? 0 : 40, 2),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 180
            };

            var txt = new TextBlock
            {
                Text = msg,
                FontSize = 11,
                Foreground = isUser ? Brushes.White : Brushes.Black,
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = txt;
            _miniChatPanel.Children.Add(bubble);

            // 保持最多7条气泡，避免过长
            if (_miniChatPanel.Children.Count > 7)
            {
                _miniChatPanel.Children.RemoveAt(0);
            }
        }

        /// 分割 AI 回复
        private List<string> SplitAiMessage(string message)
        {
            return message.Split(new string[] { @"\\\" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        #endregion
    }
}
