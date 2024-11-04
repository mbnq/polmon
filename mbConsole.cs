
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

using System;
using System.IO;
using System.Net;

namespace polmon
{
    public static class MbConsole
    {
        public static void ConfigureConsoleWindow()
        {
            try
            {
                // Set the window size (columns, rows)
                Console.SetWindowSize(100, 40);

                // Set the buffer size (columns, rows)
                Console.SetBufferSize(100, 1000);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Clear();
                Console.CursorVisible = false;
                Console.Title = "PolMon (mbnq.pl)";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"Error configuring console window: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO Error configuring console window: {ex.Message}");
            }
        }
        public static void DisplayHelp()
        {
            Console.WriteLine("PolMon (mbnq.pl) - Server Monitor");
            Console.WriteLine("Usage:");
            Console.WriteLine("  polmon.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -port <number>    Set the listening port (default: 8080)");
            Console.WriteLine("  -ip <address>     Set the IP address to listen on (default: 127.0.0.1)");
            Console.WriteLine("  -rtime <number>   Set the refresh time in milliseconds (default: 500)");
            Console.WriteLine("  -help, --help     Display this help message");
        }
        public static void ParseArguments(string[] cmdArgs)
        {
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                switch (cmdArgs[i].ToLower())
                {
                    case "-port":
                        if (i + 1 < cmdArgs.Length && int.TryParse(cmdArgs[i + 1], out int port))
                        {
                            Program.svPort = port;
                            i++; // Skip next argument as it's the value
                        }
                        else
                        {
                            Console.WriteLine("Invalid or missing value for -port. Using default port 8080.");
                        }
                        break;
                    case "-ip":
                        if (i + 1 < cmdArgs.Length && MbConsole.IsValidIPAddress(cmdArgs[i + 1]))
                        {
                            Program.svIP = cmdArgs[i + 1];
                            i++;
                        }
                        else
                        {
                            Console.WriteLine("Invalid or missing value for -ip. Using default IP 127.0.0.1.");
                        }
                        break;
                    case "-rtime":
                        if (i + 1 < cmdArgs.Length && int.TryParse(cmdArgs[i + 1], out int rtime))
                        {
                            Program.svRefreshTime = rtime;
                            i++;
                        }
                        else
                        {
                            Console.WriteLine("Invalid or missing value for -rtime. Using default refresh time 500ms.");
                        }
                        break;
                    case "-help":
                    case "--help":
                    case "?":
                    case "/?":
                    case "-?":
                    case "--?":
                        MbConsole.DisplayHelp();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {cmdArgs[i]}");
                        break;
                }
            }

            Console.WriteLine($"Configuration - IP: {Program.svIP}, Port: {Program.svPort}, Refresh Time: {Program.svRefreshTime}ms");
        }

        // Validates the provided IP address.
        public static bool IsValidIPAddress(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }
    }
}
