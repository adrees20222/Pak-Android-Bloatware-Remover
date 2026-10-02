using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AndroidDebloater.Models;

namespace AndroidDebloater.Services
{
    public class DatabaseService
    {
        private readonly Dictionary<string, AppPackage> _packageDb = new(StringComparer.OrdinalIgnoreCase);

        public DatabaseService()
        {
            InitializeBuiltInDatabase();
            LoadCustomDatabase();
        }

        public AppPackage ResolvePackage(string packageName, string apkPath)
        {
            if (_packageDb.TryGetValue(packageName, out var existing))
            {
                return new AppPackage
                {
                    PackageName = existing.PackageName,
                    FriendlyName = existing.FriendlyName,
                    Description = existing.Description,
                    Brand = existing.Brand,
                    Safety = existing.Safety,
                    ApkPath = apkPath
                };
            }

            // Fallback heuristics: derive friendly name from APK path or package name
            var apkName = Path.GetFileNameWithoutExtension(apkPath);
            if (string.IsNullOrWhiteSpace(apkName))
            {
                var parts = packageName.Split('.');
                apkName = parts[^1];
            }

            var cleanName = System.Text.RegularExpressions.Regex.Replace(apkName, "([a-z])([A-Z])", "$1 $2")
                .Replace("_", " ")
                .Replace("Sec ", "Samsung ");

            var brand = "Universal";
            var safety = SafetyLevel.Optional;

            if (packageName.StartsWith("com.samsung.") || packageName.StartsWith("com.sec."))
            {
                brand = "Samsung";
            }
            else if (packageName.StartsWith("com.google.") || packageName.StartsWith("com.android.chrome"))
            {
                brand = "Google";
            }
            else if (packageName.StartsWith("com.miui.") || packageName.StartsWith("com.xiaomi."))
            {
                brand = "Xiaomi";
            }
            else if (packageName.StartsWith("com.facebook."))
            {
                brand = "Facebook";
                safety = SafetyLevel.Safe;
            }
            else if (packageName.StartsWith("com.microsoft."))
            {
                brand = "Microsoft";
                safety = SafetyLevel.Safe;
            }

            // System core protections
            if (packageName is "android" or "com.android.systemui" or "com.android.settings" or "com.android.phone" or "com.android.server.telecom")
            {
                safety = SafetyLevel.Caution;
            }

            return new AppPackage
            {
                PackageName = packageName,
                FriendlyName = cleanName,
                Description = "System / User Application",
                Brand = brand,
                Safety = safety,
                ApkPath = apkPath
            };
        }

        private void Register(string pkg, string name, string desc, string brand, SafetyLevel safety)
        {
            _packageDb[pkg] = new AppPackage
            {
                PackageName = pkg,
                FriendlyName = name,
                Description = desc,
                Brand = brand,
                Safety = safety
            };
        }

