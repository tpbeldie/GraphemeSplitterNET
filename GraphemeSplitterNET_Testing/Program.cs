using GraphemeSplitterNET;
using ST.Library.Text;
using System;
using System.Diagnostics;
using System.Text;

namespace GraphemeSplitterNET_Testing
{
  internal class Program
  {
    static void Main(string[] args)
    {
      // Prepare test input.
      StringBuilder input = new StringBuilder();
      for (int i = 0; i < 1_000_000; i++) {
        input.Append("Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍A̴̵̜̰͔ͫ͗͢L̠ͨͧͩ͘G̴̻͈͍͔̹̑͗̎̅͛́Ǫ̵̹̻̝̳͂̌̌͘!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'汉字👩‍🦰👩‍👩‍👦‍👦️‍Abc");
      }
      string testString = input.ToString();

      // Benchmark GraphemeSplitter.
      var splitter = new GraphemeSplitter();
      var sw = Stopwatch.StartNew();
      var clusters1 = splitter.Split(testString);
      sw.Stop();
      long time1 = sw.ElapsedMilliseconds;

      // Benchmark GraphemeSplitterBuffered.
      var splitterBuffered = new GraphemeSplitterBuffered();
      sw.Restart();
      var clusters2 = splitterBuffered.Split(testString);
      sw.Stop();
      long time2 = sw.ElapsedMilliseconds;

      // Benchmark STGraphemeSplitter (No Cache).
      STGraphemeSplitter.ClearCache();
      sw.Restart();
      var clusters3_nocache = STGraphemeSplitter.Split(testString);
      sw.Stop();
      long time3_nocache = sw.ElapsedMilliseconds;

      // Benchmark STGraphemeSplitter (Dictionary Cache).
      STGraphemeSplitter.CreateDictionaryCache(); 
      sw.Restart();
      var clusters3 = STGraphemeSplitter.Split(testString);
      sw.Stop();
      long time3 = sw.ElapsedMilliseconds;

      // Benchmark STGraphemeSplitter (Array Cache).
      STGraphemeSplitter.CreateArrayCache();
      sw.Restart();
      var clusters4 = STGraphemeSplitter.Split(testString);
      sw.Stop();
      long time3_array = sw.ElapsedMilliseconds;

      // Benchmark NextBreak (Streaming/Cursor approach) - Using Buffered
      sw.Restart();
      int countNextBreak = 0;
      int idx = 0;
      while (idx < testString.Length) {
        idx = splitterBuffered.NextBreak(testString, idx);
        countNextBreak++;
      }
      sw.Stop();
      long time4 = sw.ElapsedMilliseconds;

      // Benchmark GetBreaks (Bulk index retrieval) - Using Buffered
      sw.Restart();
      var breaks = splitterBuffered.GetBreaks(testString);
      sw.Stop();
      long time5 = sw.ElapsedMilliseconds;

      Debug.WriteLine($"GraphemeSplitter: {clusters1.Count} clusters in {time1}ms");
      Debug.WriteLine($"GraphemeSplitterBuffered: {clusters2.Count} clusters in {time2}ms");
      Debug.WriteLine($"STGraphemeSplitter (No Cache): {clusters3_nocache.Count} clusters in {time3_nocache}ms");
      Debug.WriteLine($"STGraphemeSplitter (Dict): {clusters3.Count} clusters in {time3}ms");
      Debug.WriteLine($"STGraphemeSplitter (Array): {clusters4.Count} clusters in {time3_array}ms");
      Debug.WriteLine($"NextBreak (Buffered Iteration): {countNextBreak} clusters in {time4}ms");
      Debug.WriteLine($"GetBreaks (Buffered Indices): {breaks.Count} clusters in {time5}ms");
      Debug.WriteLine($"Input length: {input.Length}");

      // Show first few clusters from each splitter to verify correctness.
      Debug.WriteLine("First 20 grapheme clusters (GraphemeSplitter):");
      for (int i = 0; i < Math.Min(20, clusters1.Count); i++) {
        Debug.WriteLine($"'{clusters1[i]}'");
      }
      Debug.WriteLine("First 20 grapheme clusters (GraphemeSplitterBuffered):");
      for (int i = 0; i < Math.Min(20, clusters2.Count); i++) {
        Debug.WriteLine($"'{clusters2[i]}'");
      }
      Debug.WriteLine("First 20 grapheme clusters (STGraphemeSplitter):");
      for (int i = 0; i < Math.Min(20, clusters3.Count); i++) {
        Debug.WriteLine($"'{clusters3[i]}'");
      }

      Console.WriteLine("Benchmark complete. See Output window for details.");
      Console.ReadLine();
    }
  }
}
