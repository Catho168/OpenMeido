using System;
using System.Windows;
using System.Runtime.InteropServices;

namespace OpenMeido
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. 修复DPI缩放问题
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            // 2. 初始化应用
            base.OnStartup(e);

            // 注释掉主窗口初始化，避免编译错误
            var mainWin = new MainWindow();
            mainWin.Show();
            mainWin.Hide();
        }

        // 可选：崩溃捕获（调试用）
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"程序崩溃了:\n{e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}