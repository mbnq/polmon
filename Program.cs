using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace polmon
{
    class Program
    {
        public static int svPort = 8080;
        public static string svIP = "127.0.0.1";
        public static int svRefreshTime = 1000; // in milliseconds
        static async Task Main(string[] args)
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var networkCategory = new PerformanceCounterCategory("Network Interface");
            var instanceNames = networkCategory.GetInstanceNames();
            var networkCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", instanceNames[0]);
            var diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            var diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            var pagingCounter = new PerformanceCounter("Paging File", "% Usage", "_Total");
            var uptimeCounter = new PerformanceCounter("System", "System Up Time");
            uptimeCounter.NextValue(); // Initialize uptime counter

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{svIP}:{svPort}/");
            listener.Start();

            Console.WriteLine($"Server started, listening on port {svPort}");

            while (true)
            {
                var context = await listener.GetContextAsync();
                var response = context.Response;

                // Initialize counters
                cpuCounter.NextValue();
                networkCounter.NextValue();
                diskReadCounter.NextValue();
                diskWriteCounter.NextValue();
                pagingCounter.NextValue();

                await Task.Delay(svRefreshTime);
                Console.WriteLine("Request received");

                // Gather data
                var cpuUsage = cpuCounter.NextValue();
                var ramAvailable = ramCounter.NextValue();
                var networkUsage = networkCounter.NextValue();
                var diskRead = diskReadCounter.NextValue();
                var diskWrite = diskWriteCounter.NextValue();
                var pagingUsage = pagingCounter.NextValue();
                TimeSpan uptimeSpan = TimeSpan.FromSeconds(uptimeCounter.NextValue());
                var processCount = Process.GetProcesses().Length;
                int threadCount = 0;
                foreach (Process proc in Process.GetProcesses())
                {
                    threadCount += proc.Threads.Count;
                }

                // Build HTML response
                string html = $@"
                <html>
                    <head>
                        <meta http-equiv='refresh' content='5'>
                        <title>Server Monitor</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; }}
                            h1 {{ color: #333; }}
                            p {{ font-size: 1.2em; }}
                        </style>
                    </head>
                    <body>
                        <h1>Server Monitor</h1>
                        <p>CPU Usage: {cpuUsage:N2}%</p>
                        <p>Available Memory: {ramAvailable:N0} MB</p>
                        <p>Network Usage: {networkUsage:N0} Bytes/sec</p>
                        <p>Disk Read: {diskRead:N0} Bytes/sec</p>
                        <p>Disk Write: {diskWrite:N0} Bytes/sec</p>
                        <p>Paging File Usage: {pagingUsage:N2}%</p>
                        <p>System Uptime: {uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s</p>
                        <p>Process Count: {processCount}</p>
                        <p>Thread Count: {threadCount}</p>
                    </body>
                </html>";

                byte[] buffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
        }
    }
}
