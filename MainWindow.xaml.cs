// 引入系统运行时互操作服务，用于调用Windows API函数
using System.Runtime.InteropServices;
// 引入WPF窗口互操作功能，用于获取窗口句柄和处理Windows消息
using System.Windows.Interop;
// 引入3D媒体功能（虽然本项目中未直接使用，但可能用于高级变换）
using System.Windows.Media.Media3D;
// 引入WPF核心功能，包括窗口、控件等基础类
using System.Windows;
// 引入.NET基础系统功能，如数学计算、异常处理等
using System;
// 引入进程管理功能，用于启动外部程序
using System.Diagnostics;
// 引入泛型集合功能，用于存储和管理菜单项列表
using System.Collections.Generic;
// 引入WPF控件功能，如按钮、画布等UI元素
using System.Windows.Controls;
// 引入WPF输入处理功能，如鼠标、键盘事件和命令模式
using System.Windows.Input;
// 引入WPF媒体功能，用于变换、动画和视觉效果
using System.Windows.Media;
// 引入WPF动画功能，用于女仆动画效果
using System.Windows.Media.Animation;
// 引入WPF图像功能，用于女仆图片显示
using System.Windows.Media.Imaging;
// 引入LINQ查询功能，用于集合的筛选和操作
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

        // 新增: 内容平移变换与动画状态
        private readonly TranslateTransform _contentShift = new TranslateTransform();
        private const double MAX_WINDOW_SHIFT = 7; // 窗口随鼠标漂移的最大像素
        private bool _isClosingAnimationRunning = false;

        // ==== 迷你聊天相关字段 ====
        private bool _isMiniChatOpen = false;          // 迷你聊天栏是否打开
        private int _miniChatRoundCount = 0;           // 对话轮次计数
        private Border _miniChatContainer;             // 聊天UI容器
        private TextBox _miniChatInput;                // 输入框
        private StackPanel _miniChatPanel;             // 用于显示问答气泡
        private ApiService _miniApiService;            // 复用 ApiService
        private AppSettings _miniSettings;             // 设置
        private List<ChatMessage> _miniChatHistory = new List<ChatMessage>();

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
                    Icon = "📝",  // 使用Unicode表情符号作为图标，跨平台兼容性好
                    Command = MenuCommands.OpenNotepad,  // 引用预定义的命令对象
                    ToolTip = "打开记事本"  // 设置鼠标悬停时显示的提示文本
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
            // 如果迷你聊天栏已打开，则不自动关闭，避免干扰对话
            if (!_isMiniChatOpen)
            {
                // 触发关闭动画，而不是立即隐藏
                StartCloseAnimation();
            }
        }

        // 全局鼠标跟踪事件处理器，实现鼠标悬停时按钮的动态缩放效果
        // 这是实现"磁性"用户界面的核心方法
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

            // 遍历画布中的所有按钮控件，使用LINQ的OfType<Button>()方法筛选出Button类型的子元素
            // MainCanvas.Children返回UIElementCollection，包含画布上的所有子控件
            foreach (Button btn in MainCanvas.Children.OfType<Button>())
            {
                // 为每个按钮更新缩放效果，传入按钮引用和鼠标位置
                UpdateButtonScale(btn, windowMousePos);
            }
        }

        // 更新按钮缩放效果的核心算法，实现基于距离的动态缩放
        // 实现磁性效果
        private void UpdateButtonScale(Button button, Point mousePos)
        {
            // 计算按钮的几何中心点坐标
            // Canvas.GetLeft()获取按钮在画布中的X坐标，ActualWidth是按钮的实际渲染宽度
            double btnCenterX = Canvas.GetLeft(button) + button.ActualWidth / 2;
            // 同理计算Y坐标中心点
            double btnCenterY = Canvas.GetTop(button) + button.ActualHeight / 2;

            // 计算鼠标位置与按钮中心的距离差值
            // 这是二维平面上两点间距离计算的第一步
            double deltaX = mousePos.X - btnCenterX;
            double deltaY = mousePos.Y - btnCenterY;

            // 勾股定理计算距离
            // Math.Sqrt()计算平方根，得到鼠标到按钮中心的实际像素距离
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // 定义影响半径，超过此距离的鼠标位置不会影响按钮缩放
            double maxDist = 150; // 影响半径设为150像素

            // 使用指数衰减函数计算缩放因子，实现平滑的距离衰减效果
            // Math.Exp()是自然指数函数，-distance * 3 / maxDist确保距离越远缩放效果越小
            // 基础缩放为1（原始大小），最大额外缩放为1（即最大2倍大小）
            double scaleFactor = 1 + 1 * Math.Exp(-distance * 3 / maxDist);

            // 创建并应用缩放变换到按钮
            // ScaleTransform实现等比例缩放，两个参数分别是X轴和Y轴的缩放比例
            // RenderTransform属性控制控件的渲染变换，不影响布局计算
            button.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
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
            // 这样我们就能接收到系统发送给窗口的所有消息，包括热键消息
            source.AddHook(HwndHook);

            // 调用Windows API注册全局热键 Alt+R
            // 参数说明：hwnd-窗口句柄，HOTKEY_ID-热键标识符，MOD_ALT-Alt修饰键，VK_R-R键
            RegisterHotKey(hwnd, HOTKEY_ID, MOD_ALT, VK_R);
        }

        // 主窗口关闭事件处理器，负责清理系统资源
        // CancelEventArgs允许我们取消关闭操作，但这里我们只是清理资源
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 获取窗口句柄，使用链式调用简化代码
            var hwnd = new WindowInteropHelper(this).Handle;

            // 取消注册热键，释放系统资源
            // 这很重要，因为全局热键是系统级资源，不释放会导致资源泄漏
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
                // 调用显示窗口的方法，在鼠标位置显示菜单
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
            // 这个转换对于高DPI显示器（如4K显示器）非常重要
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
            // 清除画布上的所有现有子元素，为重新生成按钮做准备
            // MainCanvas是在XAML中定义的Canvas控件，用作按钮的容器
            if (MainCanvas != null)
            {
                MainCanvas.Children.Clear();

                // 重新添加女仆图片（确保她始终在中心）
                if (MeidoImage != null)
                {
                    MainCanvas.Children.Add(MeidoImage);
                    PositionMeidoInCenter();
                }
            }

            // 计算径向分布的半径，取窗口宽度和高度中较小值的30%
            // 这确保按钮始终在窗口可见区域内，并保持合适的间距
            double radius = Math.Min(ActualWidth, ActualHeight) * 0.3;

            // 如果迷你聊天栏打开，则将按钮分布在左半圆
            double startAngle = 0;
            double angleRange = 2 * Math.PI;
            if (_isMiniChatOpen)
            {
                startAngle = Math.PI / 2;          // 90° （上）
                angleRange = Math.PI;               // 180° 覆盖左侧
            }

            // 遍历所有菜单项，为每个菜单项创建对应的按钮
            for (int i = 0; i < menuItems.Count; i++)
            {
                // 计算当前按钮在圆周上的位置坐标
                var position = CalculateButtonPosition(i, menuItems.Count, radius, startAngle, angleRange);

                // 根据菜单项数据创建按钮控件
                var button = CreateRadialButton(menuItems[i]);

                // 设置按钮在画布中的位置，减去按钮宽度的一半实现中心对齐
                // Canvas.SetLeft和Canvas.SetTop是Canvas控件的附加属性设置方法
                Canvas.SetLeft(button, position.X - button.Width / 2);
                Canvas.SetTop(button, position.Y - button.Height / 2);

                // 将按钮添加到画布的子元素集合中
                MainCanvas.Children.Add(button);
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
                angle = startAngle + angleRange / 2; // 居中
            }
            else
            {
                angle = startAngle + angleRange * index / (total - 1);
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
                // 调用锁定计算机的方法
                LockComputer();
            }
            else if (command == MenuCommands.OpenAiChat)
            {
                // 打开聊天窗口
                OpenAiChatWindow(new List<ChatMessage>(_miniChatHistory));
            }
            else if (command == MenuCommands.OpenSettings)
            {
                // 打开妹抖酱设置窗口
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

        // 锁定计算机的包装方法，提供更友好的接口
        // 这个方法封装了Windows API调用，使代码更易读和维护
        private void LockComputer()
        {
            // 调用Windows API锁定工作站
            LockWorkStation();
        }

        /// 打开妹抖酱聊天窗口
        /// 如果窗口已存在则激活，否则创建新窗口
        private void OpenAiChatWindow(List<ChatMessage> initialMessages)
        {
            try
            {
                var chatWindow = new ChatWindow();
                chatWindow.Show();

                // 追加历史
                chatWindow.AppendMiniChatHistory(initialMessages);

                chatWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开妹抖酱的聊天窗口: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 打开设置窗口
        /// 以模态对话框形式显示设置界面
        private void OpenSettingsWindow()
        {
            try
            {
                // 创建设置窗口
                var settingsWindow = new SettingsWindow();

                // 以模态对话框形式显示设置窗口
                // 这确保用户必须完成设置操作后才能继续使用其他功能
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                // 如果打开设置窗口失败，显示错误消息
                MessageBox.Show($"无法打开设置窗口: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 将女仆图片定位到圆盘中心
        /// 让女仆始终在菜单的中心位置
        private void PositionMeidoInCenter()
        {
            if (MeidoImage != null)
            {
                // 计算窗口中心位置
                double centerX = ActualWidth / 2;
                double centerY = ActualHeight / 2;

                // 将女仆图片定位到中心（考虑图片自身的尺寸）
                Canvas.SetLeft(MeidoImage, centerX - MeidoImage.Width / 2);
                Canvas.SetTop(MeidoImage, centerY - MeidoImage.Height / 2);

                // 添加可爱的入场动画
                AnimateMeidoEntrance();
            }
        }

        /// 女仆入场动画效果
        private void AnimateMeidoEntrance()
        {
            if (MeidoImage != null)
            {
                // 创建缩放动画（从小到正常大小）
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

        // 新增: 关闭动画
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

            // 轻微平移回归
            _contentShift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs)));
            _contentShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(durationMs)));

            // 等待动画完成后隐藏窗口
            await Task.Delay(durationMs + 20);
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
                    // 不额外添加整体阴影，避免出现一个明显的整体矩形轮廓
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

        /// 根据 \\\ 分割 AI 回复
        private List<string> SplitAiMessage(string message)
        {
            return message.Split(new string[] { @"\\\" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        #endregion
    }
}
