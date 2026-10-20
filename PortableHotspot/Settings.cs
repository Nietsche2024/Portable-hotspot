using System.Text.Json;

class AppSettings
{
    public string Ssid { get; set; } = "MeinHotspot";
    public string Password { get; set; } = "";
    public int Band { get; set; } = 0;
    public string Language { get; set; } = "ar";
    public bool AutoStart { get; set; } = false;
    public bool KeepAlive { get; set; } = true;
    public bool RunWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public string Source { get; set; } = "";

    static string FilePath()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        try
        {
            var t = Path.Combine(exeDir, ".write-test");
            File.WriteAllText(t, "");
            File.Delete(t);
            return Path.Combine(exeDir, "hotspot-settings.json");
        }
        catch
        {
            var d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PortableHotspot");
            Directory.CreateDirectory(d);
            return Path.Combine(d, "hotspot-settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            var p = FilePath();
            if (File.Exists(p)) return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(p)) ?? new AppSettings();
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try { File.WriteAllText(FilePath(), JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); }
        catch { }
    }
}
