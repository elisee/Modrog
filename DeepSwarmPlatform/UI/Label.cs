using DeepSwarmBasics;
using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmPlatform.UI
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

        public override Point ComputeSize(int? maxWidth, int? maxHeight)
        {
            var size = Point.Zero;

            var lines = _text.Split('\n');
            var maxLineWidth = 0;

            foreach (var line in lines) maxLineWidth = Math.Max(maxLineWidth, FontStyle.MeasureText(line));
            if (Width == null) size.X = maxLineWidth;
            if (Height == null) size.Y = FontStyle.LineHeight;

            var actualMaxWidth = Width ?? maxWidth;

            if (Wrap)
            {
                if (actualMaxWidth != null)
                {
                    var lineCount = 0;

                    foreach (var line in lines)
                    {
                        if (line.Length == 0)
                        {
                            lineCount++;
                            continue;
                        }

                        var segmentStart = 0;
                        var segmentEnd = 0;
                        var segmentCursor = 0;
                        var segmentWidth = 0;
                        var newSegmentWidth = FontStyle.GetAdvanceWithKerning(line[0], -1);

                        while (segmentCursor < line.Length)
                        {
                            newSegmentWidth += FontStyle.GetAdvanceWithKerning(line[segmentCursor], segmentCursor > 0 ? line[segmentCursor - 1] : -1);

                            if (segmentCursor != segmentStart && line[segmentCursor] == ' ') segmentEnd = segmentCursor;

                            if (newSegmentWidth >= actualMaxWidth.Value && segmentStart != segmentEnd)
                            {
                                lineCount++;
                                segmentWidth = 0;

                                while (line[segmentEnd] == ' ') segmentEnd++;
                                segmentCursor = segmentStart = segmentEnd;
                                newSegmentWidth = FontStyle.GetAdvanceWithKerning(line[segmentCursor], -1);
                            }
                            else
                            {
                                segmentWidth = newSegmentWidth;
                                segmentCursor++;
                            }
                        }

                        if (line.Length != segmentStart) lineCount++;
                    }

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
                var lines = _text.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Length == 0)
                    {
                        _segments.Add("");
                        continue;
                    }

                    var segmentStart = 0;
                    var segmentEnd = 0;
                    var segmentCursor = 0;
                    var segmentWidth = 0;
                    var newSegmentWidth = FontStyle.GetAdvanceWithKerning(line[0], -1);

                    while (segmentCursor < line.Length)
                    {
                        newSegmentWidth += FontStyle.GetAdvanceWithKerning(line[segmentCursor], segmentCursor > 0 ? line[segmentCursor - 1] : -1);

                        if (segmentCursor != segmentStart && line[segmentCursor] == ' ') segmentEnd = segmentCursor;

                        if (Wrap && newSegmentWidth >= RectangleAfterPadding.Width && segmentStart != segmentEnd)
                        {
                            if (Ellipsize && (_segments.Count + 1) * FontStyle.LineHeight >= RectangleAfterPadding.Height)
                            {
                                var ellipsizedSegmentWidth = FontStyle.MeasureText(line[segmentStart..segmentCursor] + EllipsisText);

                                if (ellipsizedSegmentWidth > RectangleAfterPadding.Width) segmentCursor--;
                                _segments.Add(line[segmentStart..segmentCursor] + EllipsisText);
                                return;
                            }
                            else _segments.Add(line[segmentStart..segmentEnd]);

                            segmentWidth = 0;

                            while (line[segmentEnd] == ' ') segmentEnd++;
                            segmentCursor = segmentStart = segmentEnd;
                            newSegmentWidth = FontStyle.GetAdvanceWithKerning(line[segmentCursor], -1);
                        }
                        else
                        {
                            segmentWidth = newSegmentWidth;
                            segmentCursor++;
                        }
                    }

                    if (line.Length != segmentStart) _segments.Add(line[segmentStart..line.Length]);
                }
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
