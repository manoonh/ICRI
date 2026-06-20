using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using CanIRunIt.Models;

namespace CanIRunIt.Services
{
    public class SystemDetector
    {
        public SystemSpecs DetectSystem()
        {
            var specs = new SystemSpecs();

            // Detect CPU
            DetectCPU(specs);

            // Detect RAM
            DetectRAM(specs);

            // Detect GPU
            DetectGPU(specs);

            // Detect Storage
            DetectStorage(specs);

            // Detect OS
            DetectOS(specs);

            return specs;
        }

        private void DetectCPU(SystemSpecs specs)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    specs.CPUName = obj["Name"]?.ToString() ?? "Unknown";
                    specs.CPUCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    specs.CPUSpeedGHz = Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0;

                    // Extract brand
                    if (specs.CPUName.Contains("Intel", StringComparison.OrdinalIgnoreCase))
                        specs.CPUBrand = "Intel";
                    else if (specs.CPUName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
                        specs.CPUBrand = "AMD";
                    else
                        specs.CPUBrand = "Unknown";

                    // Extract generation for Intel
                    specs.CPUGeneration = ExtractCPUGeneration(specs.CPUName, specs.CPUBrand);
                    break;
                }
            }
            catch (Exception)
            {
                specs.CPUName = "Detection Failed";
            }
        }

        private int ExtractCPUGeneration(string cpuName, string brand)
        {
            // أولاً: البحث عن "Xth Gen" في بداية الاسم (مثل "11th Gen Intel...")
            var genMatch = Regex.Match(cpuName, @"(\d{1,2})(?:th|st|nd|rd)\s*Gen", RegexOptions.IgnoreCase);
            if (genMatch.Success)
            {
                return int.Parse(genMatch.Groups[1].Value);
            }

            if (brand == "Intel")
            {
                // Match patterns like "i5-1135G7" (الجيل 11)
                // الرقم الأول بعد i3/i5/i7/i9 هو الجيل
                var match = Regex.Match(cpuName, @"i[3579]-(\d{1,2})\d{2,3}[A-Z]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }

                // Match patterns like "i7-12700K" or "i5-11400"
                match = Regex.Match(cpuName, @"i[3579]-?(\d{1,2})\d{3}");
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }

                // Try older patterns like "Core i7 4790"
                match = Regex.Match(cpuName, @"i[3579]\s*(\d)\d{3}");
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }

                // Check for specific generation mentions
                if (cpuName.Contains("13th", StringComparison.OrdinalIgnoreCase) || cpuName.Contains("13900") || cpuName.Contains("13700") || cpuName.Contains("13600"))
                    return 13;
                if (cpuName.Contains("12th", StringComparison.OrdinalIgnoreCase) || cpuName.Contains("12900") || cpuName.Contains("12700") || cpuName.Contains("12600") || cpuName.Contains("12400"))
                    return 12;
                if (cpuName.Contains("11th", StringComparison.OrdinalIgnoreCase) || cpuName.Contains("1135") || cpuName.Contains("1165") || cpuName.Contains("11900") || cpuName.Contains("11700") || cpuName.Contains("11600") || cpuName.Contains("11400"))
                    return 11;
                if (cpuName.Contains("10th", StringComparison.OrdinalIgnoreCase) || cpuName.Contains("10900") || cpuName.Contains("10700") || cpuName.Contains("10600") || cpuName.Contains("10400"))
                    return 10;

                // Xeon patterns
                if (cpuName.Contains("Xeon"))
                {
                    return 12; // Assume modern Xeon
                }

                // Pentium Gold
                if (cpuName.Contains("Pentium"))
                {
                    return 8;
                }

                // Celeron
                if (cpuName.Contains("Celeron"))
                {
                    return 7;
                }
            }
            else if (brand == "AMD")
            {
                // Ryzen patterns
                var match = Regex.Match(cpuName, @"Ryzen\s*[3579]\s*(\d)\d{3}");
                if (match.Success)
                {
                    int series = int.Parse(match.Groups[1].Value);
                    return series switch
                    {
                        1 => 7,
                        2 => 8,
                        3 => 9,
                        4 => 10,
                        5 => 11,
                        6 => 12,
                        7 => 13,
                        8 => 14,
                        9 => 14,
                        _ => 10
                    };
                }

                // Ryzen 7000 series
                if (cpuName.Contains("7950") || cpuName.Contains("7900") || cpuName.Contains("7800") || cpuName.Contains("7700") || cpuName.Contains("7600"))
                    return 13;
                // Ryzen 5000 series
                if (cpuName.Contains("5950") || cpuName.Contains("5900") || cpuName.Contains("5800") || cpuName.Contains("5700") || cpuName.Contains("5600") || cpuName.Contains("5500"))
                    return 11;
            }

            return 8; // Default to 8th gen (more reasonable default for modern CPUs)
        }

        private void DetectRAM(SystemSpecs specs)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    double totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"] ?? 0);
                    specs.RAMSizeGB = Math.Round(totalBytes / (1024 * 1024 * 1024), 0);
                    break;
                }
            }
            catch (Exception)
            {
                specs.RAMSizeGB = 0;
            }
        }

        private void DetectGPU(SystemSpecs specs)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    double vram = Convert.ToDouble(obj["AdapterRAM"] ?? 0) / (1024 * 1024 * 1024);

                    // Prefer dedicated GPU
                    bool isDedicated = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                                      name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                      name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                                      name.Contains("RTX", StringComparison.OrdinalIgnoreCase) ||
                                      name.Contains("GTX", StringComparison.OrdinalIgnoreCase);

                    if (isDedicated || string.IsNullOrEmpty(specs.GPUName))
                    {
                        specs.GPUName = name;
                        specs.GPUMemoryGB = vram > 0 ? vram : ExtractVRAMFromName(name);
                        specs.HasDedicatedGPU = isDedicated;
                    }
                }
            }
            catch (Exception)
            {
                specs.GPUName = "Detection Failed";
            }
        }

        private double ExtractVRAMFromName(string gpuName)
        {
            // Try to extract VRAM from GPU name (e.g., "RTX 3060 12GB")
            var match = Regex.Match(gpuName, @"(\d+)\s*GB", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value);
            }

            // Estimate based on known GPU models
            if (gpuName.Contains("4090")) return 24;
            if (gpuName.Contains("4080")) return 16;
            if (gpuName.Contains("4070")) return 12;
            if (gpuName.Contains("4060")) return 8;
            if (gpuName.Contains("3090")) return 24;
            if (gpuName.Contains("3080")) return 10;
            if (gpuName.Contains("3070")) return 8;
            if (gpuName.Contains("3060")) return 12;
            if (gpuName.Contains("3050")) return 4;
            if (gpuName.Contains("2080")) return 8;
            if (gpuName.Contains("2070")) return 8;
            if (gpuName.Contains("2060")) return 6;
            if (gpuName.Contains("1080")) return 8;
            if (gpuName.Contains("1070")) return 8;
            if (gpuName.Contains("1060")) return 6;
            if (gpuName.Contains("1050")) return 4;

            return 2; // Default for integrated
        }

        private void DetectStorage(SystemSpecs specs)
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .ToList();

                specs.TotalStorageGB = drives.Sum(d => d.TotalSize / (1024.0 * 1024 * 1024));
                specs.FreeStorageGB = drives.Sum(d => d.AvailableFreeSpace / (1024.0 * 1024 * 1024));

                // Check for SSD/NVMe
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string mediaType = obj["MediaType"]?.ToString() ?? "";
                    string model = obj["Model"]?.ToString() ?? "";

                    if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                        model.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                    {
                        specs.HasSSD = true;
                    }

                    if (model.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                    {
                        specs.HasNVMe = true;
                    }
                }

                // Additional check via MSFT_PhysicalDisk
                try
                {
                    using var physicalSearcher = new ManagementObjectSearcher(@"\\.\root\microsoft\windows\storage", "SELECT * FROM MSFT_PhysicalDisk");
                    foreach (ManagementObject obj in physicalSearcher.Get())
                    {
                        var mediaType = Convert.ToInt32(obj["MediaType"] ?? 0);
                        // 3 = HDD, 4 = SSD
                        if (mediaType == 4)
                        {
                            specs.HasSSD = true;
                        }
                    }
                }
                catch { }
            }
            catch (Exception)
            {
                specs.FreeStorageGB = 0;
            }
        }

        private void DetectOS(SystemSpecs specs)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    specs.OSName = obj["Caption"]?.ToString() ?? "Unknown";
                    specs.OSVersion = obj["Version"]?.ToString() ?? "Unknown";

                    // Determine Windows version
                    if (specs.OSName.Contains("11"))
                        specs.WindowsVersion = 11;
                    else if (specs.OSName.Contains("10"))
                        specs.WindowsVersion = 10;
                    else
                        specs.WindowsVersion = 10;

                    break;
                }
            }
            catch (Exception)
            {
                specs.OSName = "Detection Failed";
            }
        }
    }
}
