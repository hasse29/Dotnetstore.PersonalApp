using System.Text;
using MimeKit.Text;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>A stretch of text with one style. <see cref="Href"/> is set for link text.</summary>
internal sealed record HtmlRun(string Text, bool Bold, bool Italic, bool Underline, string? Href);

/// <summary>A paragraph-like piece of a formatted message: its runs plus how it is laid out.</summary>
internal sealed record HtmlBlock(IReadOnlyList<HtmlRun> Runs, int Indent, int HeadingLevel, bool IsPreformatted, bool IsQuote);

/// <summary>
/// Reduces the HTML of a received message to simple formatted blocks (paragraphs, headings, lists, quotes, bold, italic, underline,
/// links). This is deliberately not a browser: scripts, styles, forms and frames are dropped, images are never loaded (a tracking
/// pixel would tell the sender the mail was read) and are shown as their alt text, and links are shown but not followed.
/// </summary>
internal static class HtmlFormatter
{
    public const int MaxBlocks = 5000;

    private static readonly HashSet<string> IgnoredTags = new(StringComparer.Ordinal)
    {
        "script", "style", "head", "title", "template", "noscript", "iframe", "object", "embed", "svg",
    };

    private static readonly HashSet<string> BlockTags = new(StringComparer.Ordinal)
    {
        "p", "div", "section", "article", "header", "footer", "main", "nav", "aside", "address", "table", "tr", "dl", "dt", "dd",
        "ul", "ol", "li", "blockquote", "pre", "hr", "fieldset", "center", "body", "html",
    };

    public static IReadOnlyList<HtmlBlock> Format(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var state = new State();
        using var reader = new StringReader(html);
        var tokenizer = new HtmlTokenizer(reader) { DecodeCharacterReferences = true };

        while (state.Blocks.Count < MaxBlocks && tokenizer.ReadNextToken(out var token))
        {
            switch (token)
            {
                case HtmlTagToken tag:
                    state.Tag(tag);
                    break;
                case HtmlDataToken data:
                    state.Text(data.Data);
                    break;
            }
        }

        state.Flush();
        return state.Blocks;
    }

    private sealed class State
    {
        private readonly List<HtmlRun> _runs = [];
        private readonly Stack<bool> _lists = new();
        private readonly Stack<int> _counters = new();
        private readonly Stack<string?> _links = new();
        private string? _ignoring;
        private int _ignoreDepth;
        private int _bold;
        private int _italic;
        private int _underline;
        private int _quote;
        private int _pre;
        private int _heading;

        public List<HtmlBlock> Blocks { get; } = [];

        public void Tag(HtmlTagToken tag)
        {
#pragma warning disable CA1308 // HTML tag names are lower case by convention; this is a lookup key, not a normalisation for storage.
            var name = tag.Name.ToLowerInvariant();
#pragma warning restore CA1308

            if (_ignoring is not null)
            {
                if (name == _ignoring && !tag.IsEmptyElement)
                {
                    _ignoreDepth += tag.IsEndTag ? -1 : 1;
                    if (_ignoreDepth <= 0)
                    {
                        _ignoring = null;
                    }
                }

                return;
            }

            if (!tag.IsEndTag && IgnoredTags.Contains(name) && !tag.IsEmptyElement)
            {
                _ignoring = name;
                _ignoreDepth = 1;
                return;
            }

            if (tag.IsEndTag)
            {
                Close(name);
            }
            else
            {
                Open(name, tag);
            }
        }

        private void Open(string name, HtmlTagToken tag)
        {
            if (BlockTags.Contains(name) || (name.Length == 2 && name[0] == 'h' && char.IsDigit(name[1])))
            {
                Flush();
            }

            switch (name)
            {
                case "br":
                    Append("\n");
                    break;
                case "hr":
                    Append("────────────────");
                    Flush();
                    break;
                case "ul" or "ol":
                    _lists.Push(name == "ol");
                    _counters.Push(0);
                    break;
                case "li":
                    var ordered = _lists.Count > 0 && _lists.Peek();
                    var number = _counters.Count > 0 ? _counters.Pop() + 1 : 1;
                    if (_counters.Count > 0 || _lists.Count > 0)
                    {
                        _counters.Push(number);
                    }

                    Append(ordered ? $"{number}. " : "• ");
                    break;
                case "blockquote":
                    _quote++;
                    break;
                case "pre":
                    _pre++;
                    break;
                case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                    _heading = name[1] - '0';
                    break;
                case "b" or "strong":
                    _bold++;
                    break;
                case "i" or "em" or "cite" or "dfn" or "address":
                    _italic++;
                    break;
                case "u" or "ins":
                    _underline++;
                    break;
                case "a":
                    var href = SafeLink(AttributeValue(tag, "href"));
                    _links.Push(href);
                    if (href is not null)
                    {
                        _underline++;
                    }

                    break;
                case "td" or "th":
                    if (_runs.Count > 0)
                    {
                        Append("  ");
                    }

                    if (name == "th")
                    {
                        _bold++;
                    }

                    break;
                case "img":
                    var alt = AttributeValue(tag, "alt");
                    _italic++;
                    Append(string.IsNullOrWhiteSpace(alt) ? "[image]" : $"[{alt.Trim()}]");
                    _italic--;
                    break;
            }
        }

