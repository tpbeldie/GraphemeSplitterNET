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
        input.Append("Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍A̴̵̜̰͔ͫ͗͢L̠ͨͧͩ͘G̴̻͈͍͔̹̑͗̎̅͛́Ǫ̵̹̻̝̳͂̌̌͘!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'汉字👩‍🦰👩‍👩‍👦‍👦🏳️‍🌈Abc");
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

      // Benchmark STGraphemeSplitter.
      sw.Restart();
      var clusters3 = STGraphemeSplitter.Split(testString);
      sw.Stop();
      long time3 = sw.ElapsedMilliseconds;

      Debug.WriteLine($"GraphemeSplitter: {clusters1.Count} clusters in {time1}ms");
      Debug.WriteLine($"GraphemeSplitterBuffered: {clusters2.Count} clusters in {time2}ms");
      Debug.WriteLine($"STGraphemeSplitter: {clusters3.Count} clusters in {time3}ms");
      Debug.WriteLine($"Input length: {input.Length}");

      // Show first few clusters from each splitter to verify correctness.
      Debug.WriteLine("First 10 grapheme clusters (GraphemeSplitter):");
      for (int i = 0; i < Math.Min(10, clusters1.Count); i++) {
        Debug.WriteLine($"'{clusters1[i]}'");
      }
      Debug.WriteLine("First 10 grapheme clusters (GraphemeSplitterBuffered):");
      for (int i = 0; i < Math.Min(10, clusters2.Count); i++) {
        Debug.WriteLine($"'{clusters2[i]}'");
      }
      Debug.WriteLine("First 10 grapheme clusters (STGraphemeSplitter):");
      for (int i = 0; i < Math.Min(10, clusters3.Count); i++) {
        Debug.WriteLine($"'{clusters3[i]}'");
      }

      Console.WriteLine("Benchmark complete. See Output window for details.");
      Console.ReadLine();
    }
  }
}
