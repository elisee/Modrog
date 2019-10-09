using DeepSwarmCommon;
using System.Globalization;

namespace DeepSwarmClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var engine = new Engine();

            if (args.Length >= 1)
            {
                var pieces = args[0].Split(":");
                var hostname = pieces[0];
                var port = pieces.Length > 1 ? int.Parse(pieces[1], CultureInfo.InvariantCulture) : Protocol.Port;
                var scenario = args.Length == 2 ? args[1] : null;

                engine.StartWithConnection(hostname, port, scenario);
            }
            else
            {
                engine.Start();
            }
        }
    }
}
