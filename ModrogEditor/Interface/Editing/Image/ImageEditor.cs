using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using SDL2;

namespace ModrogEditor.Interface.Editing.Image
{
    class ImageEditor : BaseAssetEditor
    {
        TextureArea _textureArea;

        public ImageEditor(Interface @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
        }

        public override void OnMounted()
        {
            var texture = SDL_image.IMG_LoadTexture(Engine.Renderer, FullAssetPath);
            SDL.SDL_QueryTexture(texture, out _, out _, out var textureWidth, out var textureHeight);

            _textureArea = new TextureArea(texture, new Rectangle(0, 0, textureWidth, textureHeight));

            Desktop.SetFocusedElement(this);
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
