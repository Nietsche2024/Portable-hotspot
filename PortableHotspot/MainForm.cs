using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

class MainForm : Form
{
    readonly AppSettings S = AppSettings.Load();
    readonly HotspotService svc = new();
    List<ConnectionProfile> sources = new();
    bool wantOn, busy, loading = true;
    int tick, retryCooldown;

    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly TabPage tHot = new(), tDev = new(), tStat = new(), tSet = new();

    // hotspot tab
    readonly Label lSource = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly Label lSsid = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly Label lPass = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly Label lBand = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    readonly ComboBox cbSource = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    readonly ComboBox cbBand = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    readonly TextBox tbSsid = new() { Dock = DockStyle.Fill };
    readonly TextBox tbPass = new() { Dock = DockStyle.Fill };
    readonly CheckBox chkShow = new() { AutoSize = true, Checked = true };
    readonly Button btnRefresh = new() { Dock = DockStyle.Fill, AutoSize = true };
    readonly Button btnGen = new() { Dock = DockStyle.Fill, AutoSize = true };
    readonly Button btnCopy = new() { Dock = DockStyle.Fill, AutoSize = true };
    readonly Button btnStart = new() { Height = 40, Width = 220 };
    readonly Button btnStop = new() { Height = 40, Width = 120, Enabled = false };
    readonly PictureBox qr = new() { Width = 200, Height = 200, SizeMode = PictureBoxSizeMode.Zoom, Anchor = AnchorStyles.None };
    readonly Label lblScan = new() { AutoSize = true, Anchor = AnchorStyles.None };
    readonly Label status = new() { AutoSize = true, MaximumSize = new Size(500, 0), Anchor = AnchorStyles.None };

