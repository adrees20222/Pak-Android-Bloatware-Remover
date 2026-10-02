using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AndroidDebloater.Models;
using AndroidDebloater.Services;
using Microsoft.Win32;

namespace AndroidDebloater
{
    public partial class MainWindow : Window
    {
        private readonly AdbService _adbService = new();
        private readonly DatabaseService _dbService = new();
        private readonly AppStoreService _storeService = new();

        private List<AppPackage> _allPackages = new();
        private readonly ObservableCollection<AppPackage> _displayedInstalled = new();
        private readonly ObservableCollection<AppPackage> _displayedUninstalled = new();
        private readonly ObservableCollection<PlayStoreSearchResult> _displayedSearchResults = new();
        private bool _isReady = false;

        public MainWindow()
        {
            InitializeComponent();

            gridInstalled.ItemsSource = _displayedInstalled;
            gridUninstalled.ItemsSource = _displayedUninstalled;
            gridStoreApps.ItemsSource = _displayedSearchResults;

            _adbService.LogOutput += AppendLog;

            Loaded += async (s, e) =>
            {
                _isReady = true;
                await RefreshAllAsync();
                await LoadInitialSuggestionsAsync();
            };
        }

        private async Task LoadInitialSuggestionsAsync()
        {
            try
            {
                var popular = await _storeService.SearchPlayStoreAsync("WhatsApp");
                _displayedSearchResults.Clear();
                foreach (var item in popular)
                {
                    _displayedSearchResults.Add(item);
                }
            }
            catch
            {
                // Ignore silent network warmup errors
            }
        }

        private void AppendLog(string message)
        {
            if (txtLogConsole == null) return;
            Dispatcher.Invoke(() =>
            {
                txtLogConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtLogConsole.ScrollToEnd();
            });
        }

        private async Task RefreshAllAsync()
        {
            SetBusy(true, "Scanning for connected devices...");

            if (!_adbService.IsAdbAvailable)
            {
                if (txtDeviceInfo != null)
                {
                    txtDeviceInfo.Text = "ADB executable not found. Please place platform-tools in folder or install Android SDK.";
                }
                if (statusIndicator != null)
                {
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                }
                SetBusy(false, "ADB missing");
                return;
            }

            var device = await _adbService.GetDeviceInfoAsync();
            if (device.IsConnected)
            {
                if (txtDeviceInfo != null)
                    txtDeviceInfo.Text = $"{device.Manufacturer} {device.Model} | Android {device.AndroidVersion} (SN: {device.Serial})";
                if (txtBatteryInfo != null)
                    txtBatteryInfo.Text = $"🔋 {device.BatteryLevel}";
                if (statusIndicator != null)
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                SetBusy(true, "Retrieving installed packages from device...");
                _allPackages = await _adbService.GetPackagesAsync(_dbService);
                ApplyFilter();
                SetBusy(false, $"Loaded {_allPackages.Count} total packages.");
            }
            else
            {
                if (txtDeviceInfo != null)
                    txtDeviceInfo.Text = "No Android device detected. Connect via USB with USB Debugging enabled.";
                if (txtBatteryInfo != null)
                    txtBatteryInfo.Text = "";
                if (statusIndicator != null)
                    statusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                _allPackages.Clear();
                _displayedInstalled.Clear();
                _displayedUninstalled.Clear();
                SetBusy(false, "No device connected.");
            }
        }

