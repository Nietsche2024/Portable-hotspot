using System.Drawing;
using System.Security.Cryptography;
using QRCoder;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

class HotspotService
{
    public NetworkOperatorTetheringManager? Mgr;

    public static List<ConnectionProfile> GetSources()
    {
        var list = new List<ConnectionProfile>();
        try
        {
            foreach (var p in NetworkInformation.GetConnectionProfiles())
                if (p.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                    list.Add(p);
        }
        catch { }
        return list;
    }

    public static string Kind(ConnectionProfile p)
    {
        uint t = p.NetworkAdapter?.IanaInterfaceType ?? 0;
        return t switch { 6 => "Ethernet", 71 => "Wi-Fi", 243 or 244 => "Mobile", _ => "Network" };
    }

    public async Task<(bool ok, string msg)> StartAsync(ConnectionProfile p, string ssid, string pass, int band)
    {
        var cap = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(p);
        if (cap != TetheringCapability.Enabled)
        {
            string key = cap switch
            {
                TetheringCapability.DisabledByGroupPolicy => "cap1",
                TetheringCapability.DisabledByHardwareLimitation => "cap2",
                TetheringCapability.DisabledByOperator => "cap3",
                TetheringCapability.DisabledBySku => "cap4",
                TetheringCapability.DisabledByRequiredAppNotInstalled => "cap5",
                _ => "cap6"
            };
            return (false, L.T(key));
        }

        Mgr = NetworkOperatorTetheringManager.CreateFromConnectionProfile(p);
        var cfg = new NetworkOperatorTetheringAccessPointConfiguration { Ssid = ssid, Passphrase = pass };
        try
        {
            cfg.Band = band switch
            {
                1 => TetheringWiFiBand.TwoPointFourGigahertz,
                2 => TetheringWiFiBand.FiveGigahertz,
                _ => TetheringWiFiBand.Auto
            };
        }
        catch { }
        await Mgr.ConfigureAccessPointAsync(cfg);

        if (Mgr.TetheringOperationalState == TetheringOperationalState.On) return (true, "");
        var r = await Mgr.StartTetheringAsync();
        return r.Status == TetheringOperationStatus.Success
            ? (true, "")
            : (false, $"{L.T("err")}: {r.Status} {r.AdditionalErrorMessage}");
    }

    public async Task StopAsync()
    {
        if (Mgr != null && Mgr.TetheringOperationalState != TetheringOperationalState.Off)
            await Mgr.StopTetheringAsync();
    }

    public static string RandomPassword(int len = 10)
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var c = new char[len];
        for (int i = 0; i < len; i++) c[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(c);
    }

    static string Esc(string s) =>
        new string(s.SelectMany(c => "\\;,:\"".Contains(c) ? new[] { '\\', c } : new[] { c }).ToArray());

    public static Bitmap? MakeQr(string ssid, string pass)
    {
        try
        {
            string payload = $"WIFI:T:WPA;S:{Esc(ssid)};P:{Esc(pass)};;";
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var png = new PngByteQRCode(data).GetGraphic(8);
            using var ms = new MemoryStream(png);
            using var img = Image.FromStream(ms);
            return new Bitmap(img);
        }
        catch { return null; }
    }
}
