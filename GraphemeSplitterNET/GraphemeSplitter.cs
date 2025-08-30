namespace GraphemeSplitterNET
{
  /// <summary>
  /// 
  /// Am extremely lightweight, zero-memory grapheme splitter 
  /// using hardcoded, compile-time Unicode rules ranges.
  /// 
  /// Its primary advantage is ZERO memory allocation for the rule lookup table,
  /// making it suitable for extremely memory-constrained environments.
  /// 
  ///  :: Trade-offs: ::
  ///  
  /// - Performance is O(n) for property lookups, which can be slower than a 
  /// cached approach for complex text.
  /// 
  /// - The rules are frozen at compile time and require manual code changes
  /// to update to new Unicode versions.
  /// 
  /// - This implementation contains a representative subset of rules. 
  /// A complete implementation would be extremely large.
  /// 
  /// </summary>
  public class GraphemeSplitter : Unicode
  {

    /// <summary>
    /// 
    /// Zero-memory O(n) Unicode property lookup using hardcoded ranges.
    /// No arrays, no dictionaries, no memory allocation.
    /// However, this is a linear checking of ranges, so it's not O(1) like a table lookup. 
    /// Best case: A character such as CR (0x000D) is found at first sight, super fast.
    /// Worst case: A character that matches no rule and becomes Other and goes through 
    /// all checks, still fast but not fast enough.
    /// 
    /// And we have to manually handle each case.
    /// This is the recommended implementation for general-purpose applications. 
    /// It will suffice for most unpretentious cases.
    /// 
    /// </summary>
    public override int GetGraphemeBreakProperty(int codePoint)
    {
      int cp = codePoint;

      // Special cases first (most common)
      if (cp == 0x000D) return CR;                    // CR
      if (cp == 0x000A) return LF;                    // LF
      if (cp == 0x200D) return ZWJ;                   // ZWJ

      // Control characters
      if (cp <= 0x001F || (cp >= 0x007F && cp <= 0x009F)) return Control;
      if (cp >= 0x200C && cp <= 0x200F) return Control;
      if (cp >= 0x2028 && cp <= 0x202E) return Control;
      if (cp >= 0x2060 && cp <= 0x2064) return Control;
      if (cp >= 0x2066 && cp <= 0x206F) return Control;
      if (cp == 0xFEFF) return Control;

      // Extend (combining marks, etc.)
      if (cp >= 0x0300 && cp <= 0x036F) return Extend; // Combining Diacritical Marks
      if (cp >= 0x0483 && cp <= 0x0489) return Extend; // Cyrillic combining
      if (cp >= 0x0591 && cp <= 0x05BD) return Extend; // Hebrew accents
      if (cp >= 0x05BF && cp <= 0x05C7) return Extend;
      if (cp >= 0x0610 && cp <= 0x061A) return Extend; // Arabic marks
      if (cp >= 0x064B && cp <= 0x065F) return Extend; // Arabic diacritics
      if (cp >= 0x0670 && cp <= 0x0670) return Extend;
      if (cp >= 0x06D6 && cp <= 0x06DC) return Extend;
      if (cp >= 0x06DF && cp <= 0x06E4) return Extend;
      if (cp >= 0x06E7 && cp <= 0x06E8) return Extend;
      if (cp >= 0x06EA && cp <= 0x06ED) return Extend;
      if (cp >= 0x0711 && cp <= 0x0711) return Extend;
      if (cp >= 0x0730 && cp <= 0x074A) return Extend;
      if (cp >= 0x07A6 && cp <= 0x07B0) return Extend;
      if (cp >= 0x07EB && cp <= 0x07F3) return Extend;
      if (cp >= 0x0816 && cp <= 0x0819) return Extend;
      if (cp >= 0x081B && cp <= 0x0823) return Extend;
      if (cp >= 0x0825 && cp <= 0x0827) return Extend;
      if (cp >= 0x0829 && cp <= 0x082D) return Extend;
      if (cp >= 0x0859 && cp <= 0x085B) return Extend;
      if (cp >= 0x08D4 && cp <= 0x08E1) return Extend;
      if (cp >= 0x08E3 && cp <= 0x0903) return Extend;
      if (cp >= 0x093A && cp <= 0x093C) return Extend;
      if (cp >= 0x093E && cp <= 0x094F) return Extend;
      if (cp >= 0x0951 && cp <= 0x0957) return Extend;
      if (cp >= 0x0962 && cp <= 0x0963) return Extend;
      if (cp >= 0x0981 && cp <= 0x0983) return Extend;
      if (cp >= 0x09BC && cp <= 0x09BC) return Extend;
      if (cp >= 0x09BE && cp <= 0x09C4) return Extend;
      if (cp >= 0x09C7 && cp <= 0x09C8) return Extend;
      if (cp >= 0x09CB && cp <= 0x09CD) return Extend;
      if (cp >= 0x09D7 && cp <= 0x09D7) return Extend;
      if (cp >= 0x09E2 && cp <= 0x09E3) return Extend;
      if (cp >= 0x0A01 && cp <= 0x0A03) return Extend;
      if (cp >= 0x0A3C && cp <= 0x0A3C) return Extend;
      if (cp >= 0x0A3E && cp <= 0x0A42) return Extend;
      if (cp >= 0x0A47 && cp <= 0x0A48) return Extend;
      if (cp >= 0x0A4B && cp <= 0x0A4D) return Extend;
      if (cp >= 0x0A51 && cp <= 0x0A51) return Extend;
      if (cp >= 0x0A70 && cp <= 0x0A71) return Extend;
      if (cp >= 0x0A75 && cp <= 0x0A75) return Extend;
      if (cp >= 0x0A81 && cp <= 0x0A83) return Extend;
      if (cp >= 0x0ABC && cp <= 0x0ABC) return Extend;
      if (cp >= 0x0ABE && cp <= 0x0AC5) return Extend;
      if (cp >= 0x0AC7 && cp <= 0x0AC9) return Extend;
      if (cp >= 0x0ACB && cp <= 0x0ACD) return Extend;
      if (cp >= 0x0AE2 && cp <= 0x0AE3) return Extend;
      if (cp >= 0x0AFA && cp <= 0x0AFF) return Extend;

      // Regional Indicators (flag sequences)
      if (cp >= 0x1F1E6 && cp <= 0x1F1FF) return Regional_Indicator;

      // Extended Pictographic (emojis)
      if (cp >= 0x1F300 && cp <= 0x1F5FF) return Extended_Pictographic; // Misc Symbols
      if (cp >= 0x1F600 && cp <= 0x1F64F) return Extended_Pictographic; // Emoticons
      if (cp >= 0x1F680 && cp <= 0x1F6FF) return Extended_Pictographic; // Transport
      if (cp >= 0x1F700 && cp <= 0x1F77F) return Extended_Pictographic; // Alchemical
      if (cp >= 0x1F780 && cp <= 0x1F7FF) return Extended_Pictographic; // Geometric Extended
      if (cp >= 0x1F800 && cp <= 0x1F8FF) return Extended_Pictographic; // Supplemental Arrows-C
      if (cp >= 0x1F900 && cp <= 0x1F9FF) return Extended_Pictographic; // Supplemental Symbols
      if (cp >= 0x1FA00 && cp <= 0x1FA6F) return Extended_Pictographic; // Chess Symbols
      if (cp >= 0x1FA70 && cp <= 0x1FAFF) return Extended_Pictographic; // Symbols and Pictographs Extended-A
      if (cp >= 0x2600 && cp <= 0x26FF) return Extended_Pictographic;   // Misc Symbols
      if (cp >= 0x2700 && cp <= 0x27BF) return Extended_Pictographic;   // Dingbats
      if (cp == 0x00A9 || cp == 0x00AE) return Extended_Pictographic;   // Copyright, Registered
      if (cp >= 0x2122 && cp <= 0x2122) return Extended_Pictographic;   // Trademark
      if (cp >= 0x2194 && cp <= 0x2199) return Extended_Pictographic;   // Arrows
      if (cp >= 0x21A9 && cp <= 0x21AA) return Extended_Pictographic;   // Arrows
      if (cp >= 0x231A && cp <= 0x231B) return Extended_Pictographic;   // Watch, Hourglass
      if (cp >= 0x2328 && cp <= 0x2328) return Extended_Pictographic;   // Keyboard
      if (cp >= 0x23CF && cp <= 0x23CF) return Extended_Pictographic;   // Eject
      if (cp >= 0x23E9 && cp <= 0x23F3) return Extended_Pictographic;   // Media controls
      if (cp >= 0x23F8 && cp <= 0x23FA) return Extended_Pictographic;   // Media controls
      if (cp >= 0x24C2 && cp <= 0x24C2) return Extended_Pictographic;   // Circled M
      if (cp >= 0x25AA && cp <= 0x25AB) return Extended_Pictographic;   // Squares
      if (cp >= 0x25B6 && cp <= 0x25B6) return Extended_Pictographic;   // Play
      if (cp >= 0x25C0 && cp <= 0x25C0) return Extended_Pictographic;   // Reverse play
      if (cp >= 0x25FB && cp <= 0x25FE) return Extended_Pictographic;   // Squares

      // Hangul
      if (cp >= 0x1100 && cp <= 0x115F) return L;     // Hangul Jamo L
      if (cp >= 0xA960 && cp <= 0xA97C) return L;     // Hangul Jamo Extended-A L
      if (cp >= 0x1160 && cp <= 0x11A7) return V;     // Hangul Jamo V
      if (cp >= 0xD7B0 && cp <= 0xD7C6) return V;     // Hangul Jamo Extended-B V
      if (cp >= 0x11A8 && cp <= 0x11FF) return T;     // Hangul Jamo T
      if (cp >= 0xD7CB && cp <= 0xD7FB) return T;     // Hangul Jamo Extended-B T
      if (cp >= 0xAC00 && cp <= 0xD7A3) {
        // Hangul Syllables
        int syllableIndex = cp - 0xAC00;
        int tIndex = syllableIndex % 28;
        return (tIndex == 0) ? LV : LVT;
      }

      // SpacingMark (basic cases)
      if (cp >= 0x0903 && cp <= 0x0903) return SpacingMark;
      if (cp >= 0x093B && cp <= 0x093B) return SpacingMark;
      if (cp >= 0x093E && cp <= 0x0940) return SpacingMark;
      if (cp >= 0x0949 && cp <= 0x094C) return SpacingMark;
      if (cp >= 0x094E && cp <= 0x094F) return SpacingMark;
      if (cp >= 0x0982 && cp <= 0x0983) return SpacingMark;
      if (cp >= 0x09BE && cp <= 0x09C0) return SpacingMark;
      if (cp >= 0x09C7 && cp <= 0x09C8) return SpacingMark;
      if (cp >= 0x09CB && cp <= 0x09CC) return SpacingMark;
      if (cp >= 0x09D7 && cp <= 0x09D7) return SpacingMark;

      // Prepend (rare)
      if (cp >= 0x0600 && cp <= 0x0605) return Prepend;
      if (cp == 0x06DD || cp == 0x070F) return Prepend;
      if (cp == 0x08E2) return Prepend;
      if (cp >= 0x110BD && cp <= 0x110BD) return Prepend;

      return Other; // Default
    }
  }
}