    // devices tab
    readonly Label lblClients = new() { Dock = DockStyle.Top, Height = 32 };
    readonly ListView lv = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };

    // stats tab
    readonly Label lblAdapter = new() { AutoSize = true };
    readonly Label lblDown = new() { AutoSize = true };
    readonly Label lblUp = new() { AutoSize = true };
    readonly Label lblTotal = new() { AutoSize = true };
    readonly Button btnTest = new() { AutoSize = true };
    readonly Label lblTest = new() { AutoSize = true, MaximumSize = new Size(480, 0) };

    // settings tab
    readonly CheckBox chkAuto = new() { AutoSize = true };
    readonly CheckBox chkKeep = new() { AutoSize = true };
    readonly CheckBox chkWin = new() { AutoSize = true };
    readonly CheckBox chkTray = new() { AutoSize = true };
    readonly Label lblLang = new() { AutoSize = true };
    readonly ComboBox cbLang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    readonly Label lblAbout = new() { AutoSize = true, MaximumSize = new Size(480, 0) };

    readonly NotifyIcon tray = new() { Icon = SystemIcons.Application, Visible = false };
    readonly ContextMenuStrip trayMenu = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };

    string? adapterId;
    long lastRx, lastTx, baseRx = -1, baseTx;

    public MainForm()
    {
        L.Lang = L.Index(S.Language);
        if (string.IsNullOrEmpty(S.Password)) S.Password = HotspotService.RandomPassword();

        Text = "Portable Hotspot";
        ClientSize = new Size(560, 660);
        MinimumSize = new Size(560, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        BuildUi();

        cbBand.Items.AddRange(new object[] { "Auto", "2.4 GHz", "5 GHz" });
        cbBand.SelectedIndex = Math.Clamp(S.Band, 0, 2);
        cbLang.Items.AddRange(new object[] { "العربية", "Deutsch", "English" });
        cbLang.SelectedIndex = L.Lang;
        tbSsid.Text = S.Ssid;
        tbPass.Text = S.Password;
        chkAuto.Checked = S.AutoStart;
        chkKeep.Checked = S.KeepAlive;
        chkWin.Checked = S.RunWithWindows;
        chkTray.Checked = S.MinimizeToTray;

        LoadSources();
        ApplyLanguage();
        UpdateQr();
        loading = false;

        // events
        btnRefresh.Click += (_, _) => LoadSources();
        btnGen.Click += (_, _) => tbPass.Text = HotspotService.RandomPassword();
        btnCopy.Click += (_, _) => { try { Clipboard.SetText(tbPass.Text); } catch { } };
        chkShow.CheckedChanged += (_, _) => tbPass.UseSystemPasswordChar = !chkShow.Checked;
        tbSsid.TextChanged += (_, _) => UpdateQr();
        tbPass.TextChanged += (_, _) => UpdateQr();
        btnStart.Click += async (_, _) => await StartAsync();
        btnStop.Click += async (_, _) => await StopAsync();
        btnTest.Click += async (_, _) => await TestInternetAsync();
        cbLang.SelectedIndexChanged += (_, _) =>
        {
            if (loading) return;
            S.Language = cbLang.SelectedIndex switch { 0 => "ar", 1 => "de", _ => "en" };
            L.Lang = cbLang.SelectedIndex;
            ApplyLanguage();
            SaveSettings();
        };
        foreach (var c in new[] { chkAuto, chkKeep, chkWin, chkTray })
            c.CheckedChanged += (_, _) => { if (!loading) SaveSettings(); };
        cbBand.SelectedIndexChanged += (_, _) => { if (!loading) SaveSettings(); };

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized && chkTray.Checked)
            {
                Hide();
                tray.Visible = true;
            }
        };
        tray.DoubleClick += (_, _) => RestoreFromTray();
        trayMenu.Items.Add("", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("", null, (_, _) => Close());
        tray.ContextMenuStrip = trayMenu;

        FormClosing += (_, _) =>
        {
            SaveSettings();
            try { svc.Mgr?.StopTetheringAsync().AsTask().Wait(3000); } catch { }
            tray.Visible = false;
            tray.Dispose();
        };

        timer.Tick += (_, _) => OnTick();
        timer.Start();

        Shown += async (_, _) => { if (S.AutoStart) await StartAsync(); };
    }

    void BuildUi()
    {
        tabs.TabPages.AddRange(new[] { tHot, tDev, tStat, tSet });
        Controls.Add(tabs);

        // --- hotspot tab ---
        var g = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(10), AutoScroll = true };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        void Add(Control a, Control b, Control? c = null)
        {
            g.Controls.Add(a); g.Controls.Add(b); g.Controls.Add(c ?? new Control { Height = 1 });
        }
        Add(lSource, cbSource, btnRefresh);
        Add(lSsid, tbSsid);
        Add(lPass, tbPass, btnGen);
        Add(new Control { Height = 1 }, chkShow, btnCopy);
        Add(lBand, cbBand);

        var btns = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 10) };
        btns.Controls.Add(btnStart);
        btns.Controls.Add(btnStop);
        g.Controls.Add(btns); g.SetColumnSpan(btns, 3);
        g.Controls.Add(qr); g.SetColumnSpan(qr, 3);
        g.Controls.Add(lblScan); g.SetColumnSpan(lblScan, 3);
        g.Controls.Add(status); g.SetColumnSpan(status, 3);
        tHot.Controls.Add(g);

        // --- devices tab ---
        lv.Columns.Add("", 240);
        lv.Columns.Add("", 200);
        tDev.Controls.Add(lv);
        tDev.Controls.Add(lblClients);
        tDev.Padding = new Padding(8);

        // --- stats tab ---
        var sp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), WrapContents = false };
        foreach (Control c in new Control[] { lblAdapter, lblDown, lblUp, lblTotal, btnTest, lblTest })
        {
            c.Margin = new Padding(3, 8, 3, 8);
            sp.Controls.Add(c);
        }
        tStat.Controls.Add(sp);

        // --- settings tab ---
        var set = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), WrapContents = false };
        foreach (Control c in new Control[] { chkAuto, chkKeep, chkWin, chkTray, lblLang, cbLang, lblAbout })
        {
            c.Margin = new Padding(3, 8, 3, 8);
            set.Controls.Add(c);
        }
        tSet.Controls.Add(set);
    }

    void ApplyLanguage()
    {
        bool rtl = L.Lang == 0;
        RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = rtl;

        tHot.Text = L.T("tabHot"); tDev.Text = L.T("tabDev"); tStat.Text = L.T("tabStat"); tSet.Text = L.T("tabSet");
        lSource.Text = L.T("source"); lSsid.Text = L.T("ssid"); lPass.Text = L.T("pass"); lBand.Text = L.T("band");
        btnRefresh.Text = L.T("refresh"); btnGen.Text = L.T("gen"); btnCopy.Text = L.T("copy");
        chkShow.Text = L.T("show");
        btnStart.Text = L.T("start"); btnStop.Text = L.T("stop");
        lblScan.Text = L.T("scan");
        lv.Columns[0].Text = L.T("colName"); lv.Columns[1].Text = L.T("colMac");
        btnTest.Text = L.T("test");
        chkAuto.Text = L.T("optAuto"); chkKeep.Text = L.T("optKeep"); chkWin.Text = L.T("optWin"); chkTray.Text = L.T("optTray");
        lblLang.Text = L.T("lang"); lblAbout.Text = L.T("about");
        trayMenu.Items[0].Text = L.T("trayShow"); trayMenu.Items[1].Text = L.T("trayExit");
        if (!wantOn) status.Text = L.T("ready");
        UpdateClients();
    }

    void LoadSources()
    {
        sources = HotspotService.GetSources();
        cbSource.Items.Clear();
        foreach (var p in sources) cbSource.Items.Add($"{p.ProfileName} ({HotspotService.Kind(p)})");
        int idx = sources.FindIndex(p => p.ProfileName == S.Source);
        if (idx < 0)
            idx = sources.FindIndex(p => HotspotService.Kind(p) == "Ethernet");
        if (cbSource.Items.Count > 0) cbSource.SelectedIndex = Math.Max(0, idx);
    }

    ConnectionProfile? SelectedProfile() =>
        cbSource.SelectedIndex >= 0 && cbSource.SelectedIndex < sources.Count ? sources[cbSource.SelectedIndex] : null;

    void UpdateQr()
    {
        if (tbPass.Text.Length < 8 || tbSsid.Text.Length == 0) { qr.Image = null; return; }
        qr.Image = HotspotService.MakeQr(tbSsid.Text, tbPass.Text);
    }

    void SaveSettings()
    {
        S.Ssid = tbSsid.Text;
        S.Password = tbPass.Text;
        S.Band = Math.Max(0, cbBand.SelectedIndex);
        S.AutoStart = chkAuto.Checked;
        S.KeepAlive = chkKeep.Checked;
        S.RunWithWindows = chkWin.Checked;
        S.MinimizeToTray = chkTray.Checked;
        S.Source = SelectedProfile()?.ProfileName ?? S.Source;
        S.Save();
        SetRunWithWindows(S.RunWithWindows);
    }

    static void SetRunWithWindows(bool on)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (k == null) return;
            if (on) k.SetValue("PortableHotspot", $"\"{Environment.ProcessPath}\"");
            else k.DeleteValue("PortableHotspot", false);
        }
        catch { }
    }

    void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        tray.Visible = false;
    }

    void SetRunning(bool on)
    {
        btnStart.Enabled = !on;
        btnStop.Enabled = on;
        tbSsid.Enabled = tbPass.Enabled = cbSource.Enabled = cbBand.Enabled = !on;
    }

    async Task StartAsync()
    {
        if (busy) return;
        busy = true;
        btnStart.Enabled = false;
        try
        {
            if (tbSsid.Text.Trim().Length == 0) { status.Text = L.T("ssidEmpty"); return; }
            if (tbPass.Text.Length < 8) { status.Text = L.T("passShort"); return; }
            var p = SelectedProfile();
            if (p == null) { status.Text = L.T("noSource"); return; }

            SaveSettings();
            var (ok, msg) = await svc.StartAsync(p, S.Ssid, S.Password, S.Band);
            if (ok)
            {
                wantOn = true;
                status.Text = $"{L.T("running")} {S.Ssid}";
                baseRx = -1; // reset traffic baseline
            }
            else status.Text = msg;
        }
        catch (Exception ex) { status.Text = $"{L.T("err")}: {ex.Message}"; }
        finally
        {
            busy = false;
            SetRunning(wantOn);
        }
    }

    async Task StopAsync()
    {
        if (busy) return;
        busy = true;
        try
        {
            wantOn = false;
            await svc.StopAsync();
            status.Text = L.T("stopped");
        }
        catch (Exception ex) { status.Text = $"{L.T("err")}: {ex.Message}"; }
        finally { busy = false; SetRunning(false); }
    }

    static string Fmt(double bytes)
    {
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        while (bytes >= 1024 && i < u.Length - 1) { bytes /= 1024; i++; }
        return $"{bytes:0.##} {u[i]}";
    }

    System.Net.NetworkInformation.NetworkInterface? FindAdapter()
    {
        var p = SelectedProfile();
        var id = p?.NetworkAdapter?.NetworkAdapterId.ToString();
        if (id == null) return null;
        return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => string.Equals(n.Id.Trim('{', '}'), id, StringComparison.OrdinalIgnoreCase));
    }

    void UpdateClients()
    {
        try
        {
            lv.Items.Clear();
            var m = svc.Mgr;
            if (m == null || m.TetheringOperationalState != TetheringOperationalState.On)
            {
                lblClients.Text = $"{L.T("clients")}: 0";
                return;
            }
            foreach (var c in m.GetTetheringClients())
            {
                string name = string.Join(", ", c.HostNames.Select(h => h.DisplayName));
                lv.Items.Add(new ListViewItem(new[] { name, c.MacAddress }));
            }
            lblClients.Text = $"{L.T("clients")}: {m.ClientCount} / {m.MaxClientCount}";
        }
        catch { }
    }

    void OnTick()
    {
        tick++;

        // traffic stats of the internet source adapter
        try
        {
            var ni = FindAdapter();
            if (ni != null)
            {
                var st = ni.GetIPv4Statistics();
                long rx = st.BytesReceived, tx = st.BytesSent;
                if (baseRx < 0 || ni.Id != adapterId) { baseRx = rx; baseTx = tx; lastRx = rx; lastTx = tx; adapterId = ni.Id; }
                lblAdapter.Text = $"{L.T("adapter")}: {ni.Name}";
                lblDown.Text = $"↓ {L.T("down")}: {Fmt(rx - lastRx)}/s";
                lblUp.Text = $"↑ {L.T("up")}: {Fmt(tx - lastTx)}/s";
                lblTotal.Text = $"{L.T("total")}: ↓ {Fmt(rx - baseRx)}   ↑ {Fmt(tx - baseTx)}";
                lastRx = rx; lastTx = tx;
            }
        }
        catch { }

        if (tick % 3 == 0) UpdateClients();

        // keep-alive: restart if Windows switched the hotspot off
        if (retryCooldown > 0) retryCooldown--;
        if (wantOn && chkKeep.Checked && !busy && retryCooldown == 0 && svc.Mgr != null)
        {
            try
            {
                if (svc.Mgr.TetheringOperationalState == TetheringOperationalState.Off)
                {
                    retryCooldown = 10;
                    _ = RestartAsync();
                }
            }
            catch { }
        }
    }

    async Task RestartAsync()
    {
        if (busy) return;
        busy = true;
        try
        {
            var r = await svc.Mgr!.StartTetheringAsync();
            if (r.Status == TetheringOperationStatus.Success) status.Text = L.T("restarted");
        }
        catch { }
        finally { busy = false; }
    }

    async Task TestInternetAsync()
    {
        btnTest.Enabled = false;
        lblTest.Text = L.T("testing");
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendAsync("1.1.1.1", 3000);
            if (reply.Status != System.Net.NetworkInformation.IPStatus.Success)
            {
                lblTest.Text = L.T("offline");
                return;
            }
            long ms = reply.RoundtripTime;
            try
            {
                await System.Net.Dns.GetHostEntryAsync("example.com");
                lblTest.Text = $"{L.T("online")} — {ms} ms";
            }
            catch { lblTest.Text = $"{L.T("dnsFail")} — {ms} ms"; }
        }
        catch { lblTest.Text = L.T("offline"); }
        finally { btnTest.Enabled = true; }
    }
}
