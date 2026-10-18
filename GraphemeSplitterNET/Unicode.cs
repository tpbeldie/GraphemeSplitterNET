using System;
using System.Collections.Generic;

namespace GraphemeSplitterNET
{

  public enum UnicodeVersion
  {
    Unicode_10,
    Unicode_11,
    Unicode_12,
    Unicode_13,
    Unicode_14,
    Unicode_15,
    Unicode_16,
    Unicode_17,
    Unicode_18,
    Unicode_Latest
  }

  public abstract class Unicode
  {

    public delegate void EachVoidCallBack(string strText, int nIndex, int nLen);
    public delegate bool EachBoolCallBack(string strText, int nIndex, int nLen);

    public const int Other = 0;
    public const int CR = 1;
    public const int LF = 2;
    public const int Control = 3;
    public const int Extend = 4;
    public const int Regional_Indicator = 5;
    public const int SpacingMark = 6;
    public const int L = 7;
    public const int V = 8;
    public const int T = 9;
    public const int LV = 10;
    public const int LVT = 11;
    public const int Prepend = 12;
    public const int ZWJ = 15;
    public const int Extended_Pictographic = 18;

    private GraphemeTable m_table;

    protected Unicode(GraphemeTable table)
    {
      m_table = table ?? throw new ArgumentNullException(nameof(table));
    }

    /// <summary>
    /// The lookup table used by this splitter.
    /// </summary>
    public GraphemeTable Table
    {
      get { return m_table; }
      protected set { m_table = value ?? throw new ArgumentNullException(nameof(value)); }
    }

    /// <summary>
    /// Returns the grapheme break property of a code point.
    /// Note: the splitting loop reads the table directly for speed, it does not call this method.
    /// </summary>
    public virtual int GetGraphemeBreakProperty(int codePoint)
    {
      return m_table.GetProperty(codePoint);
    }

