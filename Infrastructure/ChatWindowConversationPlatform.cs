using System.Windows;

namespace OpenMeido.Infrastructure
{
    public sealed class ChatWindowConversationPlatform : IChatWindowConversationPlatform
    {
        public MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return MessageBox.Show(message, title, buttons, image);
        }

        public bool? OpenSettingsDialog(Window owner)
        {
            var appServices = (Application.Current as App)?.Services;
            var settingsWindow = appServices?.GetService(typeof(SettingsWindow)) as SettingsWindow ?? new SettingsWindow();
            settingsWindow.Owner = owner;
            return settingsWindow.ShowDialog();
        }
    }
}