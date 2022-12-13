using Avalonia.Controls;
using Avalonia.Media.TextFormatting;
using Avalonia.Media;
using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Styling;
using Avalonia;

namespace TQDB_Editor.Common.Controls
{
    public partial class RichTextBlock : SelectableTextBlock, IStyleable
    {
        Type IStyleable.StyleKey => typeof(SelectableTextBlock);

        private readonly List<(ReadOnlySlice<char> slice, TextRunProperties? props)> _textChunks;
        private readonly List<CodeTag> _openTags;

        public static readonly StyledProperty<bool> UseBBCodeProperty =
            AvaloniaProperty.Register<RichTextBlock, bool>(nameof(UseBBCode));

        public bool UseBBCode
        {
            get { return GetValue(UseBBCodeProperty); }
            set { SetValue(UseBBCodeProperty, value); }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (Text == null)
                return;
            if (UseBBCode)
            {
                var parsed = ParseText(Text);
                _textChunks.AddRange(parsed);
            }
            else
                _textChunks.Add((Text.AsMemory(), null));
        }

        private TextRunProperties DefaultProperties
        {
            get
            {
                var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                return new GenericTextRunProperties(typeface, FontSize, TextDecorations, Foreground);
            }
        }

        public RichTextBlock() : base()
        {
            _textChunks = new();
            _openTags = new();
        }

        protected override TextLayout CreateTextLayout(string? text)
        {
            var paragraphProperties = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
                DefaultProperties, TextWrapping, LineHeight, 0, LetterSpacing);

            ITextSource textSource;

            //if (_textRuns != null)
            //{
            //    textSource = new InlinesTextSource(_textRuns);
            //}
            //else
            //{
            textSource = new ComplexTextSource(_textChunks, DefaultProperties);
            //}

            return new TextLayout(
                textSource,
                paragraphProperties,
                TextTrimming,
                _constraint.Width,
                _constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight);
        }

        public void AddText(string? text)
        {
            if (text == null)
                return;

            _textChunks.Add((text.AsMemory(), CreateProps()));
        }

        public void AppendText(string? text)
        {
            if (text == null)
                return;

            var parsed = ParseText(text);
            _textChunks.AddRange(parsed);
        }

        public void AppendNewLine()
        {
            _textChunks.Add(("&#10;".AsMemory(), null));
        }

        private IReadOnlyList<(ReadOnlySlice<char> slice, TextRunProperties? props)> ParseText(string? text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var ret = new List<(ReadOnlySlice<char>, TextRunProperties?)>();
            bool isCode = false;
            var buffer = new List<char>();
            var codeBuffer = new List<char>();

            foreach (var character in text)
            {
                if (isCode)
                {
                    if (character == ']')
                    {
                        isCode = false;
                        try
                        {
                            InterpretCodeBuffer(codeBuffer.ToArray());
                        }
                        catch (BBCException)
                        {
                            buffer.AddRange(codeBuffer);
                        }
                        codeBuffer.Clear();
                        continue;
                    }
                    codeBuffer.Add(character);
                }
                else
                {
                    if (character == '[')
                    {
                        isCode = true;
                        if (buffer.Count > 0)
                            ret.Add((new ReadOnlySlice<char>(buffer.ToArray()), CreateProps()));
                        buffer.Clear();
                        continue;
                    }
                    buffer.Add(character);
                }
            }
            if (buffer.Count > 0)
                ret.Add((new ReadOnlySlice<char>(buffer.ToArray()), CreateProps()));
            if (codeBuffer.Count > 0)
                ret.Add((new ReadOnlySlice<char>(codeBuffer.ToArray()), CreateProps()));

            return ret;
        }

        private void InterpretCodeBuffer(char[] codeBuffer)
        {
            var openCodes = _openTags;
            var isClosing = codeBuffer[0] == '/';
            if (isClosing)
                codeBuffer = codeBuffer[1..];

            // maybe add trimming
            var split = new string(codeBuffer).Split('=');
            var code = split[0];
            var value = split.Length > 1 ? split[1] : null;

            int exisitingIdx;
            if ((exisitingIdx = openCodes.FindIndex(x => x.Tag.Equals(code))) > -1)
            {
                if (isClosing)
                    openCodes.RemoveAt(exisitingIdx);
                else
                    throw new BBCException("Trying to recurse Tags!");
            }
            else
                openCodes.Add(new CodeTag { Tag = code, Value = value });
        }

