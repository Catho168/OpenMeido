using System;
using System.Windows;
using System.Runtime.InteropServices;
using OpenMeido.Infrastructure.DI;

namespace OpenMeido
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        protected override void OnStartup(StartupEventArgs e)
        {
            // DPI缩放
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            // 初始化应用
            Services = ServiceRegistration.ConfigureServices();
            base.OnStartup(e);

            var mainWin = Services.GetService(typeof(MainWindow)) as MainWindow ?? new MainWindow();
            MainWindow = mainWin;
            mainWin.Show();
            mainWin.Hide();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }

        // 可选：崩溃捕获（调试用）
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"程序崩溃了:\n{e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}