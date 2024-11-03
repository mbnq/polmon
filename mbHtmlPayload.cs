using System;
using System.Globalization;
using System.IO;

namespace polmon
{
    public static class HtmlTemplate
    {
        public static string GetHtml(
            float cpuUsage,
            float ramUsed,
            float totalRam,
            float ramUsagePercent,
            float networkUsage, // Added this parameter
            string networkUsageFormatted,
            string diskReadFormatted,
            string diskWriteFormatted,
            float pagingUsage,
            TimeSpan uptimeSpan,
            int processCount,
            int threadCount)
        {
            // Determine the path to index.html
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFilePath = Path.Combine(exeDirectory, "index.html");

            // Read the HTML content from index.html
            string html = File.ReadAllText(htmlFilePath);

            // Format values using InvariantCulture
            string cpuUsageStr = cpuUsage.ToString("N2", CultureInfo.InvariantCulture);
            string ramUsedStr = ramUsed.ToString("N0", CultureInfo.InvariantCulture);
            string totalRamStr = totalRam.ToString("N0", CultureInfo.InvariantCulture);
            string ramUsagePercentStr = ramUsagePercent.ToString("N2", CultureInfo.InvariantCulture);
            string pagingUsageStr = pagingUsage.ToString("N2", CultureInfo.InvariantCulture);
            string uptimeStr = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s";
            string networkUsageNumericStr = networkUsage.ToString("N0", CultureInfo.InvariantCulture);

            // Replace placeholders with actual values
            html = html.Replace("{{cpuUsage}}", cpuUsageStr)
                       .Replace("{{ramUsed}}", ramUsedStr)
                       .Replace("{{totalRam}}", totalRamStr)
                       .Replace("{{ramUsagePercent}}", ramUsagePercentStr)
                       .Replace("{{networkUsageFormatted}}", networkUsageFormatted)
                       .Replace("{{diskReadFormatted}}", diskReadFormatted)
                       .Replace("{{diskWriteFormatted}}", diskWriteFormatted)
                       .Replace("{{pagingUsage}}", pagingUsageStr)
                       .Replace("{{uptime}}", uptimeStr)
                       .Replace("{{processCount}}", processCount.ToString())
                       .Replace("{{threadCount}}", threadCount.ToString())
                       .Replace("{{networkUsageNumeric}}", networkUsageNumericStr);

            return html;
        }
    }
}
