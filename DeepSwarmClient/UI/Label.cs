using DeepSwarmCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepSwarmClient.UI
{
    class Label : Element
    {
        public Color TextColor = new Color(0xffffffff);
        public string Text { set { _text = value; _segments.Clear(); } get => _text; }
        public bool Wrap;

        string _text;
        readonly List<string> _segments = new List<string>();

        public Label(Desktop desktop, Element parent)
             : base(desktop, parent)
        {
        }

        public override Point ComputeSize(int? maxWidth, int? maxHeight)
        {
            var size = base.ComputeSize(maxWidth, maxHeight);

            // TODO: Support line breaks
            var textWidth = _text.Length * RendererHelper.FontRenderSize;
            if (Anchor.Width == null) size.X += textWidth;
            if (Anchor.Height == null) size.Y += RendererHelper.FontRenderSize;

            var actualMaxWidth = Anchor.Width ?? maxWidth;

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
                        // TODO: Measure actual text width when we have variable-width fonts
                        var characterWidth = RendererHelper.FontRenderSize;

                        if (segmentCursor != segmentStart && _text[segmentCursor] == ' ') segmentEnd = segmentCursor;

                        if (segmentWidth + characterWidth >= actualMaxWidth.Value && segmentStart != segmentEnd)
                        {
                            lineCount++;
                            segmentWidth = 0;

                            while (_text[segmentEnd] == ' ') segmentEnd++;
                            segmentCursor = segmentStart = segmentEnd;
                        }
                        else
                        {
                            segmentWidth += characterWidth;
                            segmentCursor++;
                        }
                    }

                    if (_text.Length != segmentStart) lineCount++;

                    size.X = actualMaxWidth.Value * RendererHelper.FontRenderSize;
                    size.Y = lineCount * RendererHelper.FontRenderSize;
                }
                else
                {
                    // TODO: Compute length of all words and return longest length
                    throw new NotImplementedException();
                }
            }

            return size;
        }

        public override void LayoutSelf()
        {
            _segments.Clear();

            if (Wrap)
            {
                var availableWidthForText = LayoutRectangle.Width; // TODO: Padding

                var segmentStart = 0;
                var segmentEnd = 0;
                var segmentCursor = 0;
                var segmentWidth = 0;

                while (segmentCursor < _text.Length)
                {
                    // TODO: Measure actual text width when we have variable-width fonts
                    var characterWidth = RendererHelper.FontRenderSize;

                    if (segmentCursor != segmentStart && _text[segmentCursor] == ' ') segmentEnd = segmentCursor;

                    if (segmentWidth + characterWidth >= availableWidthForText && segmentStart != segmentEnd)
                    {
                        _segments.Add(_text[segmentStart..segmentEnd]);
                        segmentWidth = 0;

                        while (_text[segmentEnd] == ' ') segmentEnd++;
                        segmentCursor = segmentStart = segmentEnd;
                    }
                    else
                    {
                        segmentWidth += characterWidth;
                        segmentCursor++;
                    }
                }

                if (_text.Length != segmentStart) _segments.Add(_text[segmentStart.._text.Length]);
            }
            else
            {
                _segments.Add(_text);
            }
        }

        protected override void DrawSelf()
        {
            Debug.Assert(_segments.Count > 0 || _text.Length == 0, "Label.Layout() must be called after setting Label.Text.");

            base.DrawSelf();

            for (var i = 0; i < _segments.Count; i++)
            {
                RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y + i * RendererHelper.FontRenderSize, _segments[i], TextColor);
            }
        }
    }
}
