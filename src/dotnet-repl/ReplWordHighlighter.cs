using RadLine;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    internal static class ReplWordHighlighter
    {
        public static WordHighlighter Create()
        {
            var wordHighlighter = new WordHighlighter();

            var keywordStyle = new Style(foreground: Color.LightSlateBlue);
            var operatorStyle = new Style(foreground: Color.SteelBlue1_1);

            var keywords = new[]
            {
                "async",
                "await",
                "bool",
                "break",
                "case",
                "catch",
                "class",
                "else",
                "finally",
                "for",
                "foreach",
                "if",
                "in",
                "int",
                "interface",
                "internal",
                "let",
                "match",
                "member",
                "mutable",
                "new",
                "not",
                "null",
                "open",
                "override",
                "private",
                "protected",
                "public",
                "record",
                "typeof",
                "return",
                "string",
                "struct",
                "switch",
                "then",
                "try",
                "type",
                "use",
                "using",
                "var",
                "void",
                "when",
                "while",
                "with",
            };

            var operatorsAndPunctuation = new[]
            {
                "_",
                "-",
                "->",
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
                "|>",
                "$",
            };

            foreach (var keyword in keywords)
            {
                wordHighlighter.AddWord(keyword, keywordStyle);
            }

            foreach (var op in operatorsAndPunctuation)
            {
                wordHighlighter.AddWord(op, operatorStyle);
            }

            return wordHighlighter;
        }
    }
}