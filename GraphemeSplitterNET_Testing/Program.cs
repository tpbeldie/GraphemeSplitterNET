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
      int iterations = 100_000;
      if (args.Length > 0 && int.TryParse(args[0], out int customIter)) {
        iterations = customIter;
      }

      Console.WriteLine($"Building test string with {iterations} iterations...");
      // Prepare test input.
      StringBuilder input = new StringBuilder();
      for (int i = 0; i < iterations; i++) {
        input.Append("Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍A̴̵̜̰͔ͫ͗͢L̠ͨͧͩ͘G̴̻͈͍͔̹̑͗̎̅͛́Ǫ̵̹̻̝̳͂̌̌͘!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'汉字👩‍🦰👩‍👩‍👦‍👦️‍Abc");
      }
      string testString = input.ToString();
      Console.WriteLine($"Input length: {testString.Length:N0} characters.");
      Console.WriteLine("Running benchmarks...");

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

      Console.WriteLine();
      Console.WriteLine("------------------- Benchmark Results -------------------");
      Console.WriteLine($"GraphemeSplitter (Split):           {clusters1.Count:N0} clusters in {time1}ms");
      Console.WriteLine($"GraphemeSplitterBuffered (Split):   {clusters2.Count:N0} clusters in {time2}ms");
      Console.WriteLine($"GraphemeSplitter (NextBreak):       {countNextBreak:N0} clusters in {time4}ms");
      Console.WriteLine($"GraphemeSplitter (GetBreaks):       {breaks.Count:N0} clusters in {time5}ms");
      Console.WriteLine($"STGraphemeSplitter (Dict Cache):    {clusters3.Count:N0} clusters in {time3}ms");
      Console.WriteLine($"STGraphemeSplitter (Array Cache):   {clusters4.Count:N0} clusters in {time3_array}ms");
      Console.WriteLine($"STGraphemeSplitter (No Cache):      {clusters3_nocache.Count:N0} clusters in {time3_nocache}ms");
      Console.WriteLine("---------------------------------------------------------");
      Console.WriteLine();

      Console.WriteLine("First 10 clusters (GraphemeSplitter):");
      for (int i = 0; i < Math.Min(10, clusters1.Count); i++) {
        Console.WriteLine($"  [{i}] '{clusters1[i]}'");
      }

      if (!Console.IsInputRedirected && args.Length == 0) {
        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
      }
    }
  }
}
