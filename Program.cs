
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PolMon
{
    class Program
    {
        public static int svPort = 8080;
        public static string svIP = "127.0.0.1";
        public static int svRefreshTime = 500; // in milliseconds
        public static string svTestVar = "test";
        public static string svTestVar2 = "test222";

        static async Task Main(string[] args)
        {
            MbConsole.ParseArguments(args);
            MbConsole.ConfigureConsole();
            Console.WriteLine("Initializing...");

            // Initialize performance counters
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            var diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            var uptimeCounter = new PerformanceCounter("System", "System Up Time");
            uptimeCounter.NextValue(); // Initialize uptime counter

            // Initialize network counters
            var networkCounters = new PerformanceCounterCategory("Network Interface")
                .GetInstanceNames()
                .Select(name => new PerformanceCounter("Network Interface", "Bytes Total/sec", name))
                .ToArray();
            foreach (var counter in networkCounters)
                counter.NextValue();

            // Start HTTP listener
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

            Console.WriteLine($"Server started at http://{svIP}:{svPort}/");

            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is InvalidOperationException)
                {
                    Console.WriteLine($"Listener exception: {ex.Message}");
                    break;
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
                    counter.NextValue();

                // Short delay to allow counters to update
                await Task.Delay(svRefreshTime);
                Console.WriteLine($"Request received: {request.RawUrl}");

                // Gather data
                var data = GatherPerformanceData(
                    cpuCounter,
                    ramCounter,
                    diskReadCounter,
                    diskWriteCounter,
                    uptimeCounter,
                    networkCounters);

                if (request.RawUrl == "/data")
                {
                    // Serve JSON data
                    await ServeJsonResponse(response, data);
                }
                else
                {
                    // Serve HTML content
                    await ServeHtmlResponse(response, data);
                }
            }

            listener.Close();
        }

        static PerformanceData GatherPerformanceData(
            PerformanceCounter cpuCounter,
            PerformanceCounter ramCounter,
            PerformanceCounter diskReadCounter,
            PerformanceCounter diskWriteCounter,
            PerformanceCounter uptimeCounter,
            PerformanceCounter[] networkCounters)
        {
            var cpuUsage = cpuCounter.NextValue();
            var ramAvailable = ramCounter.NextValue();
            var networkUsage = networkCounters.Sum(counter => counter.NextValue());
            var diskRead = diskReadCounter.NextValue();
            var diskWrite = diskWriteCounter.NextValue();
            var pagingUsage = GetPagingFileUsagePercent();
            TimeSpan uptimeSpan = TimeSpan.FromSeconds(uptimeCounter.NextValue());
            var processCount = Process.GetProcesses().Length;
            var threadCount = Process.GetProcesses().Sum(p => p.Threads.Count);
            var totalRam = GetTotalPhysicalMemory();
            var ramUsed = totalRam - ramAvailable;
            var ramUsagePercent = (ramUsed / totalRam) * 100;

            return new PerformanceData
            {
                cpuUsage = cpuUsage,
                ramUsed = ramUsed,
                totalRam = totalRam,
                ramUsagePercent = ramUsagePercent,
                networkUsage = networkUsage,
                diskReadFormatted = FormatBytes(diskRead),
                diskWriteFormatted = FormatBytes(diskWrite),
                pagingUsage = pagingUsage,
                uptime = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s",
                processCount = processCount,
                threadCount = threadCount,
                svTestVar = svTestVar,
                svTestVar2 = svTestVar2
            };

        }

        static async Task ServeJsonResponse(HttpListenerResponse response, PerformanceData data)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            string jsonData = JsonConvert.SerializeObject(data, jsonSettings);
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


        static async Task ServeHtmlResponse(HttpListenerResponse response, PerformanceData data)
        {
            string html = HtmlBuilder.BuildHtmlContent(data);
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

        // Helper methods
        static float GetTotalPhysicalMemory()
        {
            var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
                return Convert.ToSingle(obj["TotalVisibleMemorySize"]) / 1024; // Convert to MB
            return 0;
        }

        static string FormatBytes(float bytes)
        {
            return HtmlBuilder.FormatBytes(bytes);
        }

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
            return totalSize > 0 ? (currentUsage / totalSize) * 100 : 0;
        }
    }

    // Data class to hold performance data
    public class PerformanceData
    {
        public float cpuUsage { get; set; }
        public float ramUsed { get; set; }
        public float totalRam { get; set; }
        public float ramUsagePercent { get; set; }
        public float networkUsage { get; set; }
        public string diskReadFormatted { get; set; }
        public string diskWriteFormatted { get; set; }
        public float pagingUsage { get; set; }
        public string uptime { get; set; }
        public int processCount { get; set; }
        public int threadCount { get; set; }
        public string svTestVar { get; set; }
        public string svTestVar2 { get; set; }
    }

}
