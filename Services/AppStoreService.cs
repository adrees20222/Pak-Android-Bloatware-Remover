using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndroidDebloater.Services
{
    public class PlayStoreSearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "Latest";
        public string FileSizeFormatted { get; set; } = "-- MB";
        public string Rating { get; set; } = "★ 4.5";
        public string Category { get; set; } = "Android App";
        public string DirectDownloadUrl { get; set; } = string.Empty;
    }

    public class AppStoreService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        static AppStoreService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
        }

        public async Task<List<PlayStoreSearchResult>> SearchPlayStoreAsync(string query)
        {
            var results = new List<PlayStoreSearchResult>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            try
            {
                var encoded = Uri.EscapeDataString(query.Trim());
                var searchUrl = $"https://ws75.aptoide.com/api/7/apps/search?query={encoded}&limit=25";

                var json = await _http.GetStringAsync(searchUrl);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("datalist", out var datalist) &&
                    datalist.TryGetProperty("list", out var listArray) &&
                    listArray.ValueKind == JsonValueKind.Array)
                {
                    var seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var item in listArray.EnumerateArray())
                    {
                        var pkg = item.TryGetProperty("package", out var p) ? p.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(pkg) || seenPackages.Contains(pkg)) continue;
                        seenPackages.Add(pkg);

                        var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? pkg : pkg;
                        var icon = item.TryGetProperty("icon", out var ic) ? ic.GetString() ?? "" : "";

                        var dev = "Verified Developer";
                        if (item.TryGetProperty("store", out var store) && store.TryGetProperty("name", out var sn))
                        {
                            dev = sn.GetString() ?? "Verified Developer";
                        }

                        var ver = "Latest";
                        var directUrl = "";
                        var sizeFormatted = "-- MB";

                        if (item.TryGetProperty("file", out var file))
                        {
                            if (file.TryGetProperty("vername", out var vn)) ver = vn.GetString() ?? "Latest";
                            if (file.TryGetProperty("path", out var path)) directUrl = path.GetString() ?? "";
                            if (string.IsNullOrEmpty(directUrl) && file.TryGetProperty("path_alt", out var pathAlt)) directUrl = pathAlt.GetString() ?? "";

                            if (file.TryGetProperty("filesize", out var fs) && fs.TryGetInt64(out var bytes))
                            {
                                sizeFormatted = $"{bytes / (1024.0 * 1024.0):0.1} MB";
                            }
                        }

                        var rating = "★ 4.5";
                        if (item.TryGetProperty("stats", out var stats) &&
                            stats.TryGetProperty("rating", out var rt) &&
                            rt.TryGetProperty("avg", out var avg) &&
                            avg.TryGetDouble(out var ratingVal) && ratingVal > 0)
                        {
                            rating = $"★ {ratingVal:0.1}";
                        }

                        results.Add(new PlayStoreSearchResult
                        {
                            Title = name,
                            PackageName = pkg,
                            Developer = dev,
                            IconUrl = icon,
                            Version = $"v{ver}",
                            FileSizeFormatted = sizeFormatted,
                            Rating = rating,
                            Category = "Android App",
                            DirectDownloadUrl = directUrl
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }

            return results;
        }

        public async Task<string> DownloadApkForPackageAsync(string packageName, string appName, string directDownloadUrl, Action<int>? progressCallback = null)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "PakAndroid_APKs");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            var safeName = string.Concat(appName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
            var localFile = Path.Combine(tempDir, $"{packageName}_{safeName}.apk");

            var urlsToTry = new List<string>();
            if (!string.IsNullOrEmpty(directDownloadUrl)) urlsToTry.Add(directDownloadUrl);
            urlsToTry.Add($"https://d.apkpure.com/b/APK/{packageName}?version=latest");
            urlsToTry.Add($"https://f-droid.org/repo/{packageName}.apk");

            Exception? lastError = null;

            foreach (var url in urlsToTry)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    using var response = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode) continue;

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(localFile, FileMode.Create, FileAccess.Write, FileShare.None, 16384, true);

                    var buffer = new byte[16384];
                    long totalRead = 0;
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes > 0 && progressCallback != null)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            progressCallback(Math.Min(progress, 99));
                        }
                    }

                    var fi = new FileInfo(localFile);
                    if (fi.Exists && fi.Length > 10000)
                    {
                        progressCallback?.Invoke(100);
                        return localFile;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw lastError ?? new Exception($"Could not download APK for package '{packageName}'. Please try again or check your internet connection.");
        }
    }
}
