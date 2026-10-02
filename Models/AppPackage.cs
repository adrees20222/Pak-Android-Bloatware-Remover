using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AndroidDebloater.Models
{
    public enum SafetyLevel
    {
        Safe,       // 🟢 Safe to remove (Bloatware, analytics, adware, unused pre-installed social apps)
        Optional,   // 🟡 User preference (Apps like Chrome, Gmail, Calculator, Notes)
        Caution     // 🔴 Critical system component (Removing may cause bootloops or instability)
    }

    public class AppPackage : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isInstalled = true;

        public string PackageName { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = "Universal";
        public string ApkPath { get; set; } = string.Empty;
        public SafetyLevel Safety { get; set; } = SafetyLevel.Optional;

        public bool IsInstalled
        {
            get => _isInstalled;
            set { _isInstalled = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string SafetyBadgeText => Safety switch
        {
            SafetyLevel.Safe => "Safe",
            SafetyLevel.Optional => "Optional",
            SafetyLevel.Caution => "System Core",
            _ => "Unknown"
        };

        public string SafetyBadgeColor => Safety switch
        {
            SafetyLevel.Safe => "#15803D",     // Forest Green
            SafetyLevel.Optional => "#B45309", // Warm Amber
            SafetyLevel.Caution => "#B91C1C",  // Crimson Red
            _ => "#475569"
        };

        public string SafetyBadgeBackground => Safety switch
        {
            SafetyLevel.Safe => "#DCFCE7",     // Soft Mint Green
            SafetyLevel.Optional => "#FEF3C7", // Soft Light Yellow
            SafetyLevel.Caution => "#FEE2E2",  // Soft Light Rose
            _ => "#F1F5F9"
        };

        public string SafetyBadgeBorder => Safety switch
        {
            SafetyLevel.Safe => "#86EFAC",
            SafetyLevel.Optional => "#FCD34D",
            SafetyLevel.Caution => "#FCA5A5",
            _ => "#CBD5E1"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
