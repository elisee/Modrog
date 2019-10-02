namespace DeepSwarmClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var newIdentity = args.Length != 0 && args[0] == "new";

            var engine = new Engine(newIdentity);
            engine.Start();
        }
    }
}