    protected int GetCodePoint(string s, int index)
    {
      if (char.IsHighSurrogate(s[index]) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1])) {
        return char.ConvertToUtf32(s[index], s[index + 1]);
      }
      return s[index];
    }

    /// :::::::: Core ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    /// https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundary_Rules
    /// 
    /// Every rule only looks inside the current cluster, so instead of keeping a history list
    /// we carry four tiny pieces of state that are reset implicitly on each new cluster:
    /// 
    ///   riCount  - number of consecutive Regional_Indicator ending at the left char (GB12/13).
    ///   emojiSeq - we are in  Extended_Pictographic Extend*      (GB11).
    ///   emojiZwj - we are in  Extended_Pictographic Extend* ZWJ  (GB11).
    ///   incb     - 1: InCB Consonant [Extend|Linker]*            (GB9c).
    ///              2: same, with at least one Linker seen.
    /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

    // Returns the end of the cluster starting at index. index must be < s.Length.
    private int NextBoundary(string s, int index)
    {
      int length = s.Length;
      char c = s[index];

      // ASCII fast path: printable ASCII is Other, and nothing below U+0300
      // (Extend, ZWJ, SpacingMark) can be glued to it, so it is always a single cluster.
      if (c >= 0x20 && c < 0x7F) {
        int n = index + 1;
        if (n >= length || s[n] < 0x0300) return n;
      }

      ushort[] stage1 = m_table.Stage1;
      byte[] stage2 = m_table.Stage2;

      int cp = c;
      int pos = index + 1;
      if ((uint)(c - 0xD800) <= 0x3FF && pos < length && (uint)(s[pos] - 0xDC00) <= 0x3FF) {
        cp = ((c - 0xD800) << 10) + (s[pos] - 0xDC00) + 0x10000;
        pos++;
      }

      int left = stage2[(stage1[cp >> 8] << 8) | (cp & 0xFF)];
      int lp = left & GraphemeTable.PropertyMask;

      int riCount = lp == Regional_Indicator ? 1 : 0;
      bool emojiSeq = lp == Extended_Pictographic;
      bool emojiZwj = false;
      bool incbLinker = (left & GraphemeTable.InCBMask) == GraphemeTable.InCB_Linker;

      while (pos < length) {
        c = s[pos];
        cp = c;
        int next = pos + 1;
        if ((uint)(c - 0xD800) <= 0x3FF && next < length && (uint)(s[next] - 0xDC00) <= 0x3FF) {
          cp = ((c - 0xD800) << 10) + (s[next] - 0xDC00) + 0x10000;
          next++;
        }

        int right = stage2[(stage1[cp >> 8] << 8) | (cp & 0xFF)];
        int rp = right & GraphemeTable.PropertyMask;
        int rIncb = right & GraphemeTable.InCBMask;

        bool join;
        if (lp == CR || lp == LF || lp == Control) {
          // GB3: CR × LF, GB4: (Control | CR | LF) ÷
          join = lp == CR && rp == LF;
        }
        else if (rp == CR || rp == LF || rp == Control) {
          // GB5: ÷ (Control | CR | LF)
          join = false;
        }
        else if (rp == Extend || rp == ZWJ || rp == SpacingMark || lp == Prepend) {
          // GB9: × (Extend | ZWJ), GB9a: × SpacingMark, GB9b: Prepend ×
          join = true;
        }
        else {
          join =
            // GB6: L × (L | V | LV | LVT)
            (lp == L && (rp == L || rp == V || rp == LV || rp == LVT)) ||
            // GB7: (LV | V) × (V | T)
            ((lp == LV || lp == V) && (rp == V || rp == T)) ||
            // GB8: (LVT | T) × T
            ((lp == LVT || lp == T) && rp == T) ||
            // GB9c: \p{InCB=Linker} [\p{InCB=Extend} \p{InCB=Linker}]* × \p{InCB=Consonant}
            (incbLinker && rIncb == GraphemeTable.InCB_Consonant) ||
            // GB11: ExtPict Extend* ZWJ × ExtPict
            (emojiZwj && rp == Extended_Pictographic) ||
            // GB12/GB13: RI pairs
            (lp == Regional_Indicator && rp == Regional_Indicator && (riCount & 1) == 1);
        }

        if (!join) return pos; // GB999: Any ÷ Any

        // Advance the state with the right char.
        if (rIncb == GraphemeTable.InCB_Linker) incbLinker = true;
        else if (rIncb != GraphemeTable.InCB_Extend) incbLinker = false;

        emojiZwj = emojiSeq && rp == ZWJ;
        emojiSeq = rp == Extended_Pictographic || (emojiSeq && rp == Extend);
        riCount = rp == Regional_Indicator ? riCount + 1 : 0;

        lp = rp;
        pos = next;
      }
      return length;
    }

    /// :::::::: Public API :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

    // Calculates the number of grapheme clusters in a string.
    public int GetLength(string strText)
    {
      if (string.IsNullOrEmpty(strText)) return 0;
      int count = 0;
      for (int i = 0; i < strText.Length; i = NextBoundary(strText, i)) {
        count++;
      }
      return count;
    }

    // Same as GetLength, named after STGraphemeSplitter.GetCount.
    public int GetCount(string strText)
    {
      return GetLength(strText);
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    public void Each(string strText, EachVoidCallBack cb)
    {
      Each(strText, 0, cb);
    }

    // Iterates over each grapheme cluster starting at nIndex, invoking a callback for each one.
    // nIndex is assumed to be a cluster boundary. Reported indices are relative to strText.
    public void Each(string strText, int nIndex, EachVoidCallBack cb)
    {
      if (cb == null) throw new ArgumentNullException(nameof(cb));
      if (string.IsNullOrEmpty(strText)) return;
      CheckIndex(strText, nIndex);
      int i = nIndex;
      while (i < strText.Length) {
        int end = NextBoundary(strText, i);
        cb(strText, i, end - i);
        i = end;
      }
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    // Returning false from the callback stops the iteration.
    public void Each(string strText, EachBoolCallBack cb)
    {
      Each(strText, 0, cb);
    }

    // Iterates over each grapheme cluster starting at nIndex, invoking a callback for each one.
    // Returning false from the callback stops the iteration.
    public void Each(string strText, int nIndex, EachBoolCallBack cb)
    {
      if (cb == null) throw new ArgumentNullException(nameof(cb));
      if (string.IsNullOrEmpty(strText)) return;
      CheckIndex(strText, nIndex);
      int i = nIndex;
      while (i < strText.Length) {
        int end = NextBoundary(strText, i);
        if (!cb(strText, i, end - i)) return;
        i = end;
      }
    }

    //  Splits a string into a list of its constituent grapheme clusters.
    public List<string> Split(string strText)
    {
      if (string.IsNullOrEmpty(strText)) {
        return new List<string>(0);
      }
      // Most grapheme clusters are 1-2 chars long, so Length / 2 rarely needs to grow.
      var listResult = new List<string>(strText.Length / 2 + 1);
      int i = 0;
      while (i < strText.Length) {
        int end = NextBoundary(strText, i);
        listResult.Add(strText.Substring(i, end - i));
        i = end;
      }
      return listResult;
    }

    /// <summary>
    /// Gets a list of start indices for each grapheme cluster. 
    /// This is much faster than Split() for large text as it avoids string allocations.
    /// Useful for mapping grapheme clusters to character positions.
    /// </summary>
    public List<int> GetBreaks(string strText)
    {
      var boundaries = new List<int>(string.IsNullOrEmpty(strText) ? 0 : strText.Length / 2 + 1);
      GetBreaks(strText, boundaries);
      return boundaries;
    }

    /// <summary>
    /// Same as GetBreaks(string) but appends into an existing list, so the list can be reused
    /// between calls (zero allocation once the list has grown big enough).
    /// </summary>
    public void GetBreaks(string strText, List<int> boundaries)
    {
      if (boundaries == null) throw new ArgumentNullException(nameof(boundaries));
      if (string.IsNullOrEmpty(strText)) return;
      int i = 0;
      while (i < strText.Length) {
        boundaries.Add(i);
        i = NextBoundary(strText, i);
      }
    }

    /// <summary>
    /// Finds the index of the next grapheme cluster boundary after the specified start index.
    /// Zero-allocation and highly optimized for iterating or cursor movement.
    /// </summary>
    /// <param name="strText">The text to analyze.</param>
    /// <param name="startIndex">The index to start looking from, assumed to be a cluster boundary.</param>
    /// <returns>The index of the next break, or strText.Length if no more breaks are found.</returns>
    public int NextBreak(string strText, int startIndex)
    {
      if (string.IsNullOrEmpty(strText) || startIndex >= strText.Length) return strText?.Length ?? 0;
      if (startIndex < 0) startIndex = 0;
      return NextBoundary(strText, startIndex);
    }

    private static void CheckIndex(string strText, int nIndex)
    {
      if (nIndex < 0 || nIndex > strText.Length) {
        throw new ArgumentOutOfRangeException(nameof(nIndex));
      }
    }
  }
}
