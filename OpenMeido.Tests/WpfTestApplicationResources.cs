using System;
using System.Linq;
using System.Windows;

namespace OpenMeido.Tests;

internal static class WpfTestApplicationResources
{
    public static Application EnsureLoaded()
    {
        var application = Application.Current ?? new Application();
        EnsureDictionary(application, "/OpenMeido;component/Themes/DesignTokens.xaml");
        EnsureDictionary(application, "/OpenMeido;component/Themes/ApplicationSharedResources.xaml");
        EnsureDictionary(application, "/OpenMeido;component/Themes/TitleBarButtonStyles.xaml");
        return application;
    }

    private static void EnsureDictionary(Application application, string source)
    {
        if (application.Resources.MergedDictionaries.Any(dictionary =>
                string.Equals(dictionary.Source?.OriginalString, source, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(source, UriKind.Relative)
        });
    }
}