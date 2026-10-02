namespace AndroidDebloater.Models
{
    public class DeviceInfo
    {
        public string Serial { get; set; } = string.Empty;
        public string Model { get; set; } = "Unknown";
        public string Manufacturer { get; set; } = "Unknown";
        public string AndroidVersion { get; set; } = "Unknown";
        public string BatteryLevel { get; set; } = "--";
        public bool IsConnected { get; set; }

        public string DisplayName => IsConnected
            ? $"{Manufacturer} {Model} ({Serial})"
            : "No Android Device Connected";
    }
}
