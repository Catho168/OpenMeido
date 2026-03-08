using System.Windows;
using System.Windows.Media;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowInteractionPlatform : IMainWindowInteractionPlatform
    {
        public Point GetCursorScreenPosition()
        {
            var cursorPosition = System.Windows.Forms.Cursor.Position;
            return new Point(cursorPosition.X, cursorPosition.Y);
        }

        public Point GetDpiScale(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source?.CompositionTarget == null)
            {
                return new Point(1.0, 1.0);
            }

            return new Point(
                source.CompositionTarget.TransformToDevice.M11,
                source.CompositionTarget.TransformToDevice.M22);
        }

        public void Show(Window window)
        {
            window.Show();
        }

        public void Activate(Window window)
        {
            window.Activate();
        }
    }
}