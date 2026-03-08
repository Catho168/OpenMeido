using System.Reflection;
using System.Runtime.InteropServices;
using OpenMeido.Infrastructure;

namespace OpenMeido.Tests;

public sealed class MainWindowHotkeyPlatformTests
{
    [Fact]
    public void NativeImports_TargetActualUser32EntryPoints()
    {
        AssertImportEntryPoint("RegisterHotKeyNative", "RegisterHotKey");
        AssertImportEntryPoint("UnregisterHotKeyNative", "UnregisterHotKey");
    }

    private static void AssertImportEntryPoint(string methodName, string expectedEntryPoint)
    {
        var method = typeof(MainWindowHotkeyPlatform).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var importAttribute = Assert.Single(method!.GetCustomAttributes<DllImportAttribute>());
        Assert.Equal("user32.dll", importAttribute.Value, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expectedEntryPoint, importAttribute.EntryPoint);
    }
}