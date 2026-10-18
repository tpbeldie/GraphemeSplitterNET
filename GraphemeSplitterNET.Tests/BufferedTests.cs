using GraphemeSplitterNET;
using System;
using System.IO;
using Xunit;

namespace GraphemeSplitterNET.Tests
{
  // Mutates static configuration, so it must not run in parallel with itself.
  [Collection("Buffered")]
  public class BufferedTests : IDisposable
  {

    private readonly string m_oldDirectory = GraphemeSplitterBuffered.CacheDirectory;
    private readonly string m_tempDirectory = Path.Combine(Path.GetTempPath(), "gsnet-tests-" + Guid.NewGuid().ToString("N"));

    public BufferedTests()
    {
      GraphemeSplitterBuffered.CacheDirectory = m_tempDirectory;
      GraphemeSplitterBuffered.ClearCache();
    }

    public void Dispose()
    {
      GraphemeSplitterBuffered.ClearCache();
      GraphemeSplitterBuffered.CacheDirectory = m_oldDirectory;
      try { Directory.Delete(m_tempDirectory, true); } catch { }
    }

    [Fact]
    public void LoadsPinnedVersionFromCache_WithoutNetwork()
    {
      // Pretend Unicode 17 was downloaded before: pinned versions never expire.
      Directory.CreateDirectory(m_tempDirectory);
      using (var stream = File.Create(Path.Combine(m_tempDirectory, "grapheme-Unicode_17.cache"))) {
        GraphemeTable.Embedded.Write(stream);
      }

      var splitter = new GraphemeSplitterBuffered(UnicodeVersion.Unicode_17);
      Assert.Equal(GraphemeTable.Embedded.UnicodeVersion, splitter.Table.UnicodeVersion);
      Assert.Single(splitter.Split("\U0001F468\u200D\U0001F469"));
      Assert.Same(splitter.Table, new GraphemeSplitterBuffered(UnicodeVersion.Unicode_17).Table);
    }

    [Fact]
    public void CorruptedCache_IsIgnored()
    {
      Directory.CreateDirectory(m_tempDirectory);
      File.WriteAllText(Path.Combine(m_tempDirectory, "grapheme-Unicode_16.cache"), "garbage");

      // Either downloads 16.0.0 or falls back to the embedded table, but never ends up empty.
      var splitter = new GraphemeSplitterBuffered(UnicodeVersion.Unicode_16);
      Assert.Equal(Unicode.Extended_Pictographic, splitter.GetGraphemeBreakProperty(0x1F600));
      Assert.Equal(2, splitter.GetLength("e\u0301x"));
    }
  }
}
