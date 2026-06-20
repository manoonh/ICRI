using System;
using System.Text.RegularExpressions;
using CanIRunIt.Models;

namespace CanIRunIt.Services
{
    public class CompatibilityChecker
    {
        private readonly SystemSpecs _specs;

        public CompatibilityChecker(SystemSpecs specs)
        {
            _specs = specs;
        }

        public CompatibilityResult CheckCompatibility(SoftwareRequirements software)
        {
            var result = new CompatibilityResult
            {
                Software = software
            };

            // Check each component
            result.CPUCheck = CheckCPU(software);
            result.RAMCheck = CheckRAM(software);
            result.GPUCheck = CheckGPU(software);
            result.StorageCheck = CheckStorage(software);
            result.OSCheck = CheckOS(software);

            // Calculate overall status and score
            CalculateOverallStatus(result);

            return result;
        }

        private ComponentCheck CheckCPU(SoftwareRequirements software)
        {
            var check = new ComponentCheck
            {
                ComponentName = "المعالج (CPU)",
                YourSpec = _specs.CPUName,
                MinRequired = software.Minimum.CPU,
                Recommended = software.Recommended.CPU
            };

            int minGen = ExtractGeneration(software.Minimum.CPU);
            int recGen = ExtractGeneration(software.Recommended.CPU);

            // أجعل الفحص أكثر تساهلاً - المعالج الحديث أقوى بكثير
            // إذا الجيل أعلى بـ 2 من المطلوب، يكون ممتاز
            check.MeetsMinimum = _specs.CPUGeneration >= minGen - 2 || _specs.CPUCores >= 4;
            check.MeetsRecommended = _specs.CPUGeneration >= recGen - 1 || _specs.CPUCores >= 6;

            // إذا كان المعالج Core i5 أو أعلى، غالباً كافي
            if (_specs.CPUName.Contains("i5") || _specs.CPUName.Contains("i7") || 
                _specs.CPUName.Contains("i9") || _specs.CPUName.Contains("Ryzen"))
            {
                check.MeetsMinimum = true;
                if (_specs.CPUGeneration >= 8) // الجيل 8 وأحدث ممتاز لأغلب البرامج
                {
                    check.MeetsRecommended = true;
                }
            }

            if (!check.MeetsMinimum)
            {
                check.Suggestion = $"تحتاج ترقية المعالج إلى الجيل {minGen} على الأقل";
            }
            else if (!check.MeetsRecommended)
            {
                check.Suggestion = $"للأداء الأمثل، يُنصح بمعالج من الجيل {recGen}";
            }

            return check;
        }

        private ComponentCheck CheckRAM(SoftwareRequirements software)
        {
            var check = new ComponentCheck
            {
                ComponentName = "الذاكرة (RAM)",
                YourSpec = _specs.FormattedRAM,
                MinRequired = software.Minimum.RAM,
                Recommended = software.Recommended.RAM
            };

            double minRam = ExtractNumber(software.Minimum.RAM);
            double recRam = ExtractNumber(software.Recommended.RAM);

            // RAM check - تساهل بسيط (90% من المطلوب)
            check.MeetsMinimum = _specs.RAMSizeGB >= minRam * 0.9;
            check.MeetsRecommended = _specs.RAMSizeGB >= recRam * 0.9;

            if (!check.MeetsMinimum)
            {
                check.Suggestion = $"تحتاج إلى {minRam} GB RAM على الأقل";
            }
            else if (!check.MeetsRecommended)
            {
                check.Suggestion = $"للأداء الأمثل، يُنصح بـ {recRam} GB RAM";
            }

            return check;
        }

