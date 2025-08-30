using GraphemeSplitterNET;
using System;
using System.Diagnostics;
using System.Text;

namespace GraphemeSplitterNET_Testing
{
  internal class Program
  {
    static void Main(string[] args)
    {


      StringBuilder input = new StringBuilder();
      for (int i = 0; i < 1_000_000; i++) {
        input.Append("汉字👩‍🦰👩‍👩‍👦‍👦🏳️‍🌈Abc");
      }

      var x = new GraphemeSplitter();
      var sw = Stopwatch.StartNew();
      var clusters = x.Split(input.ToString());
      sw.Stop();

      Debug.WriteLine($"Grapheme clusters ({clusters.Count}) processed in {sw.ElapsedMilliseconds}ms");
      Debug.WriteLine($"Input length: {input.Length}");

      // Show first few clusters to verify correctness
      Debug.WriteLine("First 10 grapheme clusters:");
      for (int i = 0; i < Math.Min(10, clusters.Count); i++) {
        Debug.WriteLine($"'{clusters[i]}'");
      }

      Console.ReadLine();
    }
  }
}
