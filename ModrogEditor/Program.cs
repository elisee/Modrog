namespace ModrogEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new EditorApp();

            if (args.Length == 1)
            {
                var entry = app.State.ScenarioEntries.Find(x => x.Name == args[0]);
                if (entry != null) app.State.OpenScenario(entry);
            }

            app.Run();
        }
    }
}
