using SwarmPlatform.UI;

namespace ModrogEditor.Interface
{
    class EditorElement : Element
    {
        public readonly EditorApp App;

        public EditorElement(EditorApp app, Element parent)
            : base(app.Desktop, null)
        {
            App = app;
            parent?.Add(this);
        }

    }
}
