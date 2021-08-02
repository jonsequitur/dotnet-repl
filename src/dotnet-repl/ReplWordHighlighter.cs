using RadLine;
using Spectre.Console;
using System.Collections.Generic;

namespace dotnet_repl
{
    using WordsToHighlight = IEnumerable<(Style style, string[] words)>;

    internal static class ReplWordHighlighter
    {
        private static void AddWords(this WordHighlighter highlighter, WordsToHighlight wordsToHighlight)
        {
            foreach (var (style, wordsToAdd) in wordsToHighlight)
                foreach (var word in wordsToAdd)
                    highlighter.AddWord(word, style);
        }

        public static WordHighlighter Create(string languageName)
        {
            var wordHighlighter = new WordHighlighter();

            wordHighlighter.AddWords(sharedWordsToHighlight);

            if (languageName is "csharp")
                wordHighlighter.AddWords(csharpOnlyWordsToHighlight);
            else if (languageName is "fsharp")
                wordHighlighter.AddWords(fsharpOnlyWordsToHighlight);
            
            // Since it also supports powershell, more keywords might be added by adding a case for "pwsh"

            return wordHighlighter;
        }


        private static readonly WordsToHighlight sharedWordsToHighlight = new[]
            {
                
                (new Style(foreground: Color.LightSlateBlue),
                 new []
                 {
                    "and",
                    "bool",
                    "do",
                    "else",
                    "for",
                    "foreach",
                    "if",
                    "int",
                    "not",
                    "null",
                    "or",
                    "private",
                    "protected",
                    "public",
                    "typeof",
                    "string",
                    "when",
                    "with"
                 }),

                 // C# operators (or shared)
                 (new Style(foreground: Color.SteelBlue1_1), 
                  new [] 
                  {
                     "_",
                     "-", 
                     ";", 
                     ":", 
                     "!", 
                     "?", 
                     ".", 
                     "'", 
                     "(", 
                     ")",
                     "{", 
                     "}", 
                     "@", 
                     "*", 
                     "\"", 
                     "#", 
                     "%", 
                     "+", 
                     "<", 
                     "=",
                     "=>", 
                     ">", 
                     "|", 
                     "$"
                  }),
            };

        private static readonly WordsToHighlight csharpOnlyWordsToHighlight = new[]
            {
                // C#-only keywords
                (new Style(foreground: Color.LightSlateBlue),
                 new []
                 {
                    "async", 
                    "await", 
                    "break", 
                    "case", 
                    "catch",
                    "class", 
                    "else", 
                    "for", 
                    "foreach", 
                    "if",
                    "in", 
                    "interface", 
                    "internal", 
                    "override", 
                    "or", 
                    "return", 
                    "record",
                    "switch", 
                    "try",  
                    "using", 
                    "var", 
                    "void",
                    "while"
                 }),

                 // C#-only operators
                 (new Style(foreground: Color.SteelBlue1_1),
                  new []
                  {
                     ";"
                  }),
            };

        private static readonly WordsToHighlight fsharpOnlyWordsToHighlight = new[]
            {
                // F# keywords (ref. https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/keyword-reference)
                 (new Style(foreground: Color.LightSlateBlue),
                  new []
                  {
                     "elif", 
                     "fun", 
                     "function", 
                     "inline", 
                     "lazy", 
                     "let",
                     "match", 
                     "member", 
                     "mutable", 
                     "of", 
                     "open", 
                     "rec", 
                     "then", 
                     "to", 
                     "type", 
                     "val", 
                     "with", 
                     "yield"
                  }),

                 // F# operators
                 (new Style(foreground: Color.SteelBlue1_1),
                  new []{
                     "|>", 
                     "->", 
                     "<-"
                  })
            };
    }
}