        private void InitializeBuiltInDatabase()
        {
            // --- Social / Ad / Third-Party Bloatware (Safe) ---
            Register("com.facebook.katana", "Facebook App", "Official Facebook social application", "Facebook", SafetyLevel.Safe);
            Register("com.facebook.system", "Facebook App Installer", "Background installer for Facebook apps & updates", "Facebook", SafetyLevel.Safe);
            Register("com.facebook.appmanager", "Facebook App Manager", "Background updater service for Facebook", "Facebook", SafetyLevel.Safe);
            Register("com.facebook.services", "Facebook Services", "Background telemetry and notifications service", "Facebook", SafetyLevel.Safe);
            Register("flipboard.boxer.app", "Flipboard / Briefing", "Left-screen news aggregator bloatware", "Flipboard", SafetyLevel.Safe);
            Register("com.microsoft.skydrive", "Microsoft OneDrive", "Microsoft cloud storage sync service", "Microsoft", SafetyLevel.Safe);
            Register("com.microsoft.office.officehubrow", "Microsoft Office", "Pre-installed Office Suite", "Microsoft", SafetyLevel.Safe);
            Register("com.microsoft.office.outlook", "Microsoft Outlook", "Microsoft email and calendar", "Microsoft", SafetyLevel.Safe);
            Register("com.microsoft.office.word", "Microsoft Word", "Word processor", "Microsoft", SafetyLevel.Safe);
            Register("com.microsoft.office.excel", "Microsoft Excel", "Spreadsheet application", "Microsoft", SafetyLevel.Safe);
            Register("com.microsoft.office.powerpoint", "Microsoft PowerPoint", "Presentation application", "Microsoft", SafetyLevel.Safe);
            Register("com.linkedin.android", "LinkedIn", "LinkedIn professional network", "Microsoft", SafetyLevel.Safe);
            Register("com.netflix.mediaclient", "Netflix", "Netflix streaming service", "Netflix", SafetyLevel.Safe);
            Register("com.netflix.partner.activation", "Netflix Partner Activation", "Pre-installed Netflix activation service", "Netflix", SafetyLevel.Safe);
            Register("com.hiya.star", "Hiya Smart Call", "Caller identification and spam lookups", "Samsung", SafetyLevel.Safe);

            // --- Samsung Bloatware & Optional Utilities ---
            Register("com.samsung.android.app.bikemode", "Samsung S Bike Mode", "Bike ride mode safety assistant", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.game.gamehome", "Samsung Game Launcher", "Hub for installed games and statistics", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.game.gametools", "Samsung Game Tools", "Overlay menu and recording tools for games", "Samsung", SafetyLevel.Safe);
            Register("com.enhance.gameservice", "Samsung Game Optimizing Service", "Game throttling & optimization background service", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.app.watchmanagerstub", "Galaxy Wearable Stub", "Placeholder to download Galaxy Wearable app", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.scloud", "Samsung Cloud", "Samsung cloud sync for notes/photos", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.samsungpass", "Samsung Pass", "Biometric password manager", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.samsungpassautofill", "Samsung Pass Autofill", "Autofill service for Samsung Pass", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.sbrowser", "Samsung Internet", "Stock Samsung web browser", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.shealth", "Samsung Health", "Fitness and step tracking app", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.app.notes", "Samsung Notes", "Stock Samsung note taking app", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.voicenote", "Samsung Voice Recorder", "Audio recording app", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.fm", "Samsung FM Radio", "FM Radio tuner app", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.popupcalculator", "Samsung Calculator", "Stock calculator app", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.myfiles", "Samsung My Files", "Stock file manager", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.email.provider", "Samsung Email", "Stock email client", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.video", "Samsung Video", "Stock video player", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.themecenter", "Samsung Themes", "Theme store for Wallpapers and Icons", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.app.clockpack", "Always On Display / Clockpack", "Lock screen and AOD styles", "Samsung", SafetyLevel.Optional);
            Register("com.sec.android.app.samsungapps", "Galaxy Store", "Samsung App Store", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.bixby.agent", "Bixby Voice / Agent", "Samsung Bixby voice assistant", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.app.spage", "Bixby Home / Free", "Swipe left Bixby news card", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.bixby.service", "Bixby Service", "Background service for Bixby AI", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.visionintelligence", "Bixby Vision", "Camera object/text recognition", "Samsung", SafetyLevel.Safe);
            Register("com.samsung.android.fmm", "Find My Mobile", "Remote device locating service", "Samsung", SafetyLevel.Optional);
            Register("com.samsung.android.kidsinstaller", "Kids Mode Installer", "Samsung Kids Mode setup launcher", "Samsung", SafetyLevel.Safe);

            // --- Monotype Fonts ---
            Register("com.monotype.android.font.cooljazz", "Font: Cool Jazz", "Pre-installed decorative font", "Samsung", SafetyLevel.Safe);
            Register("com.monotype.android.font.chococooky", "Font: Choco Cooky", "Pre-installed decorative font", "Samsung", SafetyLevel.Safe);
            Register("com.monotype.android.font.foundation", "Font: Foundation", "Pre-installed decorative font", "Samsung", SafetyLevel.Safe);

            // --- Google Suite ---
            Register("com.google.android.apps.docs", "Google Drive", "Cloud file storage & sync", "Google", SafetyLevel.Optional);
            Register("com.google.android.apps.maps", "Google Maps", "Navigation and maps", "Google", SafetyLevel.Optional);
            Register("com.google.android.apps.photos", "Google Photos", "Cloud photo gallery and backup", "Google", SafetyLevel.Optional);
            Register("com.google.android.gm", "Gmail", "Google email client", "Google", SafetyLevel.Optional);
            Register("com.google.android.music", "Google Play Music", "Deprecated Google music player", "Google", SafetyLevel.Safe);
            Register("com.google.android.videos", "Google TV / Movies", "Google video store & player", "Google", SafetyLevel.Safe);
            Register("com.google.android.talk", "Google Hangouts / Meet", "Google messaging / video calls", "Google", SafetyLevel.Safe);
            Register("com.google.android.youtube", "YouTube", "Official YouTube app", "Google", SafetyLevel.Optional);
            Register("com.google.android.marvin.talkback", "TalkBack Accessibility", "Screen reader for visually impaired", "Google", SafetyLevel.Optional);
            Register("com.google.android.tts", "Google Speech Services (TTS)", "Text-to-speech engine", "Google", SafetyLevel.Optional);
            Register("com.google.android.googlequicksearchbox", "Google App / Assistant", "Google Search app and assistant widget", "Google", SafetyLevel.Optional);
            Register("com.android.chrome", "Google Chrome", "Google Chrome web browser", "Google", SafetyLevel.Optional);

            // --- Xiaomi / MIUI / HyperOS Bloatware ---
            Register("com.miui.analytics", "MIUI Analytics", "Xiaomi telemetry and usage tracker", "Xiaomi", SafetyLevel.Safe);
            Register("com.miui.msa.global", "MIUI System Ads (MSA)", "Background ad delivery framework", "Xiaomi", SafetyLevel.Safe);
            Register("com.miui.bugreport", "MIUI Bug Report", "Bug reporting background service", "Xiaomi", SafetyLevel.Safe);
            Register("com.miui.cleanmaster", "MIUI Cleaner (CleanMaster)", "Built-in cheetah mobile cleaner", "Xiaomi", SafetyLevel.Safe);
            Register("com.miui.yellowpage", "MIUI Yellow Pages", "Caller ID and local services", "Xiaomi", SafetyLevel.Safe);
            Register("com.xiaomi.midrop", "ShareMe / Mi Drop", "Xiaomi file transfer tool", "Xiaomi", SafetyLevel.Optional);
            Register("com.miui.player", "Mi Music", "Stock music app with ads", "Xiaomi", SafetyLevel.Optional);
            Register("com.miui.videoplayer", "Mi Video", "Stock video player with online content", "Xiaomi", SafetyLevel.Optional);
            Register("com.mipay.wallet.id", "Mi Pay / Wallet", "Xiaomi payment service", "Xiaomi", SafetyLevel.Optional);

            // --- Critical System Core (Caution) ---
            Register("android", "Android System", "Core Android OS framework", "System", SafetyLevel.Caution);
            Register("com.android.systemui", "System UI", "Navigation bar, status bar, and notifications", "System", SafetyLevel.Caution);
            Register("com.android.settings", "Settings", "Device settings menu", "System", SafetyLevel.Caution);
            Register("com.android.phone", "Phone / Dialer Framework", "Cellular telephony calling service", "System", SafetyLevel.Caution);
            Register("com.android.server.telecom", "Telecom Server", "Manages phone audio routing and calls", "System", SafetyLevel.Caution);
            Register("com.android.providers.telephony", "Telephony Provider", "Stores SMS, MMS, and APN configuration", "System", SafetyLevel.Caution);
            Register("com.google.android.gms", "Google Play Services", "Essential backend framework for Google services", "Google", SafetyLevel.Caution);
            Register("com.android.vending", "Google Play Store", "Official app market", "Google", SafetyLevel.Caution);
            Register("com.sec.android.app.launcher", "One UI Home Launcher", "Default home screen launcher", "Samsung", SafetyLevel.Caution);
        }

        private void LoadCustomDatabase()
        {
            try
            {
                var customPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_packages.json");
                if (File.Exists(customPath))
                {
                    var json = File.ReadAllText(customPath);
                    var customList = JsonSerializer.Deserialize<List<AppPackage>>(json);
                    if (customList != null)
                    {
                        foreach (var pkg in customList)
                        {
                            _packageDb[pkg.PackageName] = pkg;
                        }
                    }
                }
            }
            catch
            {
                // Fallback silently if custom JSON parsing fails
            }
        }
    }
}
