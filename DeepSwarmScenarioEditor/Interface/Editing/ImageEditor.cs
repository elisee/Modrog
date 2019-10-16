using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using SDL2;

namespace DeepSwarmScenarioEditor.Interface.Editing
{
    class ImageEditor : InterfaceElement
    {
        readonly string _filePath;
        TextureArea _textureArea;

        public ImageEditor(Interface @interface, Element parent, string filePath)
            : base(@interface, parent)
        {
            _filePath = filePath;
            if (_filePath != null) LoadTexture();
        }

        public override void OnMounted()
        {
            if (_filePath != null) LoadTexture();
        }

        void LoadTexture()
        {
            if (_textureArea != null) SDL.SDL_DestroyTexture(_textureArea.Texture);

            var texture = SDL_image.IMG_LoadTexture(Engine.Renderer, _filePath);
            SDL.SDL_QueryTexture(texture, out _, out _, out var textureWidth, out var textureHeight);

            _textureArea = new TextureArea(texture, new Rectangle(0, 0, textureWidth, textureHeight));
        }

        public override void OnUnmounted()
        {
            if (_textureArea != null)
            {
                SDL.SDL_DestroyTexture(_textureArea.Texture);
                _textureArea = null;
            }
        }

        protected override void DrawSelf()
        {
            var sourceRect = _textureArea.Rectangle.ToSDL_Rect();
            var destRect = new Rectangle(
                LayoutRectangle.X + LayoutRectangle.Width / 2 - sourceRect.w / 2,
                LayoutRectangle.Y + LayoutRectangle.Height / 2 - sourceRect.h / 2,
                sourceRect.w,
                sourceRect.h).ToSDL_Rect();
            SDL.SDL_RenderCopy(Engine.Renderer, _textureArea.Texture, ref sourceRect, ref destRect);
        }
    }
}
