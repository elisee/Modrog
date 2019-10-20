namespace ModrogEditor.Interface.Editing
{
    abstract class BaseAssetEditor : EditorElement
    {
        public readonly string FullAssetPath;

        public BaseAssetEditor(EditorApp @interface, string fullAssetPath)
            : base(@interface, null)
        {
            FullAssetPath = fullAssetPath;
        }
    }
}
