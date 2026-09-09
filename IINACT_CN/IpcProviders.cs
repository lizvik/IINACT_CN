using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace IINACT_CN;

internal class IpcProviders : IDisposable
{
    internal static Version IpcVersion => new(2, 1, 0);
    internal readonly ICallGateProvider<Version> GetVersion;
    internal readonly ICallGateProvider<Version> GetIpcVersion;

    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }
    public RainbowMage.OverlayPlugin.Handlers.Ipc.IpcHandlerController? OverlayIpcHandler { get; set; }

    internal readonly ICallGateProvider<string, bool> CreateSubscriber;
    internal readonly ICallGateProvider<string, bool> CreateLegacySubscriber;
    internal readonly ICallGateProvider<string, bool> Unsubscribe;
    internal readonly ICallGateProvider<bool> GetServerRunning;
    internal readonly ICallGateProvider<int> GetServerPort;
    internal readonly ICallGateProvider<string> GetServerIp;
    internal readonly ICallGateProvider<bool> GetServerSslEnabled;
    internal readonly ICallGateProvider<Uri?> GetServerUri;
    
    internal IpcProviders(IDalamudPluginInterface pluginInterface)
    {
        GetVersion = pluginInterface.GetIpcProvider<Version>("IINACT_CN.Version");
        GetIpcVersion = pluginInterface.GetIpcProvider<Version>("IINACT_CN.IpcVersion");
        
        CreateSubscriber = pluginInterface.GetIpcProvider<string, bool>("IINACT_CN.CreateSubscriber");
        CreateLegacySubscriber = pluginInterface.GetIpcProvider<string, bool>("IINACT_CN.CreateLegacySubscriber");
        Unsubscribe = pluginInterface.GetIpcProvider<string, bool>("IINACT_CN.Unsubscribe");
        
        GetServerRunning = pluginInterface.GetIpcProvider<bool>("IINACT_CN.Server.Listening");
        GetServerPort = pluginInterface.GetIpcProvider<int>("IINACT_CN.Server.Port");
        GetServerIp = pluginInterface.GetIpcProvider<string>("IINACT_CN.Server.Ip");
        GetServerSslEnabled = pluginInterface.GetIpcProvider<bool>("IINACT_CN.Server.SslEnabled");
        GetServerUri = pluginInterface.GetIpcProvider<Uri?>("IINACT_CN.Server.Uri");
        
        Register();
    }

    private void Register()
    {
        GetVersion.RegisterFunc(() => Assembly.GetExecutingAssembly().GetName().Version!);
        GetIpcVersion.RegisterFunc(() => IpcVersion);
        
        CreateSubscriber.RegisterFunc(name => OverlayIpcHandler?.CreateSubscriber(name) ?? false);
        CreateLegacySubscriber.RegisterFunc(name => OverlayIpcHandler?.CreateLegacySubscriber(name) ?? false);
        Unsubscribe.RegisterFunc(name => OverlayIpcHandler?.Unsubscribe(name) ?? false);

        GetServerRunning.RegisterFunc(() => Server?.Running ?? false);
        GetServerPort.RegisterFunc(() => Server?.Port ?? 0);
        GetServerIp.RegisterFunc(() => Server?.Address ?? "");
        GetServerSslEnabled.RegisterFunc(() => Server?.Secure ?? false);
        GetServerUri.RegisterFunc(() => Server?.Uri);
    }

    public void Dispose()
    {
        GetVersion.UnregisterFunc();
        GetIpcVersion.UnregisterFunc();
        
        CreateSubscriber.UnregisterFunc();
        CreateLegacySubscriber.UnregisterFunc();
        Unsubscribe.UnregisterFunc();

        GetServerRunning.UnregisterFunc();
        GetServerPort.UnregisterFunc();
        GetServerIp.UnregisterFunc();
        GetServerSslEnabled.UnregisterFunc();
        GetServerUri.UnregisterFunc();
    }
}
