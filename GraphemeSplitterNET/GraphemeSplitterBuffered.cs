using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GraphemeSplitterNET
{

  /// <summary>
  /// Same splitter as GraphemeSplitter, but the Unicode data is downloaded from unicode.org
  /// (latest version by default) and cached on disk, so the rules can follow new Unicode
  /// releases without recompiling.
  /// 
  /// - First use downloads ~1.3 MB of UCD text files once, builds the compact table and
  ///   saves it (~tens of KB) to CacheDirectory. Next runs only read the cache.
  /// - If the download fails (offline, proxy...) it falls back to the embedded table,
  ///   so it never ends up with an empty table.
  /// - Tables are shared between instances of the same version.
  /// </summary>
  public class GraphemeSplitterBuffered : Unicode
  {

    private static readonly Dictionary<UnicodeVersion, Lazy<GraphemeTable>> s_tables = new Dictionary<UnicodeVersion, Lazy<GraphemeTable>>();

    private static readonly object s_lock = new object();

    /// <summary>
    /// Folder where downloaded tables are cached. Set to null to disable the cache.
    /// Defaults to %LOCALAPPDATA%\GraphemeSplitterNET.
    /// </summary>
    public static string CacheDirectory { get; set; } =
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GraphemeSplitterNET");

    /// <summary>
    /// How long a cached Unicode_Latest table is trusted before checking unicode.org again.
    /// Pinned versions never expire, their data never changes.
    /// </summary>
    public static TimeSpan LatestCacheLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// The exception of the last failed download, if the embedded table was used as fallback.
    /// </summary>
    public static Exception LastError { get; private set; }

    public UnicodeVersion RequestedVersion { get; }

    public GraphemeSplitterBuffered() : this(UnicodeVersion.Unicode_Latest)
    {
    }

    public GraphemeSplitterBuffered(UnicodeVersion version) : base(GetTable(version))
    {
      RequestedVersion = version;
    }

    private GraphemeSplitterBuffered(UnicodeVersion version, GraphemeTable table) : base(table)
    {
      RequestedVersion = version;
    }

    /// <summary>
    /// Non-blocking way to create the splitter, recommended from UI threads.
    /// </summary>
    public static Task<GraphemeSplitterBuffered> CreateAsync(UnicodeVersion version = UnicodeVersion.Unicode_Latest)
    {
#if NET40
      return Task.Factory.StartNew(() => new GraphemeSplitterBuffered(version, GetTable(version)));
#else
      return Task.Run(() => new GraphemeSplitterBuffered(version, GetTable(version)));
#endif
    }

    /// <summary>
    /// Forgets loaded tables and deletes the cache files, forcing a new download next time.
    /// </summary>
    public static void ClearCache()
    {
      lock (s_lock) {
        s_tables.Clear();
      }
      var dir = CacheDirectory;
      if (dir == null || !Directory.Exists(dir)) return;
      foreach (var file in Directory.GetFiles(dir, "grapheme-*.cache")) {
        try { File.Delete(file); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
      }
    }

    private static GraphemeTable GetTable(UnicodeVersion version)
    {
      Lazy<GraphemeTable> lazy;
      lock (s_lock) {
        if (!s_tables.TryGetValue(version, out lazy)) {
          lazy = new Lazy<GraphemeTable>(() => LoadTable(version), isThreadSafe: true);
          s_tables.Add(version, lazy);
        }
      }
      return lazy.Value;
    }

    private static GraphemeTable LoadTable(UnicodeVersion version)
    {
      string cachePath = CacheDirectory == null ? null : Path.Combine(CacheDirectory, "grapheme-" + version + ".cache");

      GraphemeTable cached = TryLoadCache(cachePath);
      bool expired = cached != null
        && version == UnicodeVersion.Unicode_Latest
        && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) > LatestCacheLifetime;

      if (cached != null && !expired) {
        return cached;
      }

      try {
#if NET40
        var table = Task.Factory.StartNew(() => new RuleParser().DownloadAsync(version).Result).Result;
#else
        // Task.Run avoids deadlocks when called from a thread with a SynchronizationContext (WinForms/WPF).
        var table = Task.Run(() => new RuleParser().DownloadAsync(version)).GetAwaiter().GetResult();
#endif
        TryCreateCache(cachePath, table);
        LastError = null;
        return table;
      }
      catch (Exception ex) {
        LastError = ex;
        // Prefer an outdated download over the embedded data, it is at least as new.
        return cached ?? GraphemeTable.Embedded;
      }
    }

    private static GraphemeTable TryLoadCache(string cachePath)
    {
      if (cachePath == null || !File.Exists(cachePath)) return null;
      try {
        using (var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          return GraphemeTable.Read(stream);
        }
      }
      catch (Exception) {
        return null; // Corrupted or old format, rebuild it.
      }
    }

    private static void TryCreateCache(string cachePath, GraphemeTable table)
    {
      if (cachePath == null) return;
      try {
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
        // Write to a temp file first so a crash never leaves a half written cache.
        string tempPath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write)) {
          table.Write(stream);
        }
        if (File.Exists(cachePath)) File.Delete(cachePath);
        File.Move(tempPath, cachePath);
      }
      catch (Exception) {
        // The cache is only an optimization.
      }
    }
  }
}
