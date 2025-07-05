using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenMeido
{
    /// 菜单命令定义类，包含所有可用的菜单操作命令
    /// 使用静态命令对象实现命令模式，便于统一管理和扩展
    public static class MenuCommands
    {
        /// 打开记事本命令
        public static RoutedCommand OpenNotepad = new RoutedCommand();

        /// 锁定工作站命令
        public static RoutedCommand LockWorkstation = new RoutedCommand();

        /// 打开AI聊天窗口命令（替换原来的打开文档功能）
        public static RoutedCommand OpenAiChat = new RoutedCommand();

        /// 打开设置窗口命令
        public static RoutedCommand OpenSettings = new RoutedCommand();
    }
}