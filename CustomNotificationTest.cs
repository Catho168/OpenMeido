using System;
using System.Windows;

namespace OpenMeido
{
    /// <summary>
    /// 自定义通知窗口测试类
    /// </summary>
    public static class CustomNotificationTest
    {
        /// <summary>
        /// 测试自定义通知窗口
        /// </summary>
        public static void TestNotification()
        {
            try
            {
                // 测试不同类型的通知
                var result1 = CustomNotificationWindow.Show("这是一个信息提示", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"信息提示结果: {result1}");

                var result2 = CustomNotificationWindow.Show("这是一个警告提示", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine($"警告提示结果: {result2}");

                var result3 = CustomNotificationWindow.Show("这是一个错误提示", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"错误提示结果: {result3}");

                var result4 = CustomNotificationWindow.Show("您确定要执行此操作吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                Console.WriteLine($"确认提示结果: {result4}");

                var result5 = CustomNotificationWindow.Show("设置已更改但未保存，是否要保存更改？", "未保存的更改", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                Console.WriteLine($"未保存更改提示结果: {result5}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}