        private void Close(string name)
        {
            if (BlockTags.Contains(name) || (name.Length == 2 && name[0] == 'h' && char.IsDigit(name[1])))
            {
                Flush();
            }

            switch (name)
            {
                case "ul" or "ol":
                    if (_lists.Count > 0)
                    {
                        _lists.Pop();
                        _counters.Pop();
                    }

                    break;
                case "blockquote":
                    _quote = Math.Max(0, _quote - 1);
                    break;
                case "pre":
                    _pre = Math.Max(0, _pre - 1);
                    break;
                case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                    _heading = 0;
                    break;
                case "b" or "strong" or "th":
                    _bold = Math.Max(0, _bold - 1);
                    break;
                case "i" or "em" or "cite" or "dfn" or "address":
                    _italic = Math.Max(0, _italic - 1);
                    break;
                case "u" or "ins":
                    _underline = Math.Max(0, _underline - 1);
                    break;
                case "a":
                    if (_links.Count > 0 && _links.Pop() is not null)
                    {
                        _underline = Math.Max(0, _underline - 1);
                    }

                    break;
            }
        }

        public void Text(string text)
        {
            if (_ignoring is not null || text.Length == 0)
            {
                return;
            }

            Append(_pre > 0 ? text : CollapseWhitespace(text));
        }

        public void Flush()
        {
            if (_runs.Count > 0 && _runs.Any(r => !string.IsNullOrWhiteSpace(r.Text)) && Blocks.Count < MaxBlocks)
            {
                var last = _runs[^1];
                _runs[^1] = last with { Text = last.Text.TrimEnd() };
                var first = _runs[0];
                if (_pre == 0)
                {
                    _runs[0] = first with { Text = first.Text.TrimStart() };
                }

                Blocks.Add(new HtmlBlock([.. _runs.Where(r => r.Text.Length > 0)], _lists.Count + _quote, _heading, _pre > 0, _quote > 0));
            }

            _runs.Clear();
        }

        private void Append(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            // A space right after a space or at the start of a block adds nothing in normal text.
            if (_pre == 0 && text[0] == ' ' && (_runs.Count == 0 || _runs[^1].Text.EndsWith(' ') || _runs[^1].Text.EndsWith('\n')))
            {
                text = text.TrimStart(' ');
                if (text.Length == 0)
                {
                    return;
                }
            }

            var link = _links.Count > 0 ? _links.Peek() : null;
            var run = new HtmlRun(text, _bold > 0 || _heading > 0, _italic > 0, _underline > 0, link);

            if (_runs.Count > 0 && _runs[^1] with { Text = string.Empty } == run with { Text = string.Empty })
            {
                _runs[^1] = _runs[^1] with { Text = _runs[^1].Text + text };
            }
            else
            {
                _runs.Add(run);
            }
        }

        private static string CollapseWhitespace(string text)
        {
            var builder = new StringBuilder(text.Length);
            var space = false;
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    space = true;
                    continue;
                }

                if (space)
                {
                    builder.Append(' ');
                    space = false;
                }

                builder.Append(c);
            }

            if (space)
            {
                builder.Append(' ');
            }

            return builder.ToString();
        }

        private static string? AttributeValue(HtmlTagToken tag, string name) =>
            tag.Attributes.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Only web and mail links are shown as links; <c>javascript:</c> and the like are plain text.</summary>
        private static string? SafeLink(string? href)
        {
            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(href.Trim(), UriKind.Absolute, out var uri))
            {
                return null;
            }

            return uri.Scheme is "http" or "https" or "mailto" ? uri.OriginalString : null;
        }
    }
}
