namespace DeepSwarmClient.UI
{
    public class TextButton : Button
    {
        public string Text { get => _label.Text; set => _label.Text = value; }

        readonly Label _label;

        public TextButton(Desktop desktop, Element parent)
            : base(desktop, parent)
        {
            _label = new Label(desktop, this);
        }
    }
}
