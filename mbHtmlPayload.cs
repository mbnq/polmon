using System;
using System.Globalization;

namespace polmon
{
    public static class HtmlTemplate
    {
        public static string GetHtml(
            float cpuUsage,
            float ramUsed,
            float totalRam,
            float ramUsagePercent,
            string networkUsageFormatted,
            string diskReadFormatted,
            string diskWriteFormatted,
            float pagingUsage,
            TimeSpan uptimeSpan,
            int processCount,
            int threadCount)
        {
            string html = $@"
            <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta http-equiv='refresh' content='5'>
                    <title>Server Monitor</title>
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            background-color: #f0f0f0;
                            margin: 0;
                            padding: 0;
                        }}
                        h1 {{
                            color: #333;
                            text-align: center;
                            padding: 20px;
                        }}
                        .container {{
                            width: 90%;
                            margin: auto;
                        }}
                        .gauges {{
                            display: flex;
                            justify-content: space-around;
                            flex-wrap: wrap;
                        }}
                        .gauge {{
                            width: 200px;
                            height: 200px;
                            position: relative;
                            margin: 20px;
                        }}
                        .gauge canvas {{
                            width: 100%;
                            height: 100%;
                        }}
                        .gauge-value {{
                            position: absolute;
                            top: 50%;
                            left: 50%;
                            transform: translate(-50%, -50%);
                            text-align: center;
                            font-size: 24px;
                            font-weight: bold;
                        }}
                        .info {{
                            margin: 20px;
                            background-color: #fff;
                            padding: 20px;
                            border-radius: 5px;
                            box-shadow: 0 0 10px rgba(0,0,0,0.1);
                        }}
                        .info p {{
                            font-size: 1.1em;
                            margin: 10px 0;
                        }}
                    </style>
                </head>
                <body>
                    <h1>Server Monitor</h1>
                    <div class='container'>
                        <div class='gauges'>
                            <div class='gauge' id='cpuGauge'>
                                <canvas id='cpuCanvas'></canvas>
                                <div class='gauge-value'>{cpuUsage.ToString("N2", CultureInfo.InvariantCulture)}%</div>
                            </div>
                            <div class='gauge' id='ramGauge'>
                                <canvas id='ramCanvas'></canvas>
                                <div class='gauge-value'>{ramUsagePercent.ToString("N2", CultureInfo.InvariantCulture)}%</div>
                            </div>
                            <div class='gauge' id='networkGauge'>
                                <canvas id='networkCanvas'></canvas>
                                <div class='gauge-value'>{networkUsageFormatted}/s</div>
                            </div>
                        </div>
                        <div class='info'>
                            <p><strong>CPU Usage:</strong> {cpuUsage.ToString("N2", CultureInfo.InvariantCulture)}%</p>
                            <p><strong>RAM Usage:</strong> {ramUsed.ToString("N0", CultureInfo.InvariantCulture)} MB / {totalRam.ToString("N0", CultureInfo.InvariantCulture)} MB ({ramUsagePercent.ToString("N2", CultureInfo.InvariantCulture)}%)</p>
                            <p><strong>Network Usage:</strong> {networkUsageFormatted}/s</p>
                            <p><strong>Disk Read:</strong> {diskReadFormatted}/s</p>
                            <p><strong>Disk Write:</strong> {diskWriteFormatted}/s</p>
                            <p><strong>Paging File Usage:</strong> {pagingUsage.ToString("N2", CultureInfo.InvariantCulture)}%</p>
                            <p><strong>System Uptime:</strong> {uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s</p>
                            <p><strong>Process Count:</strong> {processCount}</p>
                            <p><strong>Thread Count:</strong> {threadCount}</p>
                        </div>
                    </div>
                    <script>
                        function drawGauge(canvasId, value, maxValue, color) {{
                            var canvas = document.getElementById(canvasId);
                            var context = canvas.getContext('2d');
                            var centerX = canvas.width / 2;
                            var centerY = canvas.height / 2;
                            var radius = (canvas.width / 2) - 10;
                            var startAngle = 0.75 * Math.PI;
                            var endAngle = 2.25 * Math.PI;
                            var angle = startAngle + (value / maxValue) * (endAngle - startAngle);

                            // Background circle
                            context.beginPath();
                            context.arc(centerX, centerY, radius, startAngle, endAngle, false);
                            context.lineWidth = 15;
                            context.strokeStyle = '#eee';
                            context.stroke();

                            // Value arc
                            context.beginPath();
                            context.arc(centerX, centerY, radius, startAngle, angle, false);
                            context.lineWidth = 15;
                            context.strokeStyle = color;
                            context.stroke();
                        }}

                        // Draw gauges
                        drawGauge('cpuCanvas', {cpuUsage.ToString("N2", CultureInfo.InvariantCulture)}, 100, '#FF6347');
                        drawGauge('ramCanvas', {ramUsagePercent.ToString("N2", CultureInfo.InvariantCulture)}, 100, '#4682B4');
                        drawGauge('networkCanvas', {{networkUsageFormattedNumeric}}, GetMaxNetworkSpeed(), '#3CB371');

                        function GetMaxNetworkSpeed() {{
                            return 100000000; // Adjust this value based on your network interface speed
                        }}
                    </script>
                </body>
            </html>";

            return html;
        }
    }
}
