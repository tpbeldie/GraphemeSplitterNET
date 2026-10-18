namespace GraphemeSplitterNET
{
  /// <summary>
  /// 
  /// Grapheme cluster splitter compliant with Unicode Standard Annex #29,
  /// using the Unicode data compiled into the assembly.
  /// 
  /// - No network, no files, no warm-up: the embedded table is ~tens of KB and
  ///   is decoded once on first use.
  /// - O(1) property lookups through a two-stage table (see GraphemeTable).
  /// - Zero allocations while iterating (Each, NextBreak, GetBreaks into a reused list).
  /// 
  /// UNICODE VERSION: see EmbeddedUnicodeData.Version. To update it, run
  /// GraphemeSplitterNET.Generator, or use GraphemeSplitterBuffered to fetch
  /// the latest data from unicode.org at runtime.
  /// 
  /// This is the recommended implementation for general-purpose applications.
  /// 
  /// </summary>
  public class GraphemeSplitter : Unicode
  {

    public GraphemeSplitter() : base(GraphemeTable.Embedded)
    {
    }
  }
}
