using GraphemeSplitterNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace GraphemeSplitterNET.Tests
{
  public class SplitterTests
  {

    private static readonly GraphemeSplitter Splitter = new GraphemeSplitter();

    // Samples that exercise every rule, mixed with ASCII.
    private static readonly string[] Pieces = {
      "a", "Z", " ", "\r\n", "\n", "\r", "\t",
      "e\u0301", "n\u0303\u0308",                       // GB9 combining marks
      "\U0001F468\u200D\U0001F469\u200D\U0001F467",      // GB11 family
      "\U0001F44D\U0001F3FD",                            // emoji + skin tone
      "\U0001F1FA\U0001F1F8", "\U0001F1EF\U0001F1F5",    // GB12/13 flags
      "\u1100\u1161\u11A8", "\uAC00", "\uAC01\u11A8",    // GB6-8 Hangul
      "\u0915\u094D\u0937", "\u0915\u093F",              // GB9c, GB9a Devanagari
      "\u0600\u0661",                                    // GB9b Prepend
      "\uD800", "\uDC00",                                // lone surrogates
      "\u00A9\uFE0F", "\u2764\uFE0F\u200D\U0001F525",
    };

    private static string RandomText(Random random, int pieces)
    {
      var sb = new StringBuilder();
      for (int i = 0; i < pieces; i++) sb.Append(Pieces[random.Next(Pieces.Length)]);
      return sb.ToString();
    }

    [Fact]
    public void KnownClusters()
    {
      Assert.Equal(new[] { "e\u0301", "a" }, Splitter.Split("e\u0301a"));
      Assert.Equal(new[] { "\r\n", "x" }, Splitter.Split("\r\nx"));
      Assert.Single(Splitter.Split("\U0001F468\u200D\U0001F469\u200D\U0001F467"));
      Assert.Equal(2, Splitter.GetLength("\U0001F1FA\U0001F1F8\U0001F1EF\U0001F1F5"));
      Assert.Equal(2, Splitter.GetLength("\U0001F1FA\U0001F1F8\U0001F1EF"));
      Assert.Single(Splitter.Split("\u0915\u094D\u0937"));     // क्ष (GB9c)
      Assert.Equal(5, Splitter.GetLength("Hello"));
      Assert.Empty(Splitter.Split(""));
      Assert.Equal(0, Splitter.GetLength(null));
    }

    [Fact]
    public void Each_WithIndex_ReportsAbsoluteIndices()
    {
      string text = "ab\U0001F468\u200D\U0001F469cd";
      var all = new List<(int, int)>();
      Splitter.Each(text, (s, i, n) => all.Add((i, n)));

      // Start from every boundary, the result must be the tail of the full iteration.
      for (int k = 0; k < all.Count; k++) {
        var tail = new List<(int, int)>();
        Splitter.Each(text, all[k].Item1, (s, i, n) => tail.Add((i, n)));
        Assert.Equal(all.Skip(k), tail);
      }

      var tailBool = new List<int>();
      Splitter.Each(text, 2, (string s, int i, int n) => { tailBool.Add(i); return true; });
      Assert.Equal(new[] { 2, 7, 8 }, tailBool);
    }

    [Fact]
    public void Each_Bool_StopsWhenFalse()
    {
      int calls = 0;
      Splitter.Each("abcdef", (string s, int i, int n) => ++calls < 3);
      Assert.Equal(3, calls);
    }

    [Fact]
    public void Each_IndexAtEnd_DoesNothing_AndOutOfRangeThrows()
    {
      int calls = 0;
      Splitter.Each("abc", 3, (s, i, n) => calls++);
      Assert.Equal(0, calls);
      Assert.Throws<ArgumentOutOfRangeException>(() => Splitter.Each("abc", 4, (s, i, n) => { }));
      Assert.Throws<ArgumentOutOfRangeException>(() => Splitter.Each("abc", -1, (s, i, n) => { }));
    }

    [Fact]
    public void RandomText_AllApisAgree()
    {
      var random = new Random(1234);
      var reused = new List<int>();
      for (int round = 0; round < 2000; round++) {
        string text = RandomText(random, random.Next(1, 40));

        var breaks = Splitter.GetBreaks(text);
        reused.Clear();
        Splitter.GetBreaks(text, reused);
        Assert.Equal(breaks, reused);

        var each = new List<int>();
        Splitter.Each(text, (s, i, n) => { Assert.Equal(i + n, i + n <= s.Length ? i + n : -1); each.Add(i); });
        Assert.Equal(breaks, each);

        var next = new List<int>();
        for (int i = 0; i < text.Length; i = Splitter.NextBreak(text, i)) next.Add(i);
        Assert.Equal(breaks, next);

        Assert.Equal(text, string.Concat(Splitter.Split(text)));
        Assert.Equal(breaks.Count, Splitter.GetLength(text));
      }
    }

    [Fact]
    public void AsciiFastPath_MatchesTable()
    {
      // Every printable ASCII followed by every char below U+0300 must break between them.
      for (char a = ' '; a < 0x7F; a++) {
        for (char b = '\0'; b < 0x300; b++) {
          string s = new string(new[] { a, b });
          Assert.Equal(new List<int> { 0, 1 }, Splitter.GetBreaks(s));
        }
      }
    }

    [Fact]
    public void GetGraphemeBreakProperty_UsesFullData()
    {
      Assert.Equal(Unicode.CR, Splitter.GetGraphemeBreakProperty(0x0D));
      Assert.Equal(Unicode.ZWJ, Splitter.GetGraphemeBreakProperty(0x200D));
      Assert.Equal(Unicode.Extended_Pictographic, Splitter.GetGraphemeBreakProperty(0x1F600));
      Assert.Equal(Unicode.LVT, Splitter.GetGraphemeBreakProperty(0xAC01));
      Assert.Equal(Unicode.Extend, Splitter.GetGraphemeBreakProperty(0xE0100)); // variation selector supplement
      Assert.Equal(Unicode.Other, Splitter.GetGraphemeBreakProperty(0x10FFFF));
      Assert.Equal(Unicode.Other, Splitter.GetGraphemeBreakProperty(0x110000));
      Assert.Equal(Unicode.Other, Splitter.GetGraphemeBreakProperty(-1));
    }

    [Fact]
    public void Table_RoundTrips_AndIsCompact()
    {
      var table = GraphemeTable.Embedded;
      Assert.NotEqual("unknown", table.UnicodeVersion);
      byte[] bytes = table.ToBytes();
      Assert.True(bytes.Length < 64 * 1024, $"Table is {bytes.Length} bytes");

      var copy = GraphemeTable.FromBytes(bytes);
      Assert.Equal(table.UnicodeVersion, copy.UnicodeVersion);
      for (int cp = 0; cp < GraphemeTable.CodePointCount; cp++) {
        if (table.GetValue(cp) != copy.GetValue(cp)) Assert.Fail($"Mismatch at U+{cp:X4}");
      }
    }

    [Fact]
    public void Table_RejectsCorruptedData()
    {
      byte[] bytes = GraphemeTable.Embedded.ToBytes();
      Assert.ThrowsAny<Exception>(() => GraphemeTable.FromBytes(bytes.Take(bytes.Length / 2).ToArray()));
      bytes[0] ^= 0xFF;
      Assert.Throws<InvalidDataException>(() => GraphemeTable.FromBytes(bytes));
    }

    [Fact]
    public void RuleParser_ParsesInCB()
    {
      const string gbp = "# GraphemeBreakProperty-99.0.0.txt\n0915..0939 ; Other\n093E ; SpacingMark\n094D ; Extend # virama\n";
      const string emoji = "1F600 ; Extended_Pictographic # grinning\n1F600 ; Emoji\n";
      const string derived = "# @missing: 0000..10FFFF; InCB; None\n094D ; InCB; Linker\n0915..0939 ; InCB; Consonant\n";
      var table = new RuleParser().Parse(gbp, emoji, derived);

      Assert.Equal("99.0.0", table.UnicodeVersion);
      Assert.Equal(Unicode.Extend | GraphemeTable.InCB_Linker, table.GetValue(0x094D));
      Assert.Equal(Unicode.Other | GraphemeTable.InCB_Consonant, table.GetValue(0x0915));
      Assert.Equal(Unicode.SpacingMark, table.GetValue(0x093E));
      Assert.Equal(Unicode.Extended_Pictographic, table.GetValue(0x1F600));
      Assert.Equal(Unicode.ZWJ, table.GetValue(0x200D));
    }
  }
}
