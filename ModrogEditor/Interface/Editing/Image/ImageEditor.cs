using SDL2;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;

namespace ModrogEditor.Interface.Editing.Image
{
    class ImageEditor : BaseAssetEditor
    {
        TextureArea _textureArea;

        public static void CreateEmptyFile(string fullAssetPath)
        {
            // TODO
        }

        public ImageEditor(EditorApp @interface, string fullAssetPath)
            : base(@interface, fullAssetPath)
        {
        }

        public override void OnMounted()
        {
            var texture = SDL_image.IMG_LoadTexture(Desktop.Renderer, FullAssetPath);
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
            SDL.SDL_RenderCopy(Desktop.Renderer, _textureArea.Texture, ref sourceRect, ref destRect);
        }
    }
}
