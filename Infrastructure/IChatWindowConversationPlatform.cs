using System.Windows;

namespace OpenMeido.Infrastructure
{
    public interface IChatWindowConversationPlatform
    {
        MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons, MessageBoxImage image);

        bool? OpenSettingsDialog(Window owner);
    }
}