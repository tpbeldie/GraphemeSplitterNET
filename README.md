### .NET Standard 2.0 implementation of the Unicode grapheme cluster breaking algorithm
There is no point saying the same shit. You can read [this](https://www.codeproject.com/Tips/5317106/Split-grapheme-in-Csharp) article written by my friend [DebugST](https://github.com/DebugST/) for our previous grapheme cluster breaking project [STGraphemeSplitter](https://github.com/DebugST/STGraphemeSplitter)

- This project is its new version. Faster and lighter with minimal code and two different variants where you can chose or extend from.
- **GraphemeSplitterBuffered**: The recommended implementation using O(1) lookups and binary caching. Now supports `NextBreak` (cursor iteration) and `GetBreaks` (bulk index retrieval) for zero-allocation performance.

See GraphemeSplitterNET_Test or the project STGRaphemeSplitter (same behaviour approach .Split .Each) for usage. 

# Benchmark Results

## Performance

- **GraphemeSplitterBuffered (GetBreaks)**: 14,000,000 clusters in **3123ms** (Indices only)
- **GraphemeSplitterBuffered (Split)**: 14,000,000 clusters in **3908ms**
- **GraphemeSplitterBuffered (NextBreak)**: 14,000,000 clusters in **3870ms** (Iteration)
- **GraphemeSplitter**: 15,000,000 clusters in **5479ms**
- **STGraphemeSplitter (Dict)**: 14,000,000 clusters in **6333ms**
- **STGraphemeSplitter (Array)**: 14,000,000 clusters in **9832ms**
- **STGraphemeSplitter (No Cache)**: 14,000,000 clusters in **21434ms**

**Input length:** 98,000,000  

---

## INPUT = Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍A̴̵̜̰͔ͫ͗͢L̠ͨͧͩ͘G̴̻͈͍͔̹̑͗̎̅͛́Ǫ̵̹̻̝̳͂̌̌͘!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'汉字👩‍🦰👩‍👩‍👦‍👦Abc `* 1 000 000`

## OUTPUT = First 20 Grapheme Clusters

# GraphemeSplitter:
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
'!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'
'''
'汉'
'字'
'👩‍🦰'
'👩‍👩‍👦‍👦'
'️‍'
'A'
'b'
'c'
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
# GraphemeSplitterBuffered:
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
'!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'
'''
'汉'
'字'
'👩‍🦰'
'👩‍👩‍👦‍👦️‍'
'A'
'b'
'c'
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
'!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'
# STGraphemeSplitter (no cache):
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
'!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'
'''
'汉'
'字'
'👩‍🦰'
'👩‍👩‍👦‍👦️‍'
'A'
'b'
'c'
'Z͑ͫ̓ͪ̂ͫ̽͏̴̙̤̞͉͚̯̞̠͍'
'A̴̵̜̰͔ͫ͗͢'
'L̠ͨͧͩ͘'
'G̴̻͈͍͔̹̑͗̎̅͛́'
'Ǫ̵̹̻̝̳͂̌̌͘'
'!͖̬̰̙̗̿̋ͥͥ̂ͣ̐́́͜͞'
