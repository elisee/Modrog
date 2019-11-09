using SwarmBasics;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SwarmPlatform.UI
{
    public class Label : Element
    {
        public FontStyle FontStyle;

        public Color TextColor = new Color(0xffffffff);
        public string Text { get => _text; set { _text = value ?? throw new Exception("Can't set text to null"); _segments.Clear(); } }
        public bool Wrap;
        public bool Ellipsize;

        string _text = string.Empty;
        readonly List<string> _segments = new List<string>();

        public static readonly string EllipsisText = "..."; // "…";

        public Label(Element parent) : this(parent.Desktop, parent) { }
        public Label(Desktop desktop, Element parent = null) : base(desktop, parent)
        {
            FontStyle = Desktop.MainFontStyle;
        }

        protected override Point ComputeContentSize(int? maxWidth, int? maxHeight)
        {
            var contentSize = Point.Zero;

            if (Width == null && !Ellipsize)
            {
                var maxLineWidth = 0;
                foreach (var line in _text.Split('\n')) maxLineWidth = Math.Max(maxLineWidth, FontStyle.MeasureText(line));
                contentSize.X = maxLineWidth;
            }

            if (Wrap)
            {
                if (Width != null || maxWidth != null)
                {
                    var actualMaxWidth = (Width ?? maxWidth).Value - LeftPadding - RightPadding;
                    var lineCount = 0;

                    var segmentStart = 0;

                    for (var cursor = 0; cursor < _text.Length; cursor++)
                    {
                        var segmentSplit = segmentStart = cursor;
                        var segmentWidth = 0;

                        while (cursor < _text.Length)
                        {
                            if (_text[cursor] == '\n')
                            {
                                lineCount++;
                                break;
                            }

                            segmentWidth += FontStyle.GetAdvanceWithKerning(_text[cursor], cursor > segmentStart ? _text[cursor - 1] : -1);

                            if (cursor != segmentStart && _text[cursor] == ' ') segmentSplit = cursor;

                            if (segmentWidth >= actualMaxWidth && segmentStart != segmentSplit)
                            {
                                lineCount++;

                                while (_text[segmentSplit] == ' ') segmentSplit++;
                                cursor = segmentStart = segmentSplit;
                                segmentWidth = FontStyle.GetAdvanceWithKerning(_text[cursor], -1);
                            }
                            else
                            {
                                cursor++;
                            }
                        }
                    }

                    if (segmentStart != _text.Length) lineCount++;

                    contentSize.X = actualMaxWidth;
                    contentSize.Y = lineCount * FontStyle.LineHeight;
                }
                else
                {
                    throw new NotImplementedException("Wrapped labels with no minimum width are not supported");
                }
            }
            else
            {
                contentSize.Y = FontStyle.LineHeight;
            }

            return contentSize;
        }

        public override void LayoutSelf()
        {
            _segments.Clear();

            if (Wrap)
            {
                var segmentStart = 0;

                for (var cursor = 0; cursor < _text.Length; cursor++)
                {
                    var segmenSplit = segmentStart = cursor;
                    var segmentWidth = 0;

                    while (cursor < _text.Length)
                    {
                        if (_text[cursor] == '\n')
                        {
                            _segments.Add(_text[segmentStart..cursor]);
                            break;
                        }

                        segmentWidth += FontStyle.GetAdvanceWithKerning(_text[cursor], cursor > 0 ? _text[cursor - 1] : -1);

                        if (cursor != segmentStart && _text[cursor] == ' ') segmenSplit = cursor;

                        if (segmentWidth >= _contentRectangle.Width && segmentStart != segmenSplit && (cursor + 1 < _text.Length || segmentWidth > _contentRectangle.Width))
                        {
                            if (Ellipsize && (_segments.Count + 1) * FontStyle.LineHeight >= _contentRectangle.Height)
                            {
                                var ellipsizedSegmentWidth = FontStyle.MeasureText(_text[segmentStart..cursor] + EllipsisText);

                                if (ellipsizedSegmentWidth > _contentRectangle.Width) cursor--;
                                _segments.Add(_text[segmentStart..cursor] + EllipsisText);
                                return;
                            }
                            else _segments.Add(_text[segmentStart..segmenSplit]);

                            segmentWidth = 0;

                            while (_text[segmenSplit] == ' ') segmenSplit++;
                            cursor = segmentStart = segmenSplit;
                            segmentWidth = FontStyle.GetAdvanceWithKerning(_text[cursor], -1);
                        }
                        else
                        {
                            cursor++;
                        }
                    }
                }

                if (segmentStart != _text.Length) _segments.Add(_text[segmentStart..]);
            }
            else
            {
                if (Ellipsize)
                {
                    var textWidth = FontStyle.MeasureText(_text);

                    if (textWidth > _contentRectangle.Width)
                    {
                        for (var textCursor = 0; textCursor < _text.Length; textCursor++)
                        {
                            var newEllipsizedSegmentWidth = FontStyle.MeasureText(_text[0..(textCursor + 1)] + EllipsisText);

                            if (newEllipsizedSegmentWidth > _contentRectangle.Width)
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
                FontStyle.DrawText(_contentRectangle.X, _contentRectangle.Y + i * FontStyle.LineHeight + FontStyle.LineSpacing / 2, _segments[i]);
            }
        }
    }
}
