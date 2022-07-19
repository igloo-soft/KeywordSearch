# KeywordSearch

**KeywordSearch** is a minimalist keyword indexer searcher.

Minimal code and tiny memory footprint comparing to a full text search engine.

Search result in milliseconds, fast enough for auto suggestion while the user is typing. 

Useful for searching e.g. enums by CamelCaseName, UI controls by caption text, objects by custom tags etc.

NuGet: https://www.nuget.org/packages/KeywordSearch

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