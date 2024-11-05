
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

using System;
using System.Globalization;
using System.IO;

namespace PolMon
{
    public static class HtmlBuilder
    {
        public static string BuildHtmlContent(PerformanceData data)
        {
            // Determine the path to index.html
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFilePath = Path.Combine(exeDirectory, "index.html");

            // Read the HTML content from index.html
            string html = File.ReadAllText(htmlFilePath);

            // Format values
            string cpuUsageStr = data.cpuUsage.ToString("F2", CultureInfo.InvariantCulture);
            string ramUsedStr = data.ramUsed.ToString("F0", CultureInfo.InvariantCulture);
            string totalRamStr = data.totalRam.ToString("F0", CultureInfo.InvariantCulture);
            string ramUsagePercentStr = data.ramUsagePercent.ToString("F2", CultureInfo.InvariantCulture);
            string pagingUsageStr = data.pagingUsage.ToString("F2", CultureInfo.InvariantCulture);
            string uptimeStr = data.uptime;
            string networkUsageNumericStr = data.networkUsage.ToString("F2", CultureInfo.InvariantCulture);
            string networkUsageFormatted = FormatBytes(data.networkUsage);
            string processCountStr = data.processCount.ToString(CultureInfo.InvariantCulture);
            string threadCountStr = data.threadCount.ToString(CultureInfo.InvariantCulture);
            string svTestVarStr = data.svMachineName;
            string svTestVar2Str = data.svTestVar2.ToString();

            // Replace placeholders with actual values
            html = html.Replace("{{cpuUsage}}", cpuUsageStr)
                       .Replace("{{ramUsed}}", ramUsedStr)
                       .Replace("{{totalRam}}", totalRamStr)
                       .Replace("{{ramUsagePercent}}", ramUsagePercentStr)
                       .Replace("{{networkUsageFormatted}}", networkUsageFormatted)
                       .Replace("{{diskReadFormatted}}", data.diskReadFormatted)
                       .Replace("{{diskWriteFormatted}}", data.diskWriteFormatted)
                       .Replace("{{pagingUsage}}", pagingUsageStr)
                       .Replace("{{uptime}}", uptimeStr)
                       .Replace("{{processCount}}", processCountStr)
                       .Replace("{{threadCount}}", threadCountStr)
                       .Replace("{{networkUsageNumeric}}", networkUsageNumericStr)
                       .Replace("{{svTestVarStr}}", svTestVarStr)
                       .Replace("{{svTestVar2Str}}", svTestVar2Str);

            return html;
        }

        public static string FormatBytes(float bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }
    }
}
