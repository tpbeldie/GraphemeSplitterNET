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

    public abstract int GetGraphemeBreakProperty(int codePoint);

    protected int GetCodePoint(string s, int index)
    {
      if (char.IsHighSurrogate(s[index]) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1])) {
        return char.ConvertToUtf32(s[index], s[index + 1]);
      }
      return s[index];
    }

    protected bool ShouldBreak(int left, int right, List<int> history, int riCount)
    {
      // GB3: CR × LF
      if (left == CR && right == LF) return false;
      // GB4: (Control | CR | LF) ÷
      if (left == Control || left == CR || left == LF) return true;
      // GB5: ÷ (Control | CR | LF)
      if (right == Control || right == CR || right == LF) return true;
      // GB6: L × (L | V | LV | LVT)
      if (left == L && (right == L || right == V || right == LV || right == LVT)) return false;
      // GB7: (LV | V) × (V | T)
      if ((left == LV || left == V) && (right == V || right == T)) return false;
      // GB8: (LVT | T) × T
      if ((left == LVT || left == T) && (right == T)) return false;
      // GB9: × (Extend | ZWJ)
      if (right == Extend || right == ZWJ) return false;
      // GB9a: × SpacingMark
      if (right == SpacingMark) return false;
      // GB9b: Prepend ×
      if (left == Prepend) return false;

      // GB11: Extended_Pictographic Extend* ZWJ × Extended_Pictographic
      if (right == Extended_Pictographic) {
        if (history.Count >= 2 && history[history.Count - 1] == ZWJ) {
          for (int i = history.Count - 2; i >= 0; i--) {
            if (history[i] == Extend) {
              continue;
            }
            if (history[i] == Extended_Pictographic) {
              return false;
            }
            break;
          }
        }
      }

      // GB12 & GB13: Regional indicators
      if (left == Regional_Indicator && right == Regional_Indicator && riCount % 2 == 1) {
        return false;
      }

      return true; // GB999: Any ÷ Any
    }

    // Calculates the number of grapheme clusters in a string.
    public int GetLength(string strText)
    {
      return SplitPrivate(strText, null, 0, null, null);
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    public void Each(string strText, EachVoidCallBack cb)
    {
      SplitPrivate(strText, null, 0, cb, null);
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    public void Each(string strText, int nIndex, EachVoidCallBack cb)
    {
      SplitPrivate(strText, null, nIndex, cb, null);
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    public void Each(string strText, EachBoolCallBack cb)
    {
      SplitPrivate(strText, null, 0, null, cb);
    }

    // Iterates over each grapheme cluster in a string, invoking a callback for each one.
    public void Each(string strText, int nIndex, EachBoolCallBack cb)
    {
      SplitPrivate(strText, null, nIndex, null, cb);
    }


    //  Splits a string into a list of its constituent grapheme clusters.
    public List<string> Split(string strText)
    {
      if (string.IsNullOrEmpty(strText)) {
        return new List<string>(0);
      }
      // Allocate the result list with a reasonable initial capacity.
      // Using strText.Length / 2 as heuristic to reduce reallocations:
      // most grapheme clusters are 1-2 chars long, so we usually overestimate a bit.
      var listResult = new List<string>(strText.Length / 2);
      SplitPrivate(strText, listResult, 0, null, null);
      return listResult;
    }

    private int SplitPrivate(string strText, List<string> listResult, int currentIndex, EachVoidCallBack cbVoid, EachBoolCallBack cbBool)
    {
      if (string.IsNullOrEmpty(strText)) return 0;

      int count = 0;
      int riCounter = 0;
      var history = new List<int>(4);

      int charStart = 0;
      int charLen;

      int codePoint = GetCodePoint(strText, currentIndex);
      int breakTypeLeft = GetGraphemeBreakProperty(codePoint);
      charLen = codePoint >= 0x10000 ? 2 : 1;
      currentIndex += charLen;
      history.Add(breakTypeLeft);

      while (currentIndex < strText.Length) {
        codePoint = GetCodePoint(strText, currentIndex);
        int breakTypeRight = GetGraphemeBreakProperty(codePoint);
        int currentCodeLen = codePoint >= 0x10000 ? 2 : 1;

        if (breakTypeLeft == Regional_Indicator) riCounter++; else riCounter = 0;

        if (ShouldBreak(breakTypeLeft, breakTypeRight, history, riCounter)) {
          listResult?.Add(strText.Substring(charStart, charLen));
          cbVoid?.Invoke(strText, charStart, charLen);
          if (cbBool != null && !cbBool(strText, charStart, charLen)) return count + 1;

          count++;
          charStart = currentIndex;
          charLen = currentCodeLen;
          history.Clear();
        }
        else {
          charLen += currentCodeLen;
        }

        history.Add(breakTypeRight);
        currentIndex += currentCodeLen;
        breakTypeLeft = breakTypeRight;
      }

      listResult?.Add(strText.Substring(charStart, charLen));
      cbVoid?.Invoke(strText, charStart, charLen);
      cbBool?.Invoke(strText, charStart, charLen);

      return count + 1;
    }
  }
}
