namespace CanIRunIt.Models
{
    public class SystemSpecs
    {
        public string CPUName { get; set; } = string.Empty;
        public int CPUCores { get; set; }
        public double CPUSpeedGHz { get; set; }
        public int CPUGeneration { get; set; }
        public string CPUBrand { get; set; } = string.Empty;

        public double RAMSizeGB { get; set; }

        public string GPUName { get; set; } = string.Empty;
        public double GPUMemoryGB { get; set; }
        public bool HasDedicatedGPU { get; set; }

        public double TotalStorageGB { get; set; }
        public double FreeStorageGB { get; set; }
        public bool HasSSD { get; set; }
        public bool HasNVMe { get; set; }

        public string OSName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public int WindowsVersion { get; set; }

        public string FormattedRAM => $"{RAMSizeGB:F0} GB";
        public string FormattedGPUMemory => $"{GPUMemoryGB:F1} GB";
        public string FormattedFreeStorage => $"{FreeStorageGB:F1} GB";
    }
}
