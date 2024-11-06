
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;

namespace PolMon
{
    partial class Program
    {
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
                Console.ReadKey();
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
                Console.ReadKey();
            }
            response.Close();
        }
    }
}