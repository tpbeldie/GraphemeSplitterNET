using System;
using System.IO;
using System.Collections.Generic;

namespace GraphemeSplitterNET
{
  /// <summary>
  /// 
  /// Compact two-stage lookup table holding the grapheme break property
  /// (and the Indic_Conjunct_Break property for GB9c) of every Unicode code point.
  /// 
  /// The code point space is cut in blocks of 256 code points. Identical blocks are
  /// stored only once, so the whole 0x110000 code point space fits in ~20-40 KB
  /// instead of a 4 MB int[] or a 1M-entry Dictionary, with a true O(1) lookup:
  /// 
  ///   value = Stage2[(Stage1[cp >> 8] << 8) | (cp & 0xFF)]
  /// 
  /// Every value is one byte:
  ///   bits 0..4 = grapheme break property (Unicode.Other, Unicode.CR, ...)
  ///   bits 5..6 = InCB (None, Consonant, Extend, Linker)
  /// 
  /// </summary>
  public sealed class GraphemeTable
  {

    public const int CodePointCount = 0x110000;

    public const int PropertyMask = 0x1F;
    public const int InCBMask = 0x60;

    public const int InCB_Consonant = 0x20;
    public const int InCB_Extend = 0x40;
    public const int InCB_Linker = 0x60;

    private const int BlockShift = 8;
    private const int BlockSize = 1 << BlockShift;
    private const int BlockMask = BlockSize - 1;
    private const int Magic = 0x544E5347; // "GSNT"
    private const int FormatVersion = 1;

    private static readonly Lazy<GraphemeTable> s_embedded = new Lazy<GraphemeTable>(LoadEmbedded, isThreadSafe: true);

    // Accessed directly by the splitter loop, hence internal fields instead of properties.
    internal readonly ushort[] Stage1;
    internal readonly byte[] Stage2;

    /// <summary>
    /// The Unicode version this table was built from, such as "17.0.0".
    /// </summary>
    public string UnicodeVersion { get; }

    /// <summary>
    /// The table compiled into the assembly. No network, no files.
    /// </summary>
    public static GraphemeTable Embedded => s_embedded.Value;

    private GraphemeTable(ushort[] stage1, byte[] stage2, string unicodeVersion)
    {
      Stage1 = stage1;
      Stage2 = stage2;
      UnicodeVersion = unicodeVersion;
    }

    /// <summary>
    /// Returns the raw table value (property | InCB) of a code point.
    /// </summary>
    public int GetValue(int codePoint)
    {
      if ((uint)codePoint >= CodePointCount) return Unicode.Other;
      return Stage2[(Stage1[codePoint >> BlockShift] << BlockShift) | (codePoint & BlockMask)];
    }

    /// <summary>
    /// Returns the grapheme break property of a code point.
    /// </summary>
    public int GetProperty(int codePoint)
    {
      return GetValue(codePoint) & PropertyMask;
    }

    /// <summary>
    /// Compacts a flat one-byte-per-code-point array into a two-stage table.
    /// </summary>
    public static GraphemeTable Create(byte[] values, string unicodeVersion)
    {
      if (values == null) throw new ArgumentNullException(nameof(values));
      if (values.Length != CodePointCount) throw new ArgumentException("Expected one value per code point.", nameof(values));

      var stage1 = new ushort[CodePointCount >> BlockShift];
      var blocks = new List<byte[]>();
      var index = new Dictionary<BlockKey, ushort>();

      for (int b = 0; b < stage1.Length; b++) {
        var block = new byte[BlockSize];
        Buffer.BlockCopy(values, b << BlockShift, block, 0, BlockSize);
        var key = new BlockKey(block);
        if (!index.TryGetValue(key, out ushort id)) {
          id = checked((ushort)blocks.Count);
          index.Add(key, id);
          blocks.Add(block);
        }
        stage1[b] = id;
      }

      var stage2 = new byte[blocks.Count << BlockShift];
      for (int i = 0; i < blocks.Count; i++) {
        Buffer.BlockCopy(blocks[i], 0, stage2, i << BlockShift, BlockSize);
      }
      return new GraphemeTable(stage1, stage2, unicodeVersion ?? "unknown");
    }

    /// :::::::: Serialization :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    /// Used for the embedded table and for the on-disk cache of downloaded tables.
    /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

    public byte[] ToBytes()
    {
      using (var stream = new MemoryStream()) {
        Write(stream);
        return stream.ToArray();
      }
    }

    public void Write(Stream stream)
    {
#if NET40
      var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8);
#else
      using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
#endif
      {
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(UnicodeVersion);
        writer.Write(Stage1.Length);
        var stage1Bytes = new byte[Stage1.Length * 2];
        Buffer.BlockCopy(Stage1, 0, stage1Bytes, 0, stage1Bytes.Length);
        writer.Write(stage1Bytes);
        writer.Write(Stage2.Length);
        writer.Write(Stage2);
      }
    }

    public static GraphemeTable FromBytes(byte[] data)
    {
      using (var stream = new MemoryStream(data, writable: false)) {
        return Read(stream);
      }
    }

    public static GraphemeTable Read(Stream stream)
    {
#if NET40
      var reader = new BinaryReader(stream, System.Text.Encoding.UTF8);
#else
      using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
#endif
      {
        if (reader.ReadInt32() != Magic) throw new InvalidDataException("Not a grapheme table.");
        if (reader.ReadInt32() != FormatVersion) throw new InvalidDataException("Unsupported grapheme table format.");

        string version = reader.ReadString();

        int stage1Length = reader.ReadInt32();
        if (stage1Length != CodePointCount >> BlockShift) throw new InvalidDataException("Corrupted grapheme table.");
        var stage1Bytes = reader.ReadBytes(stage1Length * 2);
        if (stage1Bytes.Length != stage1Length * 2) throw new EndOfStreamException();
        var stage1 = new ushort[stage1Length];
        Buffer.BlockCopy(stage1Bytes, 0, stage1, 0, stage1Bytes.Length);

        int stage2Length = reader.ReadInt32();
        if (stage2Length <= 0 || (stage2Length & BlockMask) != 0) throw new InvalidDataException("Corrupted grapheme table.");
        var stage2 = reader.ReadBytes(stage2Length);
        if (stage2.Length != stage2Length) throw new EndOfStreamException();

        // Validate block indices once, so lookups never need bounds checks of their own.
        int blockCount = stage2Length >> BlockShift;
        for (int i = 0; i < stage1.Length; i++) {
          if (stage1[i] >= blockCount) throw new InvalidDataException("Corrupted grapheme table.");
        }
        return new GraphemeTable(stage1, stage2, version);
      }
    }

    private static GraphemeTable LoadEmbedded()
    {
      return FromBytes(Convert.FromBase64String(EmbeddedUnicodeData.Data));
    }

    // Value-equality wrapper so identical 256-byte blocks can be deduplicated.
    private struct BlockKey : IEquatable<BlockKey>
    {
      private readonly byte[] m_data;
      private readonly int m_hash;

      public BlockKey(byte[] data)
      {
        m_data = data;
        unchecked {
          int h = (int)2166136261;
          for (int i = 0; i < data.Length; i++) h = (h ^ data[i]) * 16777619;
          m_hash = h;
        }
      }

      public bool Equals(BlockKey other)
      {
        if (m_hash != other.m_hash) return false;
        for (int i = 0; i < m_data.Length; i++) {
          if (m_data[i] != other.m_data[i]) return false;
        }
        return true;
      }

      public override bool Equals(object obj) => obj is BlockKey other && Equals(other);

      public override int GetHashCode() => m_hash;
    }
  }
}
