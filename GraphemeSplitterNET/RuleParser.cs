using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GraphemeSplitterNET
{
  public class RuleParser
  {

    private const string GraphemeBreakUrl = "https://www.unicode.org/Public/{0}/ucd/auxiliary/GraphemeBreakProperty.txt";
    private const string EmojiDataUrl = "https://www.unicode.org/Public/{0}/ucd/emoji/emoji-data.txt";
    private const string DerivedCoreUrl = "https://www.unicode.org/Public/{0}/ucd/DerivedCoreProperties.txt";

#if NET40
    /// <summary>
    /// Asynchronously downloads and parses the Unicode data files for the given version.
    /// </summary>
    public Task<GraphemeTable> DownloadAsync(UnicodeVersion version)
    {
      return Task.Factory.StartNew(() =>
      {
        var versionStr = ValidateVersion(version);
        using (var client = new WebClient()) {
          var gbp = client.DownloadString(string.Format(GraphemeBreakUrl, versionStr));
          var emoji = client.DownloadString(string.Format(EmojiDataUrl, versionStr));
          var derived = client.DownloadString(string.Format(DerivedCoreUrl, versionStr));
          return Parse(gbp, emoji, derived);
        }
      });
    }
#else
    /// <summary>
    /// Asynchronously downloads and parses the Unicode data files for the given version.
    /// </summary>
    public async Task<GraphemeTable> DownloadAsync(UnicodeVersion version)
    {
      var versionStr = ValidateVersion(version);
      // WebClient does not support concurrent requests on the same instance, so one client per file.
      var graphemeTask = DownloadStringAsync(string.Format(GraphemeBreakUrl, versionStr));
      var emojiTask = DownloadStringAsync(string.Format(EmojiDataUrl, versionStr));
      var derivedTask = DownloadStringAsync(string.Format(DerivedCoreUrl, versionStr));

      await Task.WhenAll(graphemeTask, emojiTask, derivedTask).ConfigureAwait(false);

      return Parse(await graphemeTask, await emojiTask, await derivedTask);
    }

    private static async Task<string> DownloadStringAsync(string url)
    {
      using (var client = new WebClient()) {
        return await client.DownloadStringTaskAsync(url).ConfigureAwait(false);
      }
    }
#endif

    /// <summary>
    /// Parses already available Unicode data files.
    /// derivedCoreProperties is optional, without it GB9c (Indic conjuncts) is not applied.
    /// </summary>
    public GraphemeTable Parse(string graphemeBreakProperty, string emojiData, string derivedCoreProperties)
    {
      // One byte per code point while building, compacted at the end.
      var values = new byte[GraphemeTable.CodePointCount];

      ParseContent(graphemeBreakProperty, values, false);
      ParseContent(emojiData, values, false);
      if (derivedCoreProperties != null) {
        ParseContent(derivedCoreProperties, values, true);
      }

      values[0x200D] = (byte)((values[0x200D] & GraphemeTable.InCBMask) | Unicode.ZWJ);

      return GraphemeTable.Create(values, ReadVersion(graphemeBreakProperty));
    }

    private void ParseContent(string content, byte[] values, bool inCB)
    {
      using (var reader = new StringReader(content)) {
        string line;
        while ((line = reader.ReadLine()) != null) {

          if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) {
            continue;
          }

          var parts = line.Split('#')[0].Split(';');
          if (parts.Length < 2) continue;

          int value;
          if (inCB) {
            // 094D          ; InCB; Linker # Mn       DEVANAGARI SIGN VIRAMA
            if (parts.Length < 3 || parts[1].Trim() != "InCB") continue;
            value = GetInCBFromString(parts[2].Trim());
          }
          else {
            value = GetTypeFromString(parts[1].Trim());
          }
          if (value == -1) continue;

          var codeRangeStr = parts[0].Trim();
          int start, end;
          int dots = codeRangeStr.IndexOf("..", StringComparison.Ordinal);
          if (dots >= 0) {
            start = int.Parse(codeRangeStr.Substring(0, dots), NumberStyles.HexNumber);
            end = int.Parse(codeRangeStr.Substring(dots + 2), NumberStyles.HexNumber);
          }
          else {
            start = end = int.Parse(codeRangeStr, NumberStyles.HexNumber);
          }
          // Ensure end doesn't exceed valid Unicode code point range (U+10FFFF = 1,114,111)
          if (end > 0x10FFFF) end = 0x10FFFF;

          for (int i = start; i <= end; i++) {
            values[i] = inCB
              ? (byte)((values[i] & GraphemeTable.PropertyMask) | value)
              : (byte)((values[i] & GraphemeTable.InCBMask) | value);
          }
        }
      }
    }

    // # GraphemeBreakProperty-17.0.0.txt
    private static string ReadVersion(string content)
    {
      var match = Regex.Match(content ?? string.Empty, @"GraphemeBreakProperty-([0-9.]+)\.txt");
      return match.Success ? match.Groups[1].Value : "unknown";
    }

    private string ValidateVersion(UnicodeVersion version)
    {
      switch (version) {
        case UnicodeVersion.Unicode_10:
          return "10.0.0";
        case UnicodeVersion.Unicode_11:
          return "11.0.0";
        case UnicodeVersion.Unicode_12:
          return "12.0.0";
        case UnicodeVersion.Unicode_13:
          return "13.0.0";
        case UnicodeVersion.Unicode_14:
          return "14.0.0";
        case UnicodeVersion.Unicode_15:
          return "15.0.0";
        case UnicodeVersion.Unicode_16:
          return "16.0.0";
        case UnicodeVersion.Unicode_17:
          return "17.0.0";
        case UnicodeVersion.Unicode_18:
          return "18.0.0";
        case UnicodeVersion.Unicode_Latest:
        default: return "latest";
      }
    }

    private int GetTypeFromString(string propertyName)
    {
      switch (propertyName) {
        case "CR": return Unicode.CR;
        case "LF": return Unicode.LF;
        case "Control": return Unicode.Control;
        case "Extend": return Unicode.Extend;
        case "Regional_Indicator": return Unicode.Regional_Indicator;
        case "SpacingMark": return Unicode.SpacingMark;
        case "L": return Unicode.L;
        case "V": return Unicode.V;
        case "T": return Unicode.T;
        case "LV": return Unicode.LV;
        case "LVT": return Unicode.LVT;
        case "Prepend": return Unicode.Prepend;
        case "ZWJ": return Unicode.ZWJ;
        case "Extended_Pictographic": return Unicode.Extended_Pictographic;
        default: return -1;
      }
    }

    private int GetInCBFromString(string value)
    {
      switch (value) {
        case "Consonant": return GraphemeTable.InCB_Consonant;
        case "Extend": return GraphemeTable.InCB_Extend;
        case "Linker": return GraphemeTable.InCB_Linker;
        default: return -1;
      }
    }
  }
}
