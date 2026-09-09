using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FetchDependencies;

public class FetchDependencies
{
    private const string LatestReleaseUrlGlobal =
        "https://api.github.com/repos/ravahn/FFXIV_ACT_Plugin/releases/latest";
    private const string LatestReleaseUrlChinese =
        "https://api.github.com/repos/NewMoe-Technology/FFXIV_ACT_Plugin_CN/releases/latest";
    private const string ChinesePluginAssetName = "FFXIV_ACT_Plugin.zip";

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private bool IsChinese { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(Version version, string assemblyDir, bool isChinese, HttpClient httpClient)
    {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        IsChinese = isChinese;
        HttpClient = httpClient;
    }

    public void GetFfxivPlugin()
    {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");
        var release = GetLatestRelease();

        if (IsCurrentVersion(pluginPath, release.Version))
            return;

        try
        {
            File.Delete(pluginZipPath);
            DownloadFile(release.DownloadUrl, pluginZipPath);
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);

            if (!File.Exists(pluginPath))
                throw new InvalidDataException("The downloaded archive does not contain FFXIV_ACT_Plugin.dll.");
        }
        finally
        {
            File.Delete(pluginZipPath);
        }

        foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
            File.Delete(deucalionDll);

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private (Version Version, string DownloadUrl) GetLatestRelease()
    {
        var releaseUrl = IsChinese ? LatestReleaseUrlChinese : LatestReleaseUrlGlobal;
        using var request = new HttpRequestMessage(HttpMethod.Get, releaseUrl);
        request.Headers.UserAgent.ParseAdd("IINACT_CN/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = HttpClient.Send(request);
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream();
        var release = JsonNode.Parse(stream);
        var tagName = release?["tag_name"]?.ToString();
        var versionMatch = Regex.Match(tagName ?? string.Empty, @"\d+(?:\.\d+){1,3}");

        if (!versionMatch.Success || !Version.TryParse(versionMatch.Value, out var releaseVersion))
            throw new InvalidDataException($"Could not determine the plugin version from release tag '{tagName}'.");

        var assets = release?["assets"]?.AsArray()
                     ?? throw new InvalidDataException("The latest GitHub release does not contain an asset list.");

        var pluginAsset = assets.FirstOrDefault(asset => IsPluginZipAsset(asset?["name"]?.ToString()));
        var downloadUrl = pluginAsset?["browser_download_url"]?.ToString();

        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidDataException("Could not find the FFXIV_ACT_Plugin ZIP in the latest GitHub release.");

        return (releaseVersion, downloadUrl);
    }

    private static bool IsCurrentVersion(string pluginPath, Version releaseVersion)
    {
        if (!File.Exists(pluginPath))
            return false;

        try
        {
            using var plugin = new TargetAssembly(pluginPath);
            return plugin.Version >= releaseVersion;
        }
        catch
        {
            return false;
        }
    }

    private bool IsPluginZipAsset(string? assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return false;

        if (IsChinese)
            return assetName.Equals(ChinesePluginAssetName, StringComparison.OrdinalIgnoreCase);

        return assetName.StartsWith("FFXIV_ACT_Plugin", StringComparison.OrdinalIgnoreCase)
               && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private void DownloadFile(string url, string path)
    {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
                                   .GetStreamAsync(url,
                                                   cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
    }
}
