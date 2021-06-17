using System;
using System.Collections.Generic;
using System.Linq;
using RadLine;

namespace dotnet_repl.LineEditorCommands
{
    public sealed class CompletionCommand : LineEditorCommand
    {
        private const string QUERY_STATE = "AUTOCOMPLETE_QUERY";
        private const string RESULT_STATE = "AUTOCOMPLETE_RESULTS";
        private const string INDEX_STATE = "AUTOCOMPELTE_INDEX";
        private const string CONTEXT_STATE = "AUTOCOMPELTE_CONTEXT";

        private readonly AutoComplete _kind;

        public CompletionCommand(AutoComplete kind)
        {
            _kind = kind;
        }

        public override void Execute(LineEditorContext context)
        {
            if (context.GetService(typeof(KernelCompletion)) is not KernelCompletion service)
            {
                throw new InvalidOperationException("AutoCompletion service has not been registered.");
            }

            // Find the word boundary
            var prefix = GetPrefixAtWordBoundary(context);
            var wordEnd = GetSuffixPosition(context);

            // Get the previous query
            var previousQuery = context.GetState<string?>(QUERY_STATE, () => null);
            var query = context.Buffer.Content[..wordEnd];
            if (previousQuery is { })
            {
                var previousAutoCompleteValue = context.GetState<string?>(CONTEXT_STATE, () => null);
                if (previousAutoCompleteValue != null && context.Buffer.Content == previousAutoCompleteValue)
                {
                    query = context.Buffer.Content[..Math.Min(wordEnd, previousQuery.Length)];
                }
            }

            // Query changed?
            var index = context.GetState(INDEX_STATE, () => 0);
            if (previousQuery == null || !query.Equals(previousQuery, StringComparison.OrdinalIgnoreCase))
            {
                // Fetch completions
                var completions = service.GetCompletions(context.Buffer).ToList();
                if (completions == null)
                {
                    return;
                }

                context.SetState(INDEX_STATE, 0);
                context.SetState(QUERY_STATE, context.Buffer.Content[..wordEnd]);
                context.SetState(RESULT_STATE, completions);
            }

            // Get the results
            var result = context.GetState(RESULT_STATE, () => new List<string>());
            if (result.Count == 0)
            {
                return;
            }

            // Clamp the results
            if (index > result.Count - 1)
            {
                index = 0;
            }

            if (index < 0)
            {
                index = result.Count - 1;
            }

            // Insert the autocomplete word
            context.Buffer.Clear(prefix.Length, wordEnd - prefix.Length);
            context.Buffer.Move(prefix.Length);
            context.Buffer.Insert(result[index]);
            context.Buffer.Move(prefix.Length + result[index].Length);

            context.SetState(CONTEXT_STATE, context.Buffer.Content);

            // Increase/Decrease index
            context.SetState(INDEX_STATE, _kind == AutoComplete.Next ? ++index : --index);
        }

        private static string GetPrefixAtWordBoundary(LineEditorContext context)
        {
            for (var pos = context.Buffer.CursorPosition - 1; pos > 0; pos--)
            {
                var current = context.Buffer.Content[pos];
                if (current == '.' || char.IsWhiteSpace(current))
                {
                    return context.Buffer.Content.Substring(0, pos + 1);
                }
            }

            return string.Empty;
        }

        private static int GetSuffixPosition(LineEditorContext context)
        {
            var start = Math.Max(context.Buffer.CursorPosition - 1, 0);
            var length = context.Buffer.Length;
            for (var pos = start; pos < length; pos++)
            {
                var current = context.Buffer.Content[pos];
                if (char.IsWhiteSpace(current))
                {
                    return pos;
                }
            }

            return length;
        }
    }
}