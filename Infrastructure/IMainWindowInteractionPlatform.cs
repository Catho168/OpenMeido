using System.Windows;
using System.Windows.Media;

namespace OpenMeido.Infrastructure
{
    public interface IMainWindowInteractionPlatform
    {
        Point GetCursorScreenPosition();

        Point GetDpiScale(Visual visual);

        void Show(Window window);

        void Activate(Window window);
    }
}