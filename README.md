# KeywordSearch

A simple to use keyword indexer searcher with a tiny memory footprint.

Gives search result in milliseconds. Fast enough for auto suggestion while the user is typing. 

Useful for searching CamelCaseNames, finding UI controls by keywords in caption text, and looking up objects by custom tags.

NuGet: [https://www.nuget.org/packages/KeywordSearch](https://www.nuget.org/packages/KeywordSearch)  
GitHub: [https://github.com/igloo-soft/KeywordSearch](https://github.com/igloo-soft/KeywordSearch)

```csharp
var index = new KeywordSearch<KnownColor>();

// Map each KnownColor to its name: E.g. LightGoldenrodYellow
var phrases = Enum.GetValues<KnownColor>()
  .Select(color => (Item: color, Phrase: color.ToString()));

// By default, CamelCase phrases are tokenized into keywords.
index.AddPhrases(phrases);

// Search for "gol"
// result = { Gold, Goldenrod, DarkGoldenrod, LightGoldenrodYellow, PaleGoldenrod }
var result = index.Search("gol");
```