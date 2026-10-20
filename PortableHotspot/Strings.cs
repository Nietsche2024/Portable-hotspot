static class L
{
    // 0 = Arabic, 1 = Deutsch, 2 = English
    public static int Lang = 0;
    public static int Index(string code) => code switch { "ar" => 0, "de" => 1, _ => 2 };

    static readonly Dictionary<string, string[]> M = new()
    {
        ["tabHot"] = new[] { "الهوت سبوت", "Hotspot", "Hotspot" },
        ["tabDev"] = new[] { "الأجهزة", "Geräte", "Devices" },
        ["tabStat"] = new[] { "الإحصائيات", "Statistik", "Statistics" },
        ["tabSet"] = new[] { "الإعدادات", "Einstellungen", "Settings" },
        ["source"] = new[] { "مصدر الإنترنت", "Internetquelle", "Internet source" },
        ["refresh"] = new[] { "تحديث", "Aktualisieren", "Refresh" },
        ["ssid"] = new[] { "اسم الشبكة", "Netzwerkname", "Network name" },
        ["pass"] = new[] { "كلمة السر", "Passwort", "Password" },
        ["band"] = new[] { "التردد", "Band", "Band" },
        ["gen"] = new[] { "توليد", "Zufällig", "Random" },
        ["show"] = new[] { "إظهار كلمة السر", "Passwort anzeigen", "Show password" },
        ["copy"] = new[] { "نسخ", "Kopieren", "Copy" },
        ["start"] = new[] { "تشغيل الهوت سبوت", "Hotspot starten", "Start hotspot" },
        ["stop"] = new[] { "إيقاف", "Stoppen", "Stop" },
        ["scan"] = new[] { "امسح الكود بكاميرا الآيفون للاتصال مباشرة", "QR-Code mit der iPhone-Kamera scannen", "Scan with the iPhone camera to join" },
        ["ready"] = new[] { "جاهز. وصّل كابل الإيثرنت ثم اضغط تشغيل.", "Bereit. Ethernet verbinden und Starten drücken.", "Ready. Plug in Ethernet and press Start." },
        ["noSource"] = new[] { "ما في اتصال إنترنت متاح. تأكد من كابل الإيثرنت.", "Keine Internetverbindung gefunden. Ethernet prüfen.", "No internet connection found. Check the Ethernet cable." },
        ["passShort"] = new[] { "كلمة السر لازم تكون 8 أحرف على الأقل.", "Passwort braucht mindestens 8 Zeichen.", "Password needs at least 8 characters." },
        ["ssidEmpty"] = new[] { "اكتب اسم للشبكة.", "Bitte einen Netzwerknamen eingeben.", "Enter a network name." },
        ["running"] = new[] { "شغّال ✔ — اتصل من الآيفون بالشبكة:", "Läuft ✔ — mit dem iPhone verbinden:", "Running ✔ — join from the iPhone:" },
        ["stopped"] = new[] { "تم الإيقاف.", "Gestoppt.", "Stopped." },
        ["restarted"] = new[] { "انقطع الهوت سبوت وتمت إعادة تشغيله تلقائياً.", "Hotspot wurde automatisch neu gestartet.", "Hotspot dropped and was restarted automatically." },
        ["clients"] = new[] { "الأجهزة المتصلة", "Verbundene Geräte", "Connected devices" },
        ["colName"] = new[] { "الاسم", "Name", "Name" },
        ["colMac"] = new[] { "عنوان MAC", "MAC-Adresse", "MAC address" },
        ["adapter"] = new[] { "كرت الشبكة", "Adapter", "Adapter" },
        ["down"] = new[] { "تحميل", "Download", "Download" },
        ["up"] = new[] { "رفع", "Upload", "Upload" },
        ["total"] = new[] { "المجموع منذ تشغيل البرنامج", "Gesamt seit Programmstart", "Total since launch" },
        ["test"] = new[] { "فحص الإنترنت", "Internet testen", "Test internet" },
        ["testing"] = new[] { "جاري الفحص…", "Teste…", "Testing…" },
        ["online"] = new[] { "الإنترنت شغّال", "Internet OK", "Internet OK" },
        ["offline"] = new[] { "ما في إنترنت", "Kein Internet", "No internet" },
        ["dnsFail"] = new[] { "الاتصال موجود لكن الـ DNS ما بيشتغل", "Verbindung da, aber DNS funktioniert nicht", "Connected, but DNS fails" },
        ["optAuto"] = new[] { "شغّل الهوت سبوت تلقائياً عند فتح البرنامج", "Hotspot beim Start automatisch einschalten", "Start hotspot automatically on launch" },
        ["optKeep"] = new[] { "أعد التشغيل تلقائياً إذا انقطع الهوت سبوت", "Bei Abbruch automatisch neu starten", "Auto-restart if the hotspot drops" },
        ["optWin"] = new[] { "شغّل البرنامج مع ويندوز (بدون أدمن)", "Mit Windows starten (ohne Admin)", "Run with Windows (no admin needed)" },
        ["optTray"] = new[] { "صغّر إلى شريط المهام (Tray) عند التصغير", "Beim Minimieren in den Infobereich", "Minimize to system tray" },
        ["lang"] = new[] { "اللغة", "Sprache", "Language" },
        ["about"] = new[] { "الإعدادات بتنحفظ بملف بجانب البرنامج (بورتابل).", "Einstellungen liegen portabel neben der EXE.", "Settings are stored portably next to the EXE." },
        ["trayShow"] = new[] { "إظهار", "Anzeigen", "Show" },
        ["trayExit"] = new[] { "خروج", "Beenden", "Exit" },
        ["cap1"] = new[] { "الهوت سبوت ممنوع بسياسة المجموعة (Group Policy) على هذا الجهاز. البرنامج ما بيتجاوز هالقفل.", "Hotspot ist per Gruppenrichtlinie gesperrt. Das Programm umgeht das nicht.", "Hotspot is blocked by Group Policy on this PC. This tool does not bypass that." },
        ["cap2"] = new[] { "كرت الواي فاي ما بيدعم الهوت سبوت أو ما في كرت واي فاي.", "Der WLAN-Adapter unterstützt keinen Hotspot oder fehlt.", "The Wi-Fi adapter doesn't support hotspot or is missing." },
        ["cap3"] = new[] { "الميزة معطّلة من المشغّل.", "Vom Anbieter deaktiviert.", "Disabled by the operator." },
        ["cap4"] = new[] { "إصدار ويندوز هذا ما بيدعم الهوت سبوت.", "Diese Windows-Edition unterstützt keinen Hotspot.", "This Windows edition doesn't support hotspot." },
        ["cap5"] = new[] { "ميزة مطلوبة غير مثبتة.", "Eine erforderliche App fehlt.", "A required app is not installed." },
        ["cap6"] = new[] { "الهوت سبوت معطّل لسبب غير معروف.", "Hotspot aus unbekanntem Grund deaktiviert.", "Hotspot disabled for an unknown reason." },
        ["err"] = new[] { "خطأ", "Fehler", "Error" },
    };

    public static string T(string k) => M.TryGetValue(k, out var v) ? v[Lang] : k;
}
