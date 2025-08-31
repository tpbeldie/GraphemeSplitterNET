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
    /// Determines the grapheme break property for a Unicode code point according to UAX #29.
    /// https://unicode.org/reports/tr29/
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

      /// ::::::: Special characters (GB3 - GB5) :::::::::::::::::::::::::::::::::::::::::::::::::
      /// These have the highest priority and are checked first for performance.
      /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // CR (Carriage Return) - Used in line break sequences.

      if (cp == 0x000D) return CR;

      // LF (Line Feed) - Used in line break sequences.

      if (cp == 0x000A) return LF;

      // ZWJ (Zero Width Joiner) - Critical for emoji sequences like family emojis.
      // This character joins separate emojis into a single grapheme cluster.

      if (cp == 0x200D) return ZWJ;

      /// ::::::: Control characters (GB4 - GB5) ::::::::::::::::::::::::::::::::::::::::::::::::
      /// Control characters force grapheme breaks before and after them.
      /// :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // ASCII control characters (C0 and C1 control blocks).

      if (cp <= 0x001F || (cp >= 0x007F && cp <= 0x009F)) return Control;

      // Unicode format control characters.

      if (cp >= 0x200C && cp <= 0x200F) return Control; // ZWNJ, ZWJ, LRM, RLM, etc.
      if (cp >= 0x2028 && cp <= 0x202E) return Control; // Line/paragraph separators, bidi controls.
      if (cp >= 0x2060 && cp <= 0x2064) return Control; // Word joiner, invisible operators.
      if (cp >= 0x2066 && cp <= 0x206F) return Control; // Bidi isolates and other format chars.

      // Byte Order Mark when used as format character.

      if (cp == 0xFEFF) return Control;

      /// :::::::::: Extend characters (GB9) (combining marks, etc.) ::::::::::::::::::::::
      /// These characters extend the previous grapheme cluster and cannot start a new one.
      /// Includes combining marks, diacritics, and other modifying characters.
      /// :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // Latin combining diacritical marks (most common).
      // Examples: é (e + ́), ñ (n + ̃), ü (u + ̈)

      if (cp >= 0x0300 && cp <= 0x036F) return Extend;

      // Cyrillic combining characters.
      // Used for Old Church Slavonic and other historical Cyrillic texts.

      if (cp >= 0x0483 && cp <= 0x0489) return Extend;

      // Hebrew accent marks and points.
      // Critical for proper Hebrew text rendering with vowel points and cantillation.

      if (cp >= 0x0591 && cp <= 0x05BD) return Extend;
      if (cp >= 0x05BF && cp <= 0x05C7) return Extend;

      // Arabic combining marks and diacritics.
      // Essential for proper Arabic script rendering with vowel marks (harakat).

      if (cp >= 0x0610 && cp <= 0x061A) return Extend; // Arabic marks.
      if (cp >= 0x064B && cp <= 0x065F) return Extend; // Arabic diacritics (fatha, kasra, etc.).
      if (cp >= 0x0670 && cp <= 0x0670) return Extend; // Arabic letter superscript alef.
      if (cp >= 0x06D6 && cp <= 0x06DC) return Extend; // Arabic small marks.
      if (cp >= 0x06DF && cp <= 0x06E4) return Extend; // Arabic combining marks.
      if (cp >= 0x06E7 && cp <= 0x06E8) return Extend; // Arabic combining marks.
      if (cp >= 0x06EA && cp <= 0x06ED) return Extend; // Arabic combining marks.

      // Syriac combining marks.

      if (cp >= 0x0711 && cp <= 0x0711) return Extend;
      if (cp >= 0x0730 && cp <= 0x074A) return Extend;

      // Thaana combining marks (Dhivehi script).

      if (cp >= 0x07A6 && cp <= 0x07B0) return Extend;
      if (cp >= 0x07EB && cp <= 0x07F3) return Extend;

      // Samaritan combining marks.

      if (cp >= 0x0816 && cp <= 0x0819) return Extend;
      if (cp >= 0x081B && cp <= 0x0823) return Extend;
      if (cp >= 0x0825 && cp <= 0x0827) return Extend;
      if (cp >= 0x0829 && cp <= 0x082D) return Extend;

      // Mandaic combining marks.

      if (cp >= 0x0859 && cp <= 0x085B) return Extend;

      // Arabic extended combining marks.

      if (cp >= 0x08D4 && cp <= 0x08E1) return Extend;
      if (cp >= 0x08E3 && cp <= 0x0903) return Extend;

      // Devanagari and related Indic script combining marks.
      // Critical for proper rendering of Sanskrit, Hindi, and other Indic languages.

      if (cp >= 0x093A && cp <= 0x093C) return Extend; // Devanagari combining marks.
      if (cp >= 0x093E && cp <= 0x094F) return Extend; // Devanagari vowel signs and virama.
      if (cp >= 0x0951 && cp <= 0x0957) return Extend; // Devanagari stress marks.
      if (cp >= 0x0962 && cp <= 0x0963) return Extend; // Devanagari vowel marks.

      // Bengali combining marks.

      if (cp >= 0x0981 && cp <= 0x0983) return Extend;
      if (cp >= 0x09BC && cp <= 0x09BC) return Extend; // Bengali nukta.
      if (cp >= 0x09BE && cp <= 0x09C4) return Extend; // Bengali vowel signs.
      if (cp >= 0x09C7 && cp <= 0x09C8) return Extend; // Bengali vowel signs.
      if (cp >= 0x09CB && cp <= 0x09CD) return Extend; // Bengali vowel signs and virama.
      if (cp >= 0x09D7 && cp <= 0x09D7) return Extend; // Bengali au length mark.
      if (cp >= 0x09E2 && cp <= 0x09E3) return Extend; // Bengali vowel marks.

      // Gurmukhi combining marks (Punjabi script).

      if (cp >= 0x0A01 && cp <= 0x0A03) return Extend;
      if (cp >= 0x0A3C && cp <= 0x0A3C) return Extend; // Gurmukhi nukta.
      if (cp >= 0x0A3E && cp <= 0x0A42) return Extend; // Gurmukhi vowel signs.
      if (cp >= 0x0A47 && cp <= 0x0A48) return Extend; // Gurmukhi vowel signs.
      if (cp >= 0x0A4B && cp <= 0x0A4D) return Extend; // Gurmukhi vowel signs and virama.
      if (cp >= 0x0A51 && cp <= 0x0A51) return Extend; // Gurmukhi udaat.
      if (cp >= 0x0A70 && cp <= 0x0A71) return Extend; // Gurmukhi tippi and addak.
      if (cp >= 0x0A75 && cp <= 0x0A75) return Extend; // Gurmukhi sign yakash.

      // Gujarati combining marks.

      if (cp >= 0x0A81 && cp <= 0x0A83) return Extend;
      if (cp >= 0x0ABC && cp <= 0x0ABC) return Extend; // Gujarati nukta.
      if (cp >= 0x0ABE && cp <= 0x0AC5) return Extend; // Gujarati vowel signs.
      if (cp >= 0x0AC7 && cp <= 0x0AC9) return Extend; // Gujarati vowel signs.
      if (cp >= 0x0ACB && cp <= 0x0ACD) return Extend; // Gujarati vowel signs and virama.
      if (cp >= 0x0AE2 && cp <= 0x0AE3) return Extend; // Gujarati vowel marks.
      if (cp >= 0x0AFA && cp <= 0x0AFF) return Extend; // Gujarati combining marks.

      /// :::::: Regional Indicators (flag sequences) (GB12-GB13) ::::::::::::::::::::::::::
      /// Flag emoji sequences - two regional indicators combine to form country flags.
      /// Example: 🇺🇸 = U+1F1FA (🇺) + U+1F1F8 (🇸).
      /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      if (cp >= 0x1F1E6 && cp <= 0x1F1FF) return Regional_Indicator;

      /// :::::::: Extended Pictographic characters (GB11) (emojis) ::::::::::::::::::::::::
      /// Emoji characters that can be part of ZWJ sequences.
      /// These ranges cover the vast majority of emoji characters.
      /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // Miscellaneous Symbols and Pictographs block.
      // Contains weather, zodiac, geometric shapes, and various symbols.

      if (cp >= 0x1F300 && cp <= 0x1F5FF) return Extended_Pictographic;

      // Emoticons block - faces and hand gestures.
      // Contains the most commonly used emoji: 😀😁😂🤣😃😄😅😆😉etc.

      if (cp >= 0x1F600 && cp <= 0x1F64F) return Extended_Pictographic;

      // Transport and Map Symbols block.
      // Contains vehicles, buildings, and map-related emoji.

      if (cp >= 0x1F680 && cp <= 0x1F6FF) return Extended_Pictographic;

      // Alchemical Symbols block.

      if (cp >= 0x1F700 && cp <= 0x1F77F) return Extended_Pictographic;

      // Geometric Shapes Extended block.

      if (cp >= 0x1F780 && cp <= 0x1F7FF) return Extended_Pictographic;

      // Supplemental Arrows-C block.

      if (cp >= 0x1F800 && cp <= 0x1F8FF) return Extended_Pictographic;

      // Supplemental Symbols and Pictographs block.
      // Contains many newer emoji including people, food, activities.

      if (cp >= 0x1F900 && cp <= 0x1F9FF) return Extended_Pictographic;

      // Chess Symbols block.

      if (cp >= 0x1FA00 && cp <= 0x1FA6F) return Extended_Pictographic;

      // Symbols and Pictographs Extended-A block.
      // Contains newest emoji additions.

      if (cp >= 0x1FA70 && cp <= 0x1FAFF) return Extended_Pictographic;

      // Miscellaneous Symbols block (legacy emoji range).
      // Contains older symbols that are now treated as emoji.

      if (cp >= 0x2600 && cp <= 0x26FF) return Extended_Pictographic;

      // Dingbats block (legacy emoji range).
      // Contains various symbols and shapes now used as emoji.

      if (cp >= 0x2700 && cp <= 0x27BF) return Extended_Pictographic;

      // Individual emoji characters outside the main blocks.
      // These are scattered throughout Unicode and need individual handling.

      if (cp == 0x00A9 || cp == 0x00AE) return Extended_Pictographic;   // Copyright, Registered [©️®️]
      if (cp >= 0x2122 && cp <= 0x2122) return Extended_Pictographic;   // Trademark [™️]
      if (cp >= 0x2194 && cp <= 0x2199) return Extended_Pictographic;   // Arrows [↔️↕️↖️↗️↘️↙️]
      if (cp >= 0x21A9 && cp <= 0x21AA) return Extended_Pictographic;   // Arrows [↩️↪️]
      if (cp >= 0x231A && cp <= 0x231B) return Extended_Pictographic;   // Watch, Hourglass [⌚⌛]
      if (cp >= 0x2328 && cp <= 0x2328) return Extended_Pictographic;   // Keyboard [⌨️]
      if (cp >= 0x23CF && cp <= 0x23CF) return Extended_Pictographic;   // Eject [⏏️]
      if (cp >= 0x23E9 && cp <= 0x23F3) return Extended_Pictographic;   // Media controls [⏩⏪⏫⏬⏭️⏮️⏯️⏰⏱️⏲️⏳]
      if (cp >= 0x23F8 && cp <= 0x23FA) return Extended_Pictographic;   // Media controls [⏸️⏹️⏺️]
      if (cp >= 0x24C2 && cp <= 0x24C2) return Extended_Pictographic;   // Circled M [Ⓜ️]
      if (cp >= 0x25AA && cp <= 0x25AB) return Extended_Pictographic;   // Squares [▪️▫️]
      if (cp >= 0x25B6 && cp <= 0x25B6) return Extended_Pictographic;   // Play [️▶️]
      if (cp >= 0x25C0 && cp <= 0x25C0) return Extended_Pictographic;   // Reverse play [️◀️]
      if (cp >= 0x25FB && cp <= 0x25FE) return Extended_Pictographic;   // Squares [◻️◼️◽◾]

      /// ::::::: Hangul Characters (GB6 - GB8) ::::::::::::::::::::::::::::::::::::::::::::::
      /// Korean Hangul has complex composition rules requiring special grapheme properties.
      /// Hangul syllables are composed of L(eading), V(owel), and T(railing) components.
      /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // Hangul Jamo Leading consonants (초성).
      // Modern Hangul leading consonants: ㄱ ㄴ ㄷ ㄹ ㅁ ㅂ ㅅ ㅇ ㅈ ㅊ ㅋ ㅌ ㅍ ㅎ etc.

      if (cp >= 0x1100 && cp <= 0x115F) return L;

      // Hangul Jamo Extended-A Leading consonants.
      // Additional archaic Hangul leading consonants.

      if (cp >= 0xA960 && cp <= 0xA97C) return L;

      // Hangul Jamo Vowels (중성).
      // Hangul vowel components: ㅏ ㅑ ㅓ ㅕ ㅗ ㅛ ㅜ ㅠ ㅡ ㅣ etc.

      if (cp >= 0x1160 && cp <= 0x11A7) return V;

      // Hangul Jamo Extended-B Vowels.
      // Additional archaic Hangul vowels.

      if (cp >= 0xD7B0 && cp <= 0xD7C6) return V;

      // Hangul Jamo Trailing consonants (종성).
      // Hangul final consonants: ㄱ ㄴ ㄷ ㄹ ㅁ ㅂ ㅅ ㅇ ㅈ ㅊ ㅋ ㅌ ㅍ ㅎ etc.

      if (cp >= 0x11A8 && cp <= 0x11FF) return T;

      // Hangul Jamo Extended-B Trailing consonants.
      // Additional archaic Hangul trailing consonants.

      if (cp >= 0xD7CB && cp <= 0xD7FB) return T;

      // Precomposed Hangul Syllables block (가-힣).
      // Contains all possible modern Korean syllables (11,172 syllables).
      // Each syllable encodes its L, V, T components mathematically.

      if (cp >= 0xAC00 && cp <= 0xD7A3) {

        // Calculate the composition: syllable = L×588 + V×28 + T + 0xAC00
        int syllableIndex = cp - 0xAC00;
        int tIndex = syllableIndex % 28; // Extract the trailing consonant index.

        // LV: Syllables without trailing consonant (e.g., 가, 나, 다)
        // LVT: Syllables with trailing consonant (e.g., 각, 난, 달)
        return (tIndex == 0) ? LV : LVT;

      }

      /// ::::::: SpacingMark (basic cases) :::::::::::::::::::::::::::::::::::::::::::::
      /// Characters that extend the previous cluster but also consume space.
      /// These are treated specially to prevent breaks before them.
      /// :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // Devanagari spacing combining marks.
      // Used in Sanskrit, Hindi, Marathi, and other Indic languages.

      if (cp >= 0x0903 && cp <= 0x0903) return SpacingMark; // Devanagari sign visarga.
      if (cp >= 0x093B && cp <= 0x093B) return SpacingMark; // Devanagari vowel sign ooe.
      if (cp >= 0x093E && cp <= 0x0940) return SpacingMark; // Devanagari vowel signs aa, i, ii.
      if (cp >= 0x0949 && cp <= 0x094C) return SpacingMark; // Devanagari vowel signs o, au, etc.
      if (cp >= 0x094E && cp <= 0x094F) return SpacingMark; // Devanagari prishthamatra e.

      // Bengali spacing combining marks.

      if (cp >= 0x0982 && cp <= 0x0983) return SpacingMark; // Bengali anusvara and visarga.
      if (cp >= 0x09BE && cp <= 0x09C0) return SpacingMark; // Bengali vowel signs.
      if (cp >= 0x09C7 && cp <= 0x09C8) return SpacingMark; // Bengali vowel signs e, ai.
      if (cp >= 0x09CB && cp <= 0x09CC) return SpacingMark; // Bengali vowel signs o, au.
      if (cp >= 0x09D7 && cp <= 0x09D7) return SpacingMark; // Bengali au length mark.

      /// ::::::: Prepend characters (GB9b) :::::::::::::::::::::::::::::::::::: 
      /// Characters that must not be separated from the following character.
      /// Relatively rare but important for certain scripts and formatting.
      /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      // Arabic number signs and formatting characters.
      // These modify the following text and must not be separated from it.

      if (cp >= 0x0600 && cp <= 0x0605) return Prepend; // Arabic number signs.
      if (cp == 0x06DD || cp == 0x070F) return Prepend; // Arabic end of ayah, Syriac abbreviation.
      if (cp == 0x08E2) return Prepend;                 // Arabic disputed end of ayah.

      // Kaithi number sign (historical Indic script).

      if (cp >= 0x110BD && cp <= 0x110BD) return Prepend;

      /// ::::::: Default :::::::::::::::::::::::::::::::::::::::::::::::::::::::::
      /// All other characters that don't require special grapheme break handling.
      /// This includes most letters, digits, punctuation, and symbols.
      /// :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

      return Other;
    }
  }
}
