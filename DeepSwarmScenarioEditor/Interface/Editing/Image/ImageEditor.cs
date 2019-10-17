using DeepSwarmBasics.Math;
using DeepSwarmPlatform.Graphics;
using DeepSwarmPlatform.UI;
using SDL2;

namespace DeepSwarmScenarioEditor.Interface.Editing.Image
{
    class ImageEditor : InterfaceElement
    {
        TextureArea _textureArea;

        public ImageEditor(Interface @interface, Element parent)
            : base(@interface, parent)
        {
        }

        public override void OnMounted()
        {
            var texture = SDL_image.IMG_LoadTexture(Engine.Renderer, Engine.State.GetActiveAssetFullPath());
            SDL.SDL_QueryTexture(texture, out _, out _, out var textureWidth, out var textureHeight);

            _textureArea = new TextureArea(texture, new Rectangle(0, 0, textureWidth, textureHeight));
        }

        public override void OnUnmounted()
        {
            SDL.SDL_DestroyTexture(_textureArea.Texture);
            _textureArea = null;
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