        private void ApplyFilter()
        {
            if (!_isReady || txtSearch == null || cmbSafetyFilter == null || cmbBrandFilter == null || txtCountsSummary == null || _allPackages == null)
            {
                return;
            }

            var search = txtSearch.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var safetyIndex = cmbSafetyFilter.SelectedIndex;
            var brandSelection = (cmbBrandFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Brands";

            var installed = _allPackages.Where(p => p.IsInstalled).ToList();
            var uninstalled = _allPackages.Where(p => !p.IsInstalled).ToList();

            // Filter Installed
            var filteredInstalled = installed.Where(p =>
            {
                // Search filter
                var matchSearch = string.IsNullOrEmpty(search) ||
                                  p.FriendlyName.ToLowerInvariant().Contains(search) ||
                                  p.PackageName.ToLowerInvariant().Contains(search) ||
                                  p.Description.ToLowerInvariant().Contains(search);

                // Safety filter
                var matchSafety = safetyIndex switch
                {
                    1 => p.Safety == SafetyLevel.Safe,
                    2 => p.Safety == SafetyLevel.Optional,
                    3 => p.Safety == SafetyLevel.Caution,
                    _ => true
                };

                // Brand filter
                var matchBrand = brandSelection == "All Brands" || p.Brand.Equals(brandSelection, StringComparison.OrdinalIgnoreCase);

                return matchSearch && matchSafety && matchBrand;
            }).OrderBy(p => p.FriendlyName).ToList();

            _displayedInstalled.Clear();
            foreach (var item in filteredInstalled)
            {
                _displayedInstalled.Add(item);
            }

            // Filter Uninstalled
            _displayedUninstalled.Clear();
            foreach (var item in uninstalled.OrderBy(p => p.FriendlyName))
            {
                _displayedUninstalled.Add(item);
            }

            var safeCount = _allPackages.Count(p => p.IsInstalled && p.Safety == SafetyLevel.Safe);
            txtCountsSummary.Text = $"Installed: {installed.Count} | Safe Bloat: {safeCount} | Uninstalled: {uninstalled.Count}";
        }

        private void SetBusy(bool isBusy, string statusMessage)
        {
            if (progressBar != null)
                progressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            if (txtStatus != null)
                txtStatus.Text = statusMessage;
            if (btnRefreshDevice != null)
                btnRefreshDevice.IsEnabled = !isBusy;
            if (btnUninstallSelected != null)
                btnUninstallSelected.IsEnabled = !isBusy;
            if (btnRestoreSelected != null)
                btnRestoreSelected.IsEnabled = !isBusy;
            if (btnSearchPlayStore != null)
                btnSearchPlayStore.IsEnabled = !isBusy;
        }

        // --- Filter Event Handlers ---
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void CmbSafetyFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void CmbBrandFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        // --- Selection Buttons ---
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var app in _displayedInstalled) app.IsSelected = true;
        }

