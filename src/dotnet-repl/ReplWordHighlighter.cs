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
                    "abstract",
                    "and",
                    "as",
                    "base",
                    "bool",
                    "byte", 
                    "char",
                    "class", 
                    "const", 
                    "decimal",
                    "default",
                    "delegate", 
                    "do",
                    "double",
                    "else",
                    "extern",
                    "false",
                    "fixed",
                    "for",
                    "foreach",
                    "global",
                    "if",
                    "in", 
                    "int",
                    "interface",
                    "internal", 
                    "let",
                    "nameof",
                    "namespace",
                    "new",
                    "not",
                    "null",
                    "or",
                    "override",
                    "private",
                    "protected",
                    "public",
                    "return",
                    "true",
                    "try",  
                    "typeof",
                    "sbyte", 
                    "single",
                    "static",
                    "string",
                    "struct",
                    "uint",
                    "void",
                    "when",
                    "while",
                    "with", 
                    "yield"
                 }),

                 // C# operators (or shared)
                 (new Style(foreground: Color.SteelBlue1_1), 
                  new [] 
                  {
                     "~",
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
                     "[", 
                     "]",
                     "{", 
                     "}", 
                     "@", 
                     "*", 
                     "\"", 
                     "/", 
                     "#", 
                     "%", 
                     "+", 
                     "<", 
                     "=",
                     ">", 
                     "|", 
                     "$"
                  })
            };

        private static readonly WordsToHighlight csharpOnlyWordsToHighlight = new[]
            {
                // C#-only keywords
                (new Style(foreground: Color.LightSlateBlue),
                 new []
                 {
                    "break", 
                    "case", 
                    "catch",
                    "checked",
                    "continue",
                    "enum", 
                    "event",
                    "explicit",
                    "foreach",
                    "goto",
                    "implicit",
                    "is",
                    "lock",
                    "long", 
                    "object", 
                    "operator", 
                    "out", 
                    "params", 
                    "readonly", 
                    "ref", 
                    "short",
                    "stackalloc", 
                    "switch", 
                    "this",
                    "ulong", 
                    "unchecked", 
                    "unsafe", 
                    "ushort", 
                    "using", 
                    "virtual",
                    "volatile",
                     
                    // Contextual keywords
                    "add",
                    "alias",
                    "ascending",
                    "async", 
                    "await", 
                    "by", 
                    "descending", 
                    "dynamic", 
                    "equals", 
                    "from", 
                    "get", 
                    "group", 
                    "init", 
                    "into", 
                    "join", 
                    "managed", 
                    "nint", 
                    "notnull", 
                    "nuint", 
                    "on", 
                    "orderby", 
                    "partial", 
                    "record",
                    "remove",
                    "select",
                    "set",
                    "unmanaged",
                    "value",
                    "var", 
                    "where", 
                 }),
            
                 // C#-only operators
                 (new Style(foreground: Color.SteelBlue1_1),
                  new []
                  {
                     "=>"
                  })
            };

        private static readonly WordsToHighlight fsharpOnlyWordsToHighlight = new[]
            {
                // F# keywords (ref. https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/keyword-reference)
                 (new Style(foreground: Color.LightSlateBlue),
                  new []
                  {
                     "and", 
                     "and!", 
                     "assert", 
                     "begin", 
                     "do!", 
                     "done", 
                     "downcast", 
                     "downto", 
                     "elif", 
                     "end", 
                     "exception",
                     "float32",
                     "fun", 
                     "function", 
                     "inherit", 
                     "int8",
                     "int16",
                     "int32",
                     "int64",
                     "inline", 
                     "lazy", 
                     "let!",
                     "match", 
                     "match!", 
                     "member", 
                     "module", 
                     "mutable", 
                     "obj", 
                     "of", 
                     "open", 
                     "rec", 
                     "return!", 
                     "then", 
                     "to", 
                     "type", 
                     "uint8",
                     "uint16",
                     "uint32",
                     "uint64",
                     "upcast", 
                     "use", 
                     "use!", 
                     "val",
                     "while!",
                     "with",
                     "yield!"
                  }),

                 // F# operators
                 (new Style(foreground: Color.SteelBlue1_1),
                  new []{
                     "**", 
                     "<>", 
                     "|>", 
                     "||>", 
                     "|||>", 
                     "<|", 
                     "<||", 
                     "<|||", 
                     "<-"
                  })
            };
    }
}
