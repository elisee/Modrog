namespace DeepSwarmScenarioEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            var engine = new Engine();

            if (args.Length == 1)
            {
                var entry = engine.State.ScenarioEntries.Find(x => x.Name == args[0]);
                if (entry != null) engine.State.OpenScenario(entry);
            }

            engine.Start();
        }
    }
}
