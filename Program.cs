using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Management;
using System.Linq; // For LINQ methods
using Newtonsoft.Json; // Using Newtonsoft.Json for JSON serialization
using System.IO; // For IOException

namespace polmon
{
    class Program
    {
        public static int svPort = 8080;
        public static string svIP = "127.0.0.1";
        public static int svRefreshTime = 500; // in milliseconds

        static async Task Main(string[] args)
        {
            ParseArguments(args);
            ConsoleWindowCfg();
            Console.WriteLine("Init: started");

            // Initialize performance counters
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            var diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            var uptimeCounter = new PerformanceCounter("System", "System Up Time");
            uptimeCounter.NextValue(); // Initialize uptime counter

            // Initialize network counters for all instances
            var networkCategory = new PerformanceCounterCategory("Network Interface");
            var instanceNames = networkCategory.GetInstanceNames();
            var networkCounters = instanceNames.Select(name => new PerformanceCounter("Network Interface", "Bytes Total/sec", name)).ToArray();

            // Initialize network counters
            foreach (var counter in networkCounters)
            {
                counter.NextValue();
            }

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{svIP}:{svPort}/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Error starting HTTP listener: {ex.Message}");
                return;
            }

            Console.WriteLine("Init: ok!");
            Console.WriteLine($"Server started: {svIP}, Port: {svPort}, Refresh Time: {svRefreshTime}ms");

            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (HttpListenerException ex)
                {
                    Console.WriteLine($"HTTP Listener Exception: {ex.Message}");
                    break; // Exit the loop if the listener is stopped
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Invalid Operation: {ex.Message}");
                    break; // Exit the loop if the listener is stopped
                }

                var response = context.Response;
                var request = context.Request;

                // Initialize counters
                cpuCounter.NextValue();
                diskReadCounter.NextValue();
                diskWriteCounter.NextValue();
                ramCounter.NextValue();
                uptimeCounter.NextValue();
                foreach (var counter in networkCounters)
                {
                    counter.NextValue();
                }

                // Short delay to allow counters to update
                await Task.Delay(svRefreshTime);
                Console.WriteLine($"Request received: {request.RawUrl}");

                // Gather data
                var cpuUsage = cpuCounter.NextValue();
                var ramAvailable = ramCounter.NextValue();
                var networkUsage = networkCounters.Sum(counter => counter.NextValue());
                var diskRead = diskReadCounter.NextValue();
                var diskWrite = diskWriteCounter.NextValue();
                var pagingUsage = GetPagingFileUsagePercent(); // Use WMI method
                TimeSpan uptimeSpan = TimeSpan.FromSeconds(uptimeCounter.NextValue());
                var processCount = Process.GetProcesses().Length;
                int threadCount = 0;
                foreach (Process proc in Process.GetProcesses())
                {
                    threadCount += proc.Threads.Count;
                }

                // Get total physical memory in MB
                var totalRam = GetTotalPhysicalMemory();

                // Calculate used RAM
                var ramUsed = totalRam - ramAvailable;
                var ramUsagePercent = (ramUsed / totalRam) * 100;

                // Format bytes
                string networkUsageFormatted = FormatBytes(networkUsage);
                string diskReadFormatted = FormatBytes(diskRead);
                string diskWriteFormatted = FormatBytes(diskWrite);

                if (request.RawUrl == "/data")
                {
                    // Serve JSON data
                    var data = new
                    {
                        cpuUsage,
                        ramUsed,
                        totalRam,
                        ramUsagePercent,
                        networkUsage,
                        networkUsageFormatted,
                        diskReadFormatted,
                        diskWriteFormatted,
                        pagingUsage,
                        uptime = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s",
                        processCount,
                        threadCount
                    };

                    string jsonData = JsonConvert.SerializeObject(data);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonData);
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = buffer.Length;
                    try
                    {
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (HttpListenerException ex)
                    {
                        Console.WriteLine($"Error writing response: {ex.Message}");
                    }
                    response.Close();
                }
                else
                {
                    // Serve HTML content
                    string html = HtmlTemplate.GetHtml(
                        cpuUsage,
                        ramUsed,
                        totalRam,
                        ramUsagePercent,
                        networkUsage,
                        networkUsageFormatted,
                        diskReadFormatted,
                        diskWriteFormatted,
                        pagingUsage,
                        uptimeSpan,
                        processCount,
                        threadCount);

                    byte[] buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = buffer.Length;
                    try
                    {
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    catch (HttpListenerException ex)
                    {
                        Console.WriteLine($"Error writing response: {ex.Message}");
                    }
                    response.Close();
                }
            }

            // Shutdown procedures
            listener.Close();
        }

        static void ParseArguments(string[] cmdArgs)
        {
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                switch (cmdArgs[i].ToLower())
                {
                    case "-port":
                        if (i + 1 < cmdArgs.Length && int.TryParse(cmdArgs[i + 1], out int port))
                        {
                            svPort = port;
                            i++; // Skip next argument as it's the value
                        }
                        else
                        {
                            Console.WriteLine("Invalid or missing value for -port. Using default port 8080.");
                        }
                        break;
                    case "-ip":
                        if (i + 1 < cmdArgs.Length && IsValidIPAddress(cmdArgs[i + 1]))
                        {
                            svIP = cmdArgs[i + 1];
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
                            svRefreshTime = rtime;
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
                        DisplayHelp();
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {cmdArgs[i]}");
                        break;
                }
            }

            Console.WriteLine($"Configuration - IP: {svIP}, Port: {svPort}, Refresh Time: {svRefreshTime}ms");
        }

        static void DisplayHelp()
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

        static bool IsValidIPAddress(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        static void ConsoleWindowCfg()
        {
            try
            {
                Console.SetWindowSize(100, 40);
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

        // Helper method to get total physical memory in MB
        static float GetTotalPhysicalMemory()
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return Convert.ToSingle(obj["TotalVisibleMemorySize"]) / 1024; // Convert to MB
            }
            return 0;
        }

        // Helper method to format bytes
        static string FormatBytes(float bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes = bytes / 1024;
            }
            return String.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", bytes, sizes[order]);
        }

        // Method to get paging file usage percentage
        static float GetPagingFileUsagePercent()
        {
            float totalSize = 0;
            float currentUsage = 0;
            var searcher = new ManagementObjectSearcher("SELECT AllocatedBaseSize, CurrentUsage FROM Win32_PageFileUsage");
            foreach (ManagementObject obj in searcher.Get())
            {
                totalSize += Convert.ToUInt32(obj["AllocatedBaseSize"]);
                currentUsage += Convert.ToUInt32(obj["CurrentUsage"]);
            }

            if (totalSize > 0)
            {
                return (currentUsage / totalSize) * 100;
            }
            return 0;
        }
    }
}