        private ComponentCheck CheckGPU(SoftwareRequirements software)
        {
            var check = new ComponentCheck
            {
                ComponentName = "كرت الشاشة (GPU)",
                YourSpec = $"{_specs.GPUName} ({_specs.FormattedGPUMemory})",
                MinRequired = software.Minimum.GPU,
                Recommended = software.Recommended.GPU
            };

            // Handle N/A or Integrated cases - الكثير من البرامج لا تحتاج GPU
            if (software.Minimum.GPU.Contains("N/A", StringComparison.OrdinalIgnoreCase) ||
                software.Minimum.GPU.Contains("Integrated", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(software.Minimum.GPU))
            {
                check.MeetsMinimum = true;
                check.MeetsRecommended = true;
                return check;
            }

            double minVram = ExtractVRAM(software.Minimum.GPU);
            double recVram = ExtractVRAM(software.Recommended.GPU);

            // GPU check - تساهل أكبر
            check.MeetsMinimum = _specs.GPUMemoryGB >= minVram * 0.8 || 
                                 _specs.HasDedicatedGPU || 
                                 minVram <= 2;
            
            check.MeetsRecommended = _specs.GPUMemoryGB >= recVram * 0.8 || 
                                     CheckGPUModel(software.Recommended.GPU) ||
                                     CheckGPUModel(_specs.GPUName);

            // إذا كان عنده كرت مخصص، غالباً كافي للحد الأدنى
            if (_specs.HasDedicatedGPU)
            {
                check.MeetsMinimum = true;
            }

            if (!check.MeetsMinimum)
            {
                check.Suggestion = $"تحتاج كرت شاشة بذاكرة {minVram} GB على الأقل";
            }
            else if (!check.MeetsRecommended)
            {
                check.Suggestion = $"للأداء الأمثل، يُنصح بـ {software.Recommended.GPU}";
            }

            return check;
        }

        private bool CheckGPUModel(string gpuSpec)
        {
            // Check if GPU is a good/modern model
            string[] goodGPUs = { 
                "4090", "4080", "4070", "4060", "4050",
                "3090", "3080", "3070", "3060", "3050",
                "2080", "2070", "2060",
                "1080", "1070", "1060", "1050",
                "RX 7", "RX 6", "RX 5",
                "Radeon RX",
                "GTX", "RTX"
            };
            foreach (var gpu in goodGPUs)
            {
                if (gpuSpec.Contains(gpu, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private ComponentCheck CheckStorage(SoftwareRequirements software)
        {
            var check = new ComponentCheck
            {
                ComponentName = "التخزين",
                YourSpec = $"{_specs.FormattedFreeStorage} متاح ({(_specs.HasNVMe ? "NVMe" : _specs.HasSSD ? "SSD" : "HDD")})",
                MinRequired = software.Minimum.Storage,
                Recommended = software.Recommended.Storage
            };

            double minStorage = ExtractNumber(software.Minimum.Storage);
            double recStorage = ExtractNumber(software.Recommended.Storage);

            // Convert MB to GB if needed
            if (software.Minimum.Storage.Contains("MB", StringComparison.OrdinalIgnoreCase))
                minStorage /= 1024;
            if (software.Recommended.Storage.Contains("MB", StringComparison.OrdinalIgnoreCase))
                recStorage /= 1024;

            if (minStorage < 1) minStorage = 1;
            if (recStorage < 1) recStorage = 1;

            // Storage check - دائماً تقريباً كافي إذا عندك مساحة
            check.MeetsMinimum = _specs.FreeStorageGB >= minStorage;
            check.MeetsRecommended = _specs.FreeStorageGB >= recStorage;

            // SSD/NVMe requirement - لا تمنع التشغيل، فقط تؤثر على الأداء
            bool needsSSD = software.Recommended.Storage.Contains("SSD", StringComparison.OrdinalIgnoreCase);
            bool needsNVMe = software.Recommended.Storage.Contains("NVMe", StringComparison.OrdinalIgnoreCase);

            if ((needsNVMe && !_specs.HasNVMe) || (needsSSD && !_specs.HasSSD))
            {
                // لا تغير MeetsMinimum - البرنامج يشتغل بس أبطأ
                if (check.MeetsRecommended && !_specs.HasSSD)
                {
                    check.Suggestion = "يُنصح بـ SSD للأداء الأمثل";
                }
            }

            if (!check.MeetsMinimum)
            {
                check.Suggestion = $"تحتاج إلى {minStorage:F0} GB مساحة فارغة على الأقل";
            }

            return check;
        }

        private ComponentCheck CheckOS(SoftwareRequirements software)
        {
            var check = new ComponentCheck
            {
                ComponentName = "نظام التشغيل",
                YourSpec = _specs.OSName,
                MinRequired = software.Minimum.OS,
                Recommended = software.Recommended.OS
            };

            int minWin = ExtractWindowsVersion(software.Minimum.OS);
            int recWin = ExtractWindowsVersion(software.Recommended.OS);

            // Windows 10/11 - أغلب البرامج تشتغل على الاثنين
            check.MeetsMinimum = _specs.WindowsVersion >= minWin;
            check.MeetsRecommended = _specs.WindowsVersion >= recWin;

            // Windows 10 يشغل كل شيء تقريباً
            if (_specs.WindowsVersion >= 10)
            {
                check.MeetsMinimum = true;
            }

            if (!check.MeetsMinimum)
            {
                check.Suggestion = $"تحتاج Windows {minWin} على الأقل";
            }
            else if (!check.MeetsRecommended)
            {
                check.Suggestion = $"يُنصح بالترقية إلى Windows {recWin}";
            }

            return check;
        }

        private void CalculateOverallStatus(CompatibilityResult result)
        {
            var checks = new[] 
            { 
                result.CPUCheck, 
                result.RAMCheck, 
                result.GPUCheck, 
                result.StorageCheck, 
                result.OSCheck 
            };

            int minimumMetCount = 0;
            int recommendedMetCount = 0;
            int score = 0;

            foreach (var check in checks)
            {
                if (check.MeetsMinimum)
                    minimumMetCount++;
                if (check.MeetsRecommended)
                    recommendedMetCount++;

                // Score calculation: 20 points per component
                if (check.MeetsRecommended)
                    score += 20;
                else if (check.MeetsMinimum)
                    score += 12;
                else
                    score += 3; // حتى لو فشل، نعطي نقاط بسيطة
            }

            result.CompatibilityScore = score;

            // تحديد الحالة - أكثر تساهلاً
            // إذا 4 من 5 مكونات كافية = يعمل
            if (recommendedMetCount >= 4)
                result.Status = CompatibilityStatus.RecommendedMet;
            else if (minimumMetCount >= 3)
                result.Status = CompatibilityStatus.MinimumMet;
            else
                result.Status = CompatibilityStatus.CannotRun;
        }

        private int ExtractGeneration(string cpuSpec)
        {
            // Match patterns like "i7 12th Gen" or "i5-11400"
            var match = Regex.Match(cpuSpec, @"(\d{1,2})(?:th|st|nd|rd)?\s*Gen", RegexOptions.IgnoreCase);
            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            // Match "i7-12700K" pattern
            match = Regex.Match(cpuSpec, @"i[3579]-?(\d{1,2})\d{3}");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            // Match "Core i7 4790" pattern (older CPUs)
            match = Regex.Match(cpuSpec, @"i[3579]\s+(\d)\d{3}");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);

            // Match AMD Ryzen
            match = Regex.Match(cpuSpec, @"Ryzen\s*[3579]\s*(\d)\d{3}");
            if (match.Success)
            {
                int series = int.Parse(match.Groups[1].Value);
                return series switch
                {
                    1 => 7, 2 => 8, 3 => 9, 4 => 10, 5 => 11, 6 => 12, 7 => 13, _ => 10
                };
            }

            // Default patterns
            if (cpuSpec.Contains("Xeon") || cpuSpec.Contains("13900") || cpuSpec.Contains("13th"))
                return 13;
            if (cpuSpec.Contains("12th") || cpuSpec.Contains("12900") || cpuSpec.Contains("12700"))
                return 12;
            if (cpuSpec.Contains("11th") || cpuSpec.Contains("11900") || cpuSpec.Contains("11700"))
                return 11;
            if (cpuSpec.Contains("10th") || cpuSpec.Contains("10900") || cpuSpec.Contains("10700"))
                return 10;
            if (cpuSpec.Contains("Pentium"))
                return 7;
            if (cpuSpec.Contains("Core 2"))
                return 2;

            return 6; // Default
        }

        private double ExtractNumber(string spec)
        {
            var match = Regex.Match(spec, @"(\d+(?:\.\d+)?)");
            if (match.Success)
                return double.Parse(match.Groups[1].Value);
            return 0;
        }

        private double ExtractVRAM(string gpuSpec)
        {
            var match = Regex.Match(gpuSpec, @"(\d+(?:\.\d+)?)\s*GB", RegexOptions.IgnoreCase);
            if (match.Success)
                return double.Parse(match.Groups[1].Value);

            // Extract from parentheses like "(4 GB)"
            match = Regex.Match(gpuSpec, @"\((\d+(?:\.\d+)?)\s*GB\)", RegexOptions.IgnoreCase);
            if (match.Success)
                return double.Parse(match.Groups[1].Value);

            // Default for DirectX/OpenGL mentions
            if (gpuSpec.Contains("DirectX") || gpuSpec.Contains("OpenGL"))
                return 1;

            return 1; // Default - أي كرت يكفي
        }

        private int ExtractWindowsVersion(string osSpec)
        {
            if (osSpec.Contains("11"))
                return 11;
            if (osSpec.Contains("10"))
                return 10;
            return 10;
        }
    }
}
