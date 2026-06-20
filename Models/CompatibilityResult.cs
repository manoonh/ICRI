namespace CanIRunIt.Models
{
    public enum CompatibilityStatus
    {
        CannotRun,      // ❌ لا يعمل
        MinimumMet,     // ⚠️ يعمل بالحد الأدنى
        RecommendedMet  // ✅ يعمل بشكل ممتاز
    }

    public class CompatibilityResult
    {
        public SoftwareRequirements Software { get; set; } = new SoftwareRequirements();
        public CompatibilityStatus Status { get; set; }
        public int CompatibilityScore { get; set; } // 0-100%

        // تفاصيل كل مكون
        public ComponentCheck CPUCheck { get; set; } = new ComponentCheck();
        public ComponentCheck RAMCheck { get; set; } = new ComponentCheck();
        public ComponentCheck GPUCheck { get; set; } = new ComponentCheck();
        public ComponentCheck StorageCheck { get; set; } = new ComponentCheck();
        public ComponentCheck OSCheck { get; set; } = new ComponentCheck();

        public string StatusEmoji => Status switch
        {
            CompatibilityStatus.CannotRun => "❌",
            CompatibilityStatus.MinimumMet => "⚠️",
            CompatibilityStatus.RecommendedMet => "✅",
            _ => "❓"
        };

        public string StatusText => Status switch
        {
            CompatibilityStatus.CannotRun => "لا يمكن التشغيل",
            CompatibilityStatus.MinimumMet => "يعمل بالحد الأدنى",
            CompatibilityStatus.RecommendedMet => "يعمل بشكل ممتاز",
            _ => "غير معروف"
        };

        public string StatusColor => Status switch
        {
            CompatibilityStatus.CannotRun => "#FDE8E8", // Light Red bg
            CompatibilityStatus.MinimumMet => "#FDF6B2", // Light Yellow bg
            CompatibilityStatus.RecommendedMet => "#DEF7EC", // Light Green bg
            _ => "#F3F4F6"
        };

        public string StatusColorText => Status switch
        {
            CompatibilityStatus.CannotRun => "#9B1C1C", // Dark Red text
            CompatibilityStatus.MinimumMet => "#723B13", // Dark Yellow text
            CompatibilityStatus.RecommendedMet => "#03543F", // Dark Green text
            _ => "#374151"
        };
    }

    public class ComponentCheck
    {
        public string ComponentName { get; set; } = string.Empty;
        public string YourSpec { get; set; } = string.Empty;
        public string MinRequired { get; set; } = string.Empty;
        public string Recommended { get; set; } = string.Empty;
        public bool MeetsMinimum { get; set; }
        public bool MeetsRecommended { get; set; }
        public string Suggestion { get; set; } = string.Empty;

        public string StatusEmoji => MeetsRecommended ? "✅" : (MeetsMinimum ? "⚠️" : "❌");
    }
}