        private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var app in _displayedInstalled) app.IsSelected = false;
        }

        private void BtnSelectSafeBloatware_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (var app in _displayedInstalled)
            {
                if (app.Safety == SafetyLevel.Safe)
                {
                    app.IsSelected = true;
                    count++;
                }
                else
                {
                    app.IsSelected = false;
                }
            }

            MessageBox.Show($"Selected {count} safe bloatware packages ready for uninstall.", "Safe Bloat Selected", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSelectAllUninstalled_Click(object sender, RoutedEventArgs e)
        {
            foreach (var app in _displayedUninstalled) app.IsSelected = true;
        }

        // --- Uninstall Action ---
        private async void BtnUninstallSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _displayedInstalled.Where(p => p.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Please check at least one app to uninstall.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var warningPrompt = $"Are you sure you want to uninstall {selected.Count} selected app(s)?\n\n";
            if (selected.Any(p => p.Safety == SafetyLevel.Caution))
            {
                warningPrompt += "⚠️ WARNING: You have selected one or more System Core packages. Uninstalling core apps may cause instability!\n\n";
            }
            warningPrompt += "Note: You can restore uninstalled apps anytime from the 'Restore Apps' tab.";

            var confirm = MessageBox.Show(warningPrompt, "Confirm Uninstall", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            SetBusy(true, "Uninstalling selected packages...");
            bool keepData = chkKeepData.IsChecked == true;
            int success = 0;

            foreach (var pkg in selected)
            {
                if (txtStatus != null)
                    txtStatus.Text = $"Uninstalling {pkg.FriendlyName}...";
                var ok = await _adbService.UninstallPackageAsync(pkg.PackageName, keepData);
                if (ok)
                {
                    success++;
                    pkg.IsInstalled = false;
                    pkg.IsSelected = false;
                }
            }

            SetBusy(false, $"Uninstalled {success} of {selected.Count} apps.");
            MessageBox.Show($"Successfully uninstalled {success} of {selected.Count} apps.", "Uninstall Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAllAsync();
        }

        // --- Restore Action ---
        private async void BtnRestoreSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _displayedUninstalled.Where(p => p.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Please select at least one app to restore.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true, "Restoring selected packages...");
            int success = 0;

            foreach (var pkg in selected)
            {
                if (txtStatus != null)
                    txtStatus.Text = $"Restoring {pkg.FriendlyName}...";
                var ok = await _adbService.RestorePackageAsync(pkg.PackageName);
                if (ok)
                {
                    success++;
                    pkg.IsInstalled = true;
                    pkg.IsSelected = false;
                }
            }

            SetBusy(false, $"Restored {success} of {selected.Count} apps.");
            MessageBox.Show($"Successfully restored {success} of {selected.Count} apps.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAllAsync();
        }

        // --- App Search & Direct Install ---
        private async void BtnSearchPlayStore_Click(object sender, RoutedEventArgs e) => await ExecuteAppSearch();

        private async void TxtPlayStoreQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExecuteAppSearch();
            }
        }

        private async Task ExecuteAppSearch()
        {
            var query = txtPlayStoreQuery.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            SetBusy(true, $"Searching for '{query}'...");
            txtSearchResultsHeader.Text = $"Searching for '{query}'...";

            try
            {
                var results = await _storeService.SearchPlayStoreAsync(query);
                _displayedSearchResults.Clear();
                foreach (var item in results)
                {
                    _displayedSearchResults.Add(item);
                }

                txtSearchResultsHeader.Text = $"Available Apps for '{query}' ({results.Count} found):";
                SetBusy(false, $"Found {results.Count} apps ready for 1-click install.");
            }
            catch (Exception ex)
            {
                SetBusy(false, "Search failed.");
                MessageBox.Show($"Error searching apps: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewInPlayStore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string pkg && !string.IsNullOrEmpty(pkg))
            {
                OpenBrowser($"https://play.google.com/store/apps/details?id={pkg}");
            }
        }

        private async void BtnInstallSearchResult_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlayStoreSearchResult app)
            {
                var confirm = MessageBox.Show($"Download and install '{app.Title}' ({app.PackageName})\nSize: {app.FileSizeFormatted}\n\nInstall directly onto your connected phone?", "Install App", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    SetBusy(true, $"Downloading {app.Title} ({app.FileSizeFormatted})...");
                    var localApk = await _storeService.DownloadApkForPackageAsync(app.PackageName, app.Title, app.DirectDownloadUrl, p =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (txtStatus != null) txtStatus.Text = $"Downloading {app.Title} ({p}%)...";
                        });
                    });

                    SetBusy(true, $"Installing {app.Title} to phone via ADB...");
                    var ok = await _adbService.InstallApkAsync(localApk);
                    SetBusy(false, ok ? $"Installed {app.Title} successfully!" : "Installation failed.");

                    if (ok)
                    {
                        MessageBox.Show($"🎉 '{app.Title}' ({app.PackageName}) was successfully installed on your phone!", "Installation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        await RefreshAllAsync();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to install '{app.Title}'. Check the Activity Log tab for details.", "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    SetBusy(false, "Download failed.");
                    MessageBox.Show($"Download/Install error: {ex.Message}", "Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- Local APK Install Handlers ---
        private async void BtnBrowseApk_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Android Package (*.apk)|*.apk|All Files (*.*)|*.*",
                Title = "Select APK File to Install"
            };

            if (ofd.ShowDialog() == true)
            {
                await InstallLocalApkFile(ofd.FileName);
            }
        }

        private void BorderApkDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private async void BorderApkDrop_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (file.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        await InstallLocalApkFile(file);
                    }
                }
            }
        }

        private async Task InstallLocalApkFile(string apkPath)
        {
            var fileName = Path.GetFileName(apkPath);
            SetBusy(true, $"Installing {fileName}...");
            var ok = await _adbService.InstallApkAsync(apkPath);
            SetBusy(false, ok ? $"Installed {fileName}!" : "Installation failed.");

            if (ok)
            {
                MessageBox.Show($"Successfully installed '{fileName}' on your connected Android device!", "Install Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshAllAsync();
            }
            else
            {
                MessageBox.Show($"Failed to install '{fileName}'. Check the Activity Log tab for details.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Export Package List ---
        private void BtnExportList_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON Profile (*.json)|*.json|Text File (*.txt)|*.txt",
                FileName = $"android_packages_{DateTime.Now:yyyyMMdd}.json"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    if (sfd.FileName.EndsWith(".json"))
                    {
                        var json = JsonSerializer.Serialize(_allPackages, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json);
                    }
                    else
                    {
                        var lines = _allPackages.Select(p => $"{p.FriendlyName} | {p.PackageName} | {p.Safety} | Installed: {p.IsInstalled}");
                        File.WriteAllLines(sfd.FileName, lines);
                    }
                    MessageBox.Show("Package list exported successfully!", "Exported", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- Wireless ADB & Power Tools ---
        private async void BtnConnectWireless_Click(object sender, RoutedEventArgs e)
        {
            var ip = txtWirelessIp.Text.Trim();
            if (!int.TryParse(txtWirelessPort.Text.Trim(), out int port)) port = 5555;

            SetBusy(true, $"Connecting to {ip}:{port}...");
            var res = await _adbService.ConnectWirelessAsync(ip, port);
            SetBusy(false, res);
            MessageBox.Show(res, "Wireless ADB Result", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAllAsync();
        }

        private async void BtnDisconnectWireless_Click(object sender, RoutedEventArgs e)
        {
            var res = await _adbService.DisconnectWirelessAsync();
            MessageBox.Show(res, "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
            await RefreshAllAsync();
        }

        private async void BtnRebootNormal_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reboot device now?", "Reboot", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await _adbService.RebootAsync("normal");
        }

        private async void BtnRebootRecovery_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reboot into Recovery Mode?", "Recovery", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await _adbService.RebootAsync("recovery");
        }

        private async void BtnRebootBootloader_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Reboot into Fastboot / Bootloader Mode?", "Bootloader", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                await _adbService.RebootAsync("bootloader");
        }

        // --- Log Handlers ---
        private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtLogConsole.Text);
            MessageBox.Show("Activity log copied to clipboard.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e) => txtLogConsole?.Clear();

        private async void BtnRefreshDevice_Click(object sender, RoutedEventArgs e) => await RefreshAllAsync();

        // --- Developer Credit Links Handlers ---
        private void LinkPortfolio_Click(object sender, MouseButtonEventArgs e) => OpenBrowser("https://adrees2022.blogspot.com/");
        private void LinkSupport_Click(object sender, MouseButtonEventArgs e) => OpenBrowser("https://my-extension.blogspot.com/p/support.html");
        private void LinkDonate_Click(object sender, MouseButtonEventArgs e) => OpenBrowser("https://my-extension.blogspot.com/p/donate.html");
        private void LinkTerms_Click(object sender, MouseButtonEventArgs e) => OpenBrowser("https://my-extension.blogspot.com/p/terms.html");
        private void LinkPrivacy_Click(object sender, MouseButtonEventArgs e) => OpenBrowser("https://my-extension.blogspot.com/p/privacy-policy_15.html");

        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}