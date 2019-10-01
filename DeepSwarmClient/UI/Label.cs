using DeepSwarmCommon;
using System;
using System.Collections.Generic;

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
                    if (textWidth > actualMaxWidth)
                    {
                        size.X = actualMaxWidth.Value;
                        size.Y = RendererHelper.FontRenderSize * (int)Math.Ceiling((double)textWidth / actualMaxWidth.Value);
                    }
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

            var availableWidthForText = LayoutRectangle.Width; // TODO: Padding

            var segmentStart = 0;
            var segmentEnd = 0;
            var segmentWidth = 0;

            for (var i = 0; i < _text.Length; i++)
            {
                // TODO: Measure actual text width when we have variable-width fonts
                var characterWidth = RendererHelper.FontRenderSize;

                if (segmentWidth + characterWidth >= availableWidthForText && segmentStart != segmentEnd)
                {
                    _segments.Add(_text[segmentStart..segmentEnd]);
                    segmentStart = segmentEnd;
                    segmentWidth = 0;
                }
                else
                {
                    segmentWidth += characterWidth;
                    segmentEnd++;
                }
            }

            if (_text.Length != segmentStart) _segments.Add(_text[segmentStart.._text.Length]);
        }

        protected override void DrawSelf()
        {
            base.DrawSelf();

            for (var i = 0; i < _segments.Count; i++)
            {
                RendererHelper.DrawText(Desktop.Renderer, LayoutRectangle.X, LayoutRectangle.Y + i * RendererHelper.FontRenderSize, _segments[i], TextColor);
            }
        }
    }
}
