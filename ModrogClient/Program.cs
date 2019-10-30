using ModrogCommon;
using System.Globalization;

namespace ModrogClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new ClientApp();

            if (args.Length >= 1)
            {
                if (args[0] == "--scenario")
                {
                    app.State.StartServer(args[1]);
                }
                else
                {
                    var pieces = args[0].Split(":");
                    var hostname = pieces[0];
                    var port = pieces.Length > 1 ? int.Parse(pieces[1], CultureInfo.InvariantCulture) : Protocol.Port;
                    var scenario = args.Length == 2 ? args[1] : null;
                    app.State.Connect(hostname, port, scenario);
                }
            }

            app.Run();
        }
    }
}
