﻿
/* 

    www.mbnq.pl 2024 
    https://mbnq.pl/
    mbnq00 on gmail

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
        static float GetCPUTemperature()
        {
            var temperatures = new List<float>();

            foreach (var hardwareItem in computer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();

                    // Check sensors directly under the CPU hardware item
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            temperatures.Add(sensor.Value.Value);
                        }
                    }

                    // Traverse sub-hardware
                    foreach (var subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();

                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                temperatures.Add(sensor.Value.Value);
                            }
                        }
                    }
                }
            }

            return temperatures.Count > 0 ? temperatures.Average() : 0;
        }
        static float GetGPUTemperature()
        {
            var temperatures = new List<float>();

            foreach (var hardwareItem in computer.Hardware)
            {
                if ((hardwareItem.HardwareType == HardwareType.GpuNvidia) || (hardwareItem.HardwareType == HardwareType.GpuAti))
                {
                    hardwareItem.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                        {
                            temperatures.Add(sensor.Value.Value);
                        }
                    }
                    // Traverse sub-hardware
                    foreach (var subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();

                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                            {
                                temperatures.Add(sensor.Value.Value);
                            }
                        }
                    }
                }
            }

            return temperatures.Count > 0 ? temperatures.Average() : 0;
        }
        static float GetGPULoad()
        {
            var loads = new List<float>();

            foreach (var hardwareItem in computer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.GpuNvidia || hardwareItem.HardwareType == HardwareType.GpuAti)
                {
                    hardwareItem.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                        {
                            if (sensor.Name.Equals("GPU Core", StringComparison.InvariantCultureIgnoreCase))
                            {
                                loads.Add(sensor.Value.Value);
                            }
                        }
                    }

                    foreach (var subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();

                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            {
                                if (sensor.Name.Equals("GPU Core", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    loads.Add(sensor.Value.Value);
                                }
                            }
                        }
                    }
                }
            }

            if (loads.Count <= 0) return 0;
            // Calculate and return the average GPU load
            return loads.Count > 0 ? loads.Average() : 0;
        }
        static float GetCPULoad()
        {
            var loads = new List<float>();

            foreach (var hardwareItem in computer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.CPU)
                {
                    hardwareItem.Update();

                    // Check the main hardware item for CPU load
                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        // Console.WriteLine(sensor.Name);
                        if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                        {
                            if (sensor.Name.Equals("CPU Total", StringComparison.InvariantCultureIgnoreCase))
                            {
                                loads.Add(sensor.Value.Value);
                            }
                        }
                    }

                    // Check any sub-hardware for CPU load
                    foreach (var subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();

                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            {
                                if (sensor.Name.Equals("CPU Total", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    loads.Add(sensor.Value.Value);
                                }
                            }
                        }
                    }
                }
            }

            // Calculate and return the average CPU load
            return loads.Count > 0 ? loads.Average() : 0;
        }
        static float GetFansAvgSpeed()
        {
            var fans = new List<float>();

            foreach (var hardwareItem in computer.Hardware)
            {
                if (hardwareItem.HardwareType == HardwareType.Mainboard)
                {
                    hardwareItem.Update();

                    foreach (var sensor in hardwareItem.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                        {
                            fans.Add(sensor.Value.Value);
                        }
                    }

                    foreach (var subHardware in hardwareItem.SubHardware)
                    {
                        subHardware.Update();

                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Fan && sensor.Value.HasValue)
                            {
                                fans.Add(sensor.Value.Value);
                            }
                        }
                    }
                }
            }
            return fans.Count > 0 ? fans.Average() : 0;
        }
    }
}