        private TextRunProperties? CreateProps()
        {
            var openCodes = _openTags;
            if (openCodes.Count == 0)
                return null;

            var defaultProps = DefaultProperties;
            var brush = defaultProps.ForegroundBrush;
            var fontStyle = defaultProps.Typeface.Style;
            var fontWeight = defaultProps.Typeface.Weight;

            var onlyInvalid = true;
            foreach (var openCode in openCodes)
            {
                switch (openCode.Tag)
                {
                    case "color":
                        if (openCode.Value != null && Color.TryParse(openCode.Value, out var color))
                            brush = new SolidColorBrush(color);
                        onlyInvalid = false;
                        break;
                    case "i":
                        fontStyle = FontStyle.Italic;
                        onlyInvalid = false;
                        break;
                    case "o":
                        fontStyle = FontStyle.Oblique;
                        onlyInvalid = false;
                        break;
                    case "b":
                        fontWeight = FontWeight.Bold;
                        onlyInvalid = false;
                        break;
                }
            }
            if (onlyInvalid)
                return null;

            double fontSize = defaultProps.FontRenderingEmSize;
            var fontStretch = defaultProps.Typeface.Stretch;
            var textDeco = defaultProps.TextDecorations;
            var fontFamily = defaultProps.Typeface.FontFamily;

            var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);

            return new GenericTextRunProperties(typeface, fontSize, textDeco, brush);
        }

        public void PushColor(Color color)
        {
            _openTags.Add(new CodeTag { Tag = "color", Value = color.ToString() });
        }

        public void Pop()
        {
            _openTags.RemoveAt(_openTags.Count - 1);
        }


        private struct CodeTag
        {
            public string Tag { get; set; }
            public string? Value { get; set; }
        }

        public class BBCException : Exception
        {
            public BBCException() : base() { }
            public BBCException(string msg) : base(msg) { }
        }

        private readonly struct ComplexTextSource : ITextSource
        {
            private readonly TextRunProperties _defaultProperties;
            private readonly IReadOnlyDictionary<int, int> _sourceIndexToPosMap;
            private readonly IReadOnlyList<(ReadOnlySlice<char> slice, TextRunProperties? props)> _textChunks;
            private readonly int _length;

            public ComplexTextSource(IEnumerable<(ReadOnlySlice<char> slice, TextRunProperties? props)> textChunks, TextRunProperties defaultProps)
            {
                _defaultProperties = defaultProps;
                _textChunks = textChunks.ToList();
                _sourceIndexToPosMap = GenerateMap(_textChunks);
                _length = _textChunks.Sum(x => x.slice.Length);
            }

            private static IReadOnlyDictionary<int, int> GenerateMap(IReadOnlyList<(ReadOnlySlice<char>, TextRunProperties?)> textChunks)
            {
                var ret = new Dictionary<int, int>(textChunks.Count);
                var pos = 0;
                var idx = 0;

                foreach (var chunk in textChunks)
                {
                    ret[pos] = idx++;
                    pos += chunk.Item1.Length;
                }

                return ret;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _length)
                {
                    return new TextEndOfParagraph();
                }

                int startIndex = 0;
                if (!_sourceIndexToPosMap.TryGetValue(textSourceIndex, out int chunkIndex))
                {
                    var keysEnumerator = _sourceIndexToPosMap.Keys.GetEnumerator();

                    while (keysEnumerator.MoveNext())
                    {
                        if (keysEnumerator.Current < textSourceIndex)
                        {
                            chunkIndex = _sourceIndexToPosMap[keysEnumerator.Current];
                            startIndex = textSourceIndex - keysEnumerator.Current;
                        }
                        else
                            break;
                    }
                }

                var (runText, runProps) = _textChunks[chunkIndex];

                runText = runText.Skip(startIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(runText, runProps ?? _defaultProperties);
            }
        }
    }
}