using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AndroidDebloater.Models;

namespace AndroidDebloater.Services
{
    public class AdbService
    {
        private string? _adbPath;

        public event Action<string>? LogOutput;

        public AdbService()
        {
            _adbPath = LocateAdb();
        }

        public string? AdbPath => _adbPath;

        public bool IsAdbAvailable => !string.IsNullOrEmpty(_adbPath) && File.Exists(_adbPath);

        private string? LocateAdb()
        {
            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new List<string>
            {
                Path.Combine(localApp, @"Android\Sdk\platform-tools\adb.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "platform-tools", "adb.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe"),
                @"C:\platform-tools\adb.exe",
                @"C:\Android\platform-tools\adb.exe"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Check PATH environment variable
            var envPath = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                foreach (var dir in envPath.Split(Path.PathSeparator))
                {
                    var full = Path.Combine(dir.Trim(), "adb.exe");
                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
            }

            return null;
        }

        public async Task<string> RunCommandAsync(string arguments)
        {
            if (!IsAdbAvailable)
            {
                throw new FileNotFoundException("adb.exe not found on this machine.");
            }

            LogOutput?.Invoke($"[ADB] > adb {arguments}");

            var psi = new ProcessStartInfo
            {
                FileName = _adbPath!,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (!string.IsNullOrWhiteSpace(error) && !error.Contains("daemon started"))
            {
                LogOutput?.Invoke($"[ERROR] {error.Trim()}");
            }

            return output;
        }

        public async Task<DeviceInfo> GetDeviceInfoAsync()
        {
            var info = new DeviceInfo();

            try
            {
                var devicesOutput = await RunCommandAsync("devices");
                var lines = devicesOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string? activeSerial = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("List of devices") || line.Contains("daemon")) continue;
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[1].Trim() == "device")
                    {
                        activeSerial = parts[0].Trim();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(activeSerial))
                {
                    info.IsConnected = false;
                    return info;
                }

                info.Serial = activeSerial;
                info.IsConnected = true;

                // Query device properties
                info.Manufacturer = (await RunCommandAsync("shell getprop ro.product.manufacturer")).Trim();
                info.Model = (await RunCommandAsync("shell getprop ro.product.model")).Trim();
                info.AndroidVersion = (await RunCommandAsync("shell getprop ro.build.version.release")).Trim();

                // Battery level
                var batteryRaw = await RunCommandAsync("shell dumpsys battery");
                var batteryMatch = Regex.Match(batteryRaw, @"level:\s*(\d+)");
                if (batteryMatch.Success)
                {
                    info.BatteryLevel = batteryMatch.Groups[1].Value + "%";
                }
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke($"[EXCEPTION] Failed to get device info: {ex.Message}");
                info.IsConnected = false;
            }

            return info;
        }

        public async Task<List<AppPackage>> GetPackagesAsync(DatabaseService db)
        {
            var list = new List<AppPackage>();
            var installedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 1. Get all currently installed packages for User 0
                var raw = await RunCommandAsync("shell pm list packages -f");
                var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^package:(.*?)=([a-zA-Z0-9_\.]+)$");
                    if (match.Success)
                    {
                        var apkPath = match.Groups[1].Value.Trim();
                        var pkgName = match.Groups[2].Value.Trim();
                        installedPackages.Add(pkgName);

                        var pkg = db.ResolvePackage(pkgName, apkPath);
                        pkg.IsInstalled = true;
                        list.Add(pkg);
                    }
                }

                // 2. Get uninstalled / disabled packages (available for restore)
                var uninstalledRaw = await RunCommandAsync("shell pm list packages -u -f");
                var uninstalledLines = uninstalledRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in uninstalledLines)
                {
                    var match = Regex.Match(line, @"^package:(.*?)=([a-zA-Z0-9_\.]+)$");
                    if (match.Success)
                    {
                        var apkPath = match.Groups[1].Value.Trim();
                        var pkgName = match.Groups[2].Value.Trim();

                        if (!installedPackages.Contains(pkgName))
                        {
                            var pkg = db.ResolvePackage(pkgName, apkPath);
                            pkg.IsInstalled = false;
                            list.Add(pkg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke($"[EXCEPTION] Failed to list packages: {ex.Message}");
            }

            return list;
        }

        public async Task<bool> UninstallPackageAsync(string packageName, bool keepData = true)
        {
            try
            {
                var flags = keepData ? "-k --user 0" : "--user 0";
                var output = await RunCommandAsync($"shell pm uninstall {flags} {packageName}");
                LogOutput?.Invoke($"[UNINSTALL] {packageName} => {output.Trim()}");
                return output.Contains("Success", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke($"[ERROR] Uninstall failed for {packageName}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestorePackageAsync(string packageName)
        {
            try
            {
                var output = await RunCommandAsync($"shell cmd package install-existing {packageName}");
                LogOutput?.Invoke($"[RESTORE] {packageName} => {output.Trim()}");
                return output.Contains("installed for user", StringComparison.OrdinalIgnoreCase) ||
                       output.Contains("Success", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke($"[ERROR] Restore failed for {packageName}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ConnectWirelessAsync(string ipAddress, int port = 5555)
        {
            return (await RunCommandAsync($"connect {ipAddress}:{port}")).Trim();
        }

        public async Task<string> DisconnectWirelessAsync()
        {
            return (await RunCommandAsync("disconnect")).Trim();
        }

        public async Task<string> RebootAsync(string mode = "normal")
        {
            return mode switch
            {
                "recovery" => await RunCommandAsync("reboot recovery"),
                "bootloader" => await RunCommandAsync("reboot bootloader"),
                _ => await RunCommandAsync("reboot")
            };
        }

        public async Task<bool> InstallApkAsync(string apkPath)
        {
            try
            {
                if (!File.Exists(apkPath))
                {
                    LogOutput?.Invoke($"[ERROR] File not found: {apkPath}");
                    return false;
                }

                LogOutput?.Invoke($"[INSTALL] Installing APK: {apkPath}...");
                var output = await RunCommandAsync($"install -r -d \"{apkPath}\"");
                LogOutput?.Invoke($"[INSTALL RESULT] {output.Trim()}");
                return output.Contains("Success", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke($"[ERROR] APK installation failed: {ex.Message}");
                return false;
            }
        }
    }
}
