using GraphemeSplitterNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace GraphemeSplitterNET.Tests
{
  /// <summary>
  /// Runs the official Unicode conformance file:
  /// https://www.unicode.org/Public/latest/ucd/auxiliary/GraphemeBreakTest.txt
  /// </summary>
  public class ConformanceTests
  {

    public static IEnumerable<object[]> Cases()
    {
      string path = Path.Combine(AppContext.BaseDirectory, "Data", "GraphemeBreakTest.txt");
      int lineNumber = 0;
      foreach (var raw in File.ReadLines(path)) {
        lineNumber++;
        string line = raw.Split('#')[0].Trim();
        if (line.Length == 0) continue;
        yield return new object[] { lineNumber, line, raw.Contains('#') ? raw.Substring(raw.IndexOf('#')).Trim() : "" };
      }
    }

    // "÷ 0020 × 0308 ÷" -> text, expected cluster start indices.
    internal static (string text, List<int> breaks) ParseCase(string line)
    {
      var sb = new StringBuilder();
      var breaks = new List<int>();
      foreach (var token in line.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
        if (token == "÷") {
          if (!breaks.Contains(sb.Length)) breaks.Add(sb.Length);
        }
        else if (token != "×") {
          sb.Append(char.ConvertFromUtf32(int.Parse(token, NumberStyles.HexNumber)));
        }
      }
      breaks.Remove(sb.Length); // Final ÷ is the end of text, not a cluster start.
      return (sb.ToString(), breaks);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GetBreaks_MatchesUnicode(int lineNumber, string line, string comment)
    {
      var (text, expected) = ParseCase(line);
      var actual = new GraphemeSplitter().GetBreaks(text);
      Assert.True(expected.SequenceEqual(actual),
        $"Line {lineNumber}: {line}\nExpected [{string.Join(",", expected)}] got [{string.Join(",", actual)}]\n{comment}");
    }

    [Fact]
    public void AllCases_AllApisAgree()
    {
      var splitter = new GraphemeSplitter();
      foreach (var row in Cases()) {
        var (text, expected) = ParseCase((string)row[1]);

        // Each
        var each = new List<int>();
        splitter.Each(text, (s, i, n) => each.Add(i));
        Assert.Equal(expected, each);

        // NextBreak
        var next = new List<int>();
        for (int i = 0; i < text.Length; i = splitter.NextBreak(text, i)) next.Add(i);
        Assert.Equal(expected, next);

        // Split + GetLength
        var parts = splitter.Split(text);
        Assert.Equal(expected.Count, parts.Count);
        Assert.Equal(expected.Count, splitter.GetLength(text));
        Assert.Equal(text, string.Concat(parts));
      }
    }
  }
}
