
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

using System;
using System.Diagnostics;
using System.Net;

namespace PolMon
{
    public static class MbConsole
    {
        public static void ConfigureConsole()
        {
            try
            {
                Console.SetWindowSize(100, 40);
                Console.SetBufferSize(100, 1000);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                Console.CursorVisible = false;
                Console.Title = "PolMon";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configuring console: {ex.Message}");
            }
        }

        public static void DisplayHelp()
        {
            Console.WriteLine("PolMon - Server Monitor");
            Console.WriteLine("Usage:");
            Console.WriteLine("  polmon.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -port <number>    Set the listening port (default: 8080)");
            Console.WriteLine("  -ip <address>     Set the IP address to listen on (default: 127.0.0.1)");
            Console.WriteLine("  -rtime <number>   Set the refresh time in milliseconds (default: 500)");
            Console.WriteLine("  -local            Open the server in the default web browser when ran locally");
            Console.WriteLine("  -help, --help     Display this help message");
        }

        public static void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                            Program.svPort = port;
                        else
                            Console.WriteLine("Invalid or missing value for -port. Using default port 8080.");
                        break;
                    case "-ip":
                        if (i + 1 < args.Length && IPAddress.TryParse(args[++i], out _))
                            Program.svIP = args[i];
                        else
                            Console.WriteLine("Invalid or missing value for -ip. Using default IP 127.0.0.1.");
                        break;
                    case "-rtime":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int rtime))
                            Program.svRefreshTime = rtime;
                        else
                            Console.WriteLine("Invalid or missing value for -rtime. Using default refresh time 500ms.");
                        break;
                    case "-local":
                        int localPort = Program.svPort > 0 ? Program.svPort : 8080;
                        string url = $"http://{Program.svIP}:{localPort}";
                        Console.WriteLine($"Opening local server at {url}");
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        break;
                    case "-help":
                    case "--help":
                    case "/?":
                        DisplayHelp();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        break;
                }
            }
        }
    }
}
