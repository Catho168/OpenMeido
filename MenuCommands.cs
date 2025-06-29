using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenMeido
{
    /// <summary>
    /// 菜单命令定义类，包含所有可用的菜单操作命令
    /// 使用静态命令对象实现命令模式，便于统一管理和扩展
    /// </summary>
    public static class MenuCommands
    {
        /// <summary>
        /// 打开记事本命令
        /// </summary>
        public static RoutedCommand OpenNotepad = new RoutedCommand();

        /// <summary>
        /// 锁定工作站命令
        /// </summary>
        public static RoutedCommand LockWorkstation = new RoutedCommand();

        /// <summary>
        /// 打开AI聊天窗口命令（替换原来的打开文档功能）
        /// </summary>
        public static RoutedCommand OpenAiChat = new RoutedCommand();

        /// <summary>
        /// 打开设置窗口命令
        /// </summary>
        public static RoutedCommand OpenSettings = new RoutedCommand();

        // 后续可扩展更多命令
    }
}