
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

    Notes: RUN AS ADMINISTRATOR!

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenHardwareMonitor.Hardware;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace PolMon
{
    partial class Program
    {
        public static int svPort = 8080;
        public static string svIP = "127.0.0.1";
        public static int svRefreshTime = 500; // in milliseconds
        public static string svMachineName = "n/a";
        public static float svTestVar2 = 0.00f;
        public static float svCPUTemp = 0.00f;
        public static float svGPUTemp = 0.00f;
        public static float svGPULoad = 0.00f;
        public static float svFANSpeeds = 0.00f;
        public static float svCPULoad = 0.00f;

        public static Computer computer = new Computer
        {
            CPUEnabled = true,
            MainboardEnabled = true,
            GPUEnabled = true,
            HDDEnabled = true,
            RAMEnabled = true,
            FanControllerEnabled = true,
        };
        static async Task Main(string[] args)
        {

#if !DEBUG
            if (!IsAdministrator())
            {
                try
                {
                    ProcessStartInfo proc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Process.GetCurrentProcess().MainModule.FileName,
                        Verb = "runas"
                    };
                    Process.Start(proc);
                }
                catch
                {
                    Console.WriteLine("This application requires administrator privileges to run.");
                }
                Console.ReadKey();
                Environment.Exit(0);
                return;
            }
#endif

            MbConsole.ParseArguments(args);
            MbConsole.ConfigureConsole();
            Console.WriteLine("Initializing...");

            computer.Open();

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
                    await ServeHtmlResponse(response);
                }
            }

            listener.Close();
        }
        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
                cpuUsage = GetCPULoad(),
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
                svMachineName = Environment.MachineName,
                svCPUTemp = GetCPUTemperature(),
                svGPUTemp = GetGPUTemperature(),
                svGPULoad = GetGPULoad(),
                svFANAvgSpeed = GetFansAvgSpeed(),
                svTestVar2 = GetCPULoad(),
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
        static async Task ServeHtmlResponse(HttpListenerResponse response)
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFilePath = Path.Combine(exeDirectory, "html\\index.html");

            string html = File.ReadAllText(htmlFilePath);
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
}
