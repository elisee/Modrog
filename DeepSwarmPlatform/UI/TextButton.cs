namespace DeepSwarmPlatform.UI
{
    public class TextButton : Button
    {
        public string Text { get => Label.Text; set => Label.Text = value; }

        public readonly Label Label;

        public TextButton(Element parent) : this(parent.Desktop, parent) { }
        public TextButton(Desktop desktop, Element parent) : base(desktop, parent)
        {
            Label = new Label(desktop, this);
        }
    }
}
