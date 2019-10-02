using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmClient.UI
{
    public class Label : Element
    {
        public FontStyle FontStyle;

        public Color TextColor = new Color(0xffffffff);
        public string Text { get => _text; set { _text = value; _segments.Clear(); } }
        public bool Wrap;
        public bool Ellipsize;

        string _text = string.Empty;
        readonly List<string> _segments = new List<string>();

        public static readonly string EllipsisText = "…";

        public Label(Element parent) : this(parent.Desktop, parent) { }
        public Label(Desktop desktop, Element parent = null) : base(desktop, parent)
        {
            FontStyle = Desktop.MainFontStyle;
        }

        public override Point ComputeSize(int? maxWidth, int? maxHeight)
        {
            var size = Point.Zero;

            // TODO: Support line breaks
            var textWidth = FontStyle.MeasureText(_text);
            if (Width == null) size.X = textWidth;
            if (Height == null) size.Y = FontStyle.LineHeight;

            var actualMaxWidth = Width ?? maxWidth;

            if (Wrap)
            {
                if (actualMaxWidth != null)
                {
                    var lineCount = 0;

                    var segmentStart = 0;
                    var segmentEnd = 0;
                    var segmentCursor = 0;
                    var segmentWidth = 0;

                    while (segmentCursor < _text.Length)
                    {
                        // TODO: Could just pass the current and previous character
                        var newSegmentWidth = FontStyle.MeasureText(_text[segmentStart..(segmentCursor + 1)]);

                        if (segmentCursor != segmentStart && _text[segmentCursor] == ' ') segmentEnd = segmentCursor;

                        if (newSegmentWidth >= actualMaxWidth.Value && segmentStart != segmentEnd)
                        {
                            lineCount++;
                            segmentWidth = 0;

                            while (_text[segmentEnd] == ' ') segmentEnd++;
                            segmentCursor = segmentStart = segmentEnd;
                        }
                        else
                        {
                            segmentWidth = newSegmentWidth;
                            segmentCursor++;
                        }
                    }

                    if (_text.Length != segmentStart) lineCount++;

                    size.X = actualMaxWidth.Value;
                    size.Y = lineCount * FontStyle.LineHeight;
                }
                else
                {
                    // TODO: Compute length of all words and return longest length
                    throw new NotImplementedException();
                }
            }

            return size + base.ComputeSize(maxWidth, maxHeight);
        }

        public override void LayoutSelf()
        {
            _segments.Clear();

            if (Wrap)
            {
                var segmentStart = 0;
                var segmentEnd = 0;
                var segmentCursor = 0;
                var segmentWidth = 0;

                while (segmentCursor < _text.Length)
                {
                    // TODO: Compute just pass the current and previous character
                    var newSegmentWidth = FontStyle.MeasureText(_text[segmentStart..(segmentCursor + 1)]);

                    if (segmentCursor != segmentStart && _text[segmentCursor] == ' ') segmentEnd = segmentCursor;

                    if (Wrap && newSegmentWidth >= RectangleAfterPadding.Width && segmentStart != segmentEnd)
                    {
                        if (Ellipsize && (_segments.Count + 1) * FontStyle.LineHeight >= RectangleAfterPadding.Height)
                        {
                            var ellipsizedSegmentWidth = FontStyle.MeasureText(_text[segmentStart..segmentCursor] + EllipsisText);

                            if (ellipsizedSegmentWidth > RectangleAfterPadding.Width) segmentCursor--;
                            _segments.Add(_text[segmentStart..segmentCursor] + EllipsisText);
                            return;
                        }
                        else _segments.Add(_text[segmentStart..segmentEnd]);

                        segmentWidth = 0;

                        while (_text[segmentEnd] == ' ') segmentEnd++;
                        segmentCursor = segmentStart = segmentEnd;
                    }
                    else
                    {
                        segmentWidth = newSegmentWidth;
                        segmentCursor++;
                    }
                }

                if (_text.Length != segmentStart) _segments.Add(_text[segmentStart.._text.Length]);
            }
            else
            {
                if (Ellipsize)
                {
                    var textWidth = FontStyle.MeasureText(_text);

                    if (textWidth > RectangleAfterPadding.Width)
                    {
                        for (var textCursor = 0; textCursor < _text.Length; textCursor++)
                        {
                            var newEllipsizedSegmentWidth = FontStyle.MeasureText(_text[0..(textCursor + 1)] + EllipsisText);

                            if (newEllipsizedSegmentWidth > RectangleAfterPadding.Width)
                            {
                                _segments.Add(_text[0..textCursor] + EllipsisText);
                                return;
                            }
                        }
                    }
                }

                _segments.Add(_text);
            }
        }

        protected override void DrawSelf()
        {
            Debug.Assert(_segments.Count > 0 || _text.Length == 0, "Label.Layout() must be called after setting Label.Text.");

            base.DrawSelf();

            for (var i = 0; i < _segments.Count; i++)
            {
                TextColor.UseAsDrawColor(Desktop.Renderer);
                FontStyle.DrawText(RectangleAfterPadding.X, RectangleAfterPadding.Y + i * FontStyle.LineHeight + FontStyle.LineSpacing / 2, _segments[i]);
            }
        }
    }
}
