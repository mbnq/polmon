
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

*/

namespace PolMon
{
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
        public string svMachineName { get; set; }
        public float svTestVar2 { get; set; }
        public float svCPUTemp { get; set; }
        public float svGPUTemp { get; set; }
        public float svGPULoad { get; set; }
        public float svFANAvgSpeed { get; set; }
        public float svGPUFanSpeed { get; set; }
    }
}
