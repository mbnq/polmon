using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace polmon
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            var networkCategory = new PerformanceCounterCategory("Network Interface");
            var instanceNames = networkCategory.GetInstanceNames();
            var networkCounter = new PerformanceCounter("Network Interface", "Bytes Total/sec", instanceNames[0]);

            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Console.WriteLine("Server started, listening on port 8080...");

            while (true)
            {
                var context = await listener.GetContextAsync();
                var response = context.Response;

                // First call returns 0, so call twice
                cpuCounter.NextValue();
                networkCounter.NextValue();
                await Task.Delay(1000);

                // Gather data
                var cpuUsage = cpuCounter.NextValue();
                var ramAvailable = ramCounter.NextValue();
                var networkUsage = networkCounter.NextValue();

                // Build HTML response
                string html = $@"
                <html>
                    <head>
                        <meta http-equiv='refresh' content='5'>
                        <title>Server Monitor</title>
                    </head>
                    <body>
                        <h1>Server Monitor</h1>
                        <p>CPU Usage: {cpuUsage}%</p>
                        <p>Available Memory: {ramAvailable} MB</p>
                        <p>Network Usage: {networkUsage} Bytes/sec</p>
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
