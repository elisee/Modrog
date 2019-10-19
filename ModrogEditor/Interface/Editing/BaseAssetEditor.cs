namespace ModrogEditor.Interface.Editing
{
    abstract class BaseAssetEditor : InterfaceElement
    {
        public readonly string FullAssetPath;

        public BaseAssetEditor(Interface @interface, string fullAssetPath)
            : base(@interface, null)
        {
            FullAssetPath = fullAssetPath;
        }
    }
}
