using SDL2;
using SwarmBasics.Math;
using SwarmPlatform.Graphics;
using System;

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
            Load();
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(_mainLayer);
        }

        protected override void Unload()
        {
            if (_textureArea != null)
            {
                SDL.SDL_DestroyTexture(_textureArea.Texture);
                _textureArea = null;
            }
        }

        protected override bool TryLoad(out string error)
        {
            var texture = SDL_image.IMG_LoadTexture(Desktop.Renderer, FullAssetPath);

            if (texture == IntPtr.Zero)
            {
                error = "Error while loading texture: " + SDL.SDL_GetError();
                return false;
            }

            SDL.SDL_QueryTexture(texture, out _, out _, out var textureWidth, out var textureHeight);
            _textureArea = new TextureArea(texture, new Rectangle(0, 0, textureWidth, textureHeight));

            error = null;
            return true;
        }

        protected override bool TrySave(out string error)
        {
            // Nothing for now
            error = null;
            return true;
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
