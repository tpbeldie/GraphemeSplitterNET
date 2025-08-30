using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GraphemeSplitterNET
{
  public class RuleParser
  {

    private const string GraphemeBreakUrl = "https://unicode.org/Public/UCD/{0}/ucd/auxiliary/GraphemeBreakProperty.txt";
    private const string EmojiDataUrl = "https://www.unicode.org/Public/UCD/{0}/ucd/emoji/emoji-data.txt";

    /// <summary>
    /// Asynchronously downloads and parses the Unicode data files.
    /// </summary>
    public async Task<Dictionary<int, int>> ParseAsync(UnicodeVersion version)
    {
      var codePoints = new Dictionary<int, int>();
      using (var client = new WebClient()) { 

        var versionStr = ValidateVersion(version);
        var graphemeTask = client.DownloadStringTaskAsync(string.Format(GraphemeBreakUrl, versionStr));
        var emojiTask = client.DownloadStringTaskAsync(string.Format(EmojiDataUrl, versionStr));

        await Task.WhenAll(graphemeTask, emojiTask).ConfigureAwait(false);

        ParseContent(await graphemeTask, codePoints);
        ParseContent(await emojiTask, codePoints);

        codePoints[0x200D] = Unicode.ZWJ;

        return codePoints;
      }
    }

    private void ParseContent(string content, Dictionary<int, int> codePoints)
    {
      using (var reader = new StringReader(content)) {
        string line;
        while ((line = reader.ReadLine()) != null) {

          if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) {
            continue;
          }

          var parts = line.Split(';');
          if (parts.Length < 2) continue;

          var codeRangeStr = parts[0].Trim();
          var propertyName = parts[1].Split('#')[0].Trim();

          int propertyType = GetTypeFromString(propertyName);
          if (propertyType == -1) continue;

          if (codeRangeStr.Contains("..")) {
            var rangeParts = codeRangeStr.Split(new[] { ".." }, StringSplitOptions.None);
            int start = int.Parse(rangeParts[0], NumberStyles.HexNumber);
            int end = int.Parse(rangeParts[1], NumberStyles.HexNumber);
            for (int i = start; i <= end; i++) {
              codePoints[i] = propertyType;
            }
          }
          else {
            int code = int.Parse(codeRangeStr, NumberStyles.HexNumber);
            codePoints[code] = propertyType;
          }
        }
      }
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
        case "Extended_Pictographic": return Unicode.Extended_Pictographic;
        default: return -1;
      }
    }
  }
}
