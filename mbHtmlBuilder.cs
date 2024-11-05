
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

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
            float networkUsage, // Ensure this parameter is present
            string networkUsageFormatted,
            string diskReadFormatted,
            string diskWriteFormatted,
            float pagingUsage,
            TimeSpan uptimeSpan,
            int processCount,
            int threadCount,
            string svTestVar
         )
        {
            // Determine the path to index.html
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string htmlFilePath = Path.Combine(exeDirectory, "index.html");

            // Read the HTML content from index.html
            string html = File.ReadAllText(htmlFilePath);

            // Format values
            string cpuUsageStr = cpuUsage.ToString("F2", CultureInfo.InvariantCulture);
            string ramUsedStr = ramUsed.ToString("F0", CultureInfo.InvariantCulture);
            string totalRamStr = totalRam.ToString("F0", CultureInfo.InvariantCulture);
            string ramUsagePercentStr = ramUsagePercent.ToString("F2", CultureInfo.InvariantCulture);
            string pagingUsageStr = pagingUsage.ToString("F2", CultureInfo.InvariantCulture);
            string uptimeStr = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s";
            string networkUsageNumericStr = networkUsage.ToString("F2", CultureInfo.InvariantCulture);
            string processCountStr = processCount.ToString(CultureInfo.InvariantCulture);
            string threadCountStr = threadCount.ToString(CultureInfo.InvariantCulture);
            string svTestVarStr = svTestVar;

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
                       .Replace("{{processCount}}", processCountStr)
                       .Replace("{{threadCount}}", threadCountStr)
                       .Replace("{{networkUsageNumeric}}", networkUsageNumericStr)
                       .Replace("{{svTestVarStr}}", svTestVarStr)
                       ;

            return html;
        }
    }
}
