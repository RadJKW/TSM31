namespace TSM31.Core.Services.Config;

internal static class NativeDialog
{
    public static void ShowError(string title, string message)
    {
#if WINDOWS
        try
        {
            MessageBoxW(IntPtr.Zero, message, title, (uint)(MessageBoxType.MB_OK | MessageBoxType.MB_ICONERROR | MessageBoxType.MB_TOPMOST | MessageBoxType.MB_SETFOREGROUND));
            return;
        }
        catch
        {
            // fall through to console fallback
        }
#endif
#if BROWSER
        // Running in WebAssembly: no blocking native dialog available here without JS interop; fallback to console.
        Console.Error.WriteLine($"{title}: {message}");
#else
        Console.Error.WriteLine($"{title}: {message}");
#endif
    }

#if WINDOWS
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [Flags]
    private enum MessageBoxType : uint
    {
        MB_OK = 0x00000000,
        MB_ICONERROR = 0x00000010,
        MB_TOPMOST = 0x00040000,
        MB_SETFOREGROUND = 0x00010000
    }
#endif
}
