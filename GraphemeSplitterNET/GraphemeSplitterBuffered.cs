using System;
using System.Collections.Generic;
using System.IO;

namespace GraphemeSplitterNET
{
  public class GraphemeSplitterBuffered : Unicode
  {

    private static int[] m_bmp_cache;

    private static Dictionary<int, int> m_supplementary_cache;

    public static readonly UnicodeVersion UnicodeVersion = UnicodeVersion.Unicode_Latest;

    private static readonly Lazy<bool> s_initializer = new Lazy<bool>(Initialize, isThreadSafe: true);

    public GraphemeSplitterBuffered()
    {
      EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
      _ = s_initializer.Value;
    }

    private static bool TryLoadCache(string cachePath)
    {
      if (File.Exists(cachePath)) {
        try {
          using (var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
          using (var reader = new BinaryReader(stream)) {
            int supplementaryCount = reader.ReadInt32();
            m_supplementary_cache = new Dictionary<int, int>(supplementaryCount);
            for (int i = 0; i < supplementaryCount; i++) {
              m_supplementary_cache.Add(reader.ReadInt32(), reader.ReadInt32());
            }
            byte[] bmpBytes = reader.ReadBytes(65536 * 4);
            m_bmp_cache = new int[65536];
            Buffer.BlockCopy(bmpBytes, 0, m_bmp_cache, 0, bmpBytes.Length);

            return true;
          }
        }
        catch (Exception ex) {
          Console.WriteLine($"Could not read grapheme cache: {ex.Message}. Rebuilding...");
        }
      }
      return false;
    }

    private static void TryCreateCache(string cachePath)
    {
      try {
        using (var stream = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream)) {
          writer.Write(m_supplementary_cache.Count);
          foreach (var kvp in m_supplementary_cache) {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
          }

          byte[] bmpBytes = new byte[m_bmp_cache.Length * 4];
          Buffer.BlockCopy(m_bmp_cache, 0, bmpBytes, 0, bmpBytes.Length);
          writer.Write(bmpBytes);

          Console.WriteLine($"Cache created at: {cachePath}");
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Warning: Could not save grapheme cache: {ex.Message}");
      }
    }

    private static bool Initialize()
    {
      string cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grapheme.cache");
      if (TryLoadCache(cachePath)) {
        return true; // Initialized.
      }
      TryBuildCaches();
      TryCreateCache(cachePath);
      return true;
    }

    private static void TryBuildCaches()
    {
      try {
        Console.WriteLine("Parsing Unicode rules for the first time. This may take a moment...");
        var parser = new RuleParser();
        var allCodePoints = parser.ParseAsync(UnicodeVersion).GetAwaiter().GetResult();

        m_bmp_cache = new int[65536];
        m_supplementary_cache = new Dictionary<int, int>();

        foreach (var kvp in allCodePoints) {
          if (kvp.Key < 65536)
            m_bmp_cache[kvp.Key] = kvp.Value;
          else
            m_supplementary_cache[kvp.Key] = kvp.Value;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error building grapheme caches: {ex.Message}");
        m_bmp_cache = new int[65536];
        m_supplementary_cache = new Dictionary<int, int>();
      }
    }

    public override int GetGraphemeBreakProperty(int codePoint)
    {
      if (codePoint < 65536) {
        return m_bmp_cache[codePoint];
      }
      if (m_supplementary_cache.TryGetValue(codePoint, out int value)) {
        return value;
      }
      return Other;
    }
  }
}
