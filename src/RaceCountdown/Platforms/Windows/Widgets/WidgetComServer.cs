using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace RaceCountdown.Widgets;

/// <summary>
/// Runs the exe as the widget provider's out-of-process COM server (plan D5, spike S2). The Widgets Board starts the
/// exe with <see cref="Argument"/> (see <c>com:ExeServer</c> in <c>Package.appxmanifest</c>); the MAUI UI is not started.
/// </summary>
internal static partial class WidgetComServer
{
    public const string Argument = "-RegisterProcessAsComServer";

    /// <summary>The provider's COM class id (plan §10). Must match <c>com:Class</c> and <c>CreateInstance</c> in the manifest.</summary>
    public static readonly Guid ProviderClsid = new("AF609A3B-CBBB-4A48-BE42-84FF9CF5F9B2");

    private const uint ClsctxLocalServer = 0x4;
    private const uint RegclsMultipleUse = 0x1;

    public static bool IsRequested(string[] args) => args.Any(a => string.Equals(a, Argument, StringComparison.OrdinalIgnoreCase));

    /// <summary>Serves the provider until the last widget is removed. Blocks the calling thread.</summary>
    public static void Run()
    {
        // Calls from the board arrive on RPC threads, so serve from the multithreaded apartment, not Main's STA.
        var server = new Thread(Serve) { Name = "Widget COM server" };
        server.SetApartmentState(ApartmentState.MTA);
        server.Start();
        server.Join();
    }

    private static void Serve()
    {
        using var noWidgetsLeft = new ManualResetEvent(false);
        var factory = new WidgetProviderFactory(() => new CountdownWidgetProvider(noWidgetsLeft));

        var wrappers = new StrategyBasedComWrappers();
        var unknown = wrappers.GetOrCreateComInterfaceForObject(factory, CreateComInterfaceFlags.None);
        try
        {
            Marshal.ThrowExceptionForHR(CoRegisterClassObject(ProviderClsid, unknown, ClsctxLocalServer, RegclsMultipleUse, out var cookie));
            noWidgetsLeft.WaitOne();
            CoRevokeClassObject(cookie);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoRegisterClassObject(in Guid rclsid, nint pUnk, uint dwClsContext, uint flags, out uint lpdwRegister);

    [LibraryImport("ole32.dll")]
    private static partial int CoRevokeClassObject(uint dwRegister);
}

[GeneratedComInterface]
[Guid("00000001-0000-0000-C000-000000000046")]
internal partial interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject);

    [PreserveSig]
    int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

/// <summary>Hands the board the single provider instance, so all widgets share one timer and feed store.</summary>
[GeneratedComClass]
internal sealed partial class WidgetProviderFactory(Func<CountdownWidgetProvider> create) : IClassFactory
{
    private const int ClassENoAggregation = unchecked((int)0x80040110);

    private readonly Lazy<CountdownWidgetProvider> provider = new(create);

    public int CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject)
    {
        ppvObject = 0;
        if (pUnkOuter != 0)
        {
            return ClassENoAggregation;
        }

        try
        {
            var inspectable = MarshalInspectable<IWidgetProvider>.FromManaged(provider.Value);
            try
            {
                return Marshal.QueryInterface(inspectable, riid, out ppvObject);
            }
            finally
            {
                Marshal.Release(inspectable);
            }
        }
        catch (Exception ex)
        {
            // Returned to the board as the exception's HRESULT.
            Debug.WriteLine($"Creating the widget provider failed: {ex}");
            return ex.HResult;
        }
    }

    public int LockServer(bool fLock) => 0;
}
