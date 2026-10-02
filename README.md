# 🇵🇰 Pak Android Bloatware Remover

<p align="center">
  <img src="Assets/app_icon.png" alt="Pak Android Bloatware Remover Logo" width="140" height="140" />
</p>

<p align="center">
  <strong>A modern, lightweight, and safe open-source Android bloatware remover &amp; direct app installer for Windows.</strong><br>
  <em>Clean pre-installed carrier and OEM bloatware without root access in seconds.</em>
</p>

<p align="center">
  <a href="https://github.com/adrees20222/Pak-Android-Bloatware-Remover/releases"><img src="https://img.shields.io/github/v/release/adrees20222/Pak-Android-Bloatware-Remover?style=for-the-badge&color=047857" alt="Release"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-8.0%20%7C%20WPF-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8.0"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-059669?style=for-the-badge" alt="MIT License"></a>
  <a href="https://microsoft.com/windows"><img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows" alt="Windows"></a>
</p>

---

## 🌟 Key Features

- 🟢 **Human-Friendly App Names**: Automatically translates cryptic package IDs (e.g. `com.sec.android.app.sbrowser`) into recognizable names like *Samsung Internet*, *Facebook Services*, *Google Drive*.
- 🛡️ **3-Tier Safety Rating System**:
  - 🟢 **Safe to Remove**: Bloatware, telemetry, analytics, and pre-installed partner apps.
  - 🟡 **Optional / Review**: User preferences (e.g., Chrome, Samsung Notes, Gmail, Calculator).
  - 🔴 **System Core**: Critical Android OS services protected with warning confirmations.
- ⚡ **1-Click Safe Bloatware Cleaning**: Instant **"Select Safe Bloatware"** button to check and clean known bloatware automatically.
- 🔄 **Safe Restore & Reinstall**: Easily restore previously removed system apps back onto your device anytime using the **Restore Apps** tab.
- 🔍 **App Search & Direct Phone Installer**: Search for any app from Google Play / Open Repositories, inspect the Play Store page, and install directly to your phone — **zero Google account or Play Store login required**.
- 📁 **Local APK Drag & Drop**: Drag and drop any `.apk` file into the window to install immediately via ADB.
- 📶 **Wireless ADB (Wi-Fi)**: Connect and manage your phone over Wi-Fi without needing a USB cable.
- ⚙️ **Device Power Tools**: Quick 1-click reboot to Normal, Recovery mode, or Fastboot / Bootloader.
- 📥 **Export Package Profiles**: Export installed app configurations to JSON or text files.
- 🪶 **Ultra Lightweight & Fast**: Built with native C# .NET WPF, launching instantly with minimal memory footprint.

---

## 📱 Supported Manufacturers

- **Samsung** (One UI / TouchWiz)
- **Xiaomi / Redmi / POCO** (MIUI / HyperOS)
- **Google Pixel**
- **OnePlus** (OxygenOS)
- **Oppo / Realme** (ColorOS / Realme UI)
- **Vivo** (Funtouch OS / OriginOS)
- **Motorola / Lenovo / Generic Android**

---

## 🚀 How to Use & Mobile Setup

### Step 1: Enable Developer Options on Your Phone
1. Open your phone's **Settings** app.
2. Scroll down to **About Phone** (or *Software Information*).
3. Find **Build Number** and tap it **7 times quickly** until you see *"You are now a developer!"*.

### Step 2: Turn ON USB Debugging
1. Go back to the main **Settings** menu.
2. Open **Developer Options** (on some phones: *System ➔ Developer Options*).
3. Scroll down and toggle **USB Debugging** **ON**.

### Step 3: Connect to PC
1. Connect your phone to your PC via a USB cable.
2. On your phone screen, check *"Always allow from this computer"* and tap **OK / Allow**.
3. Open **Pak Android Bloatware Remover** and your device will connect automatically!

---

## 📥 Download & Installation

1. Go to the [Releases](https://github.com/adrees20222/Pak-Android-Bloatware-Remover/releases) page.
2. Download **`Pak_Android_Bloatware_Remover_Setup_v1.0.0.exe`**.
3. Run the installer and follow the setup wizard.

> [!NOTE]
> **Windows SmartScreen Notice**: If Windows shows a *"Windows protected your PC"* message on first run, click **More info** ➔ **Run anyway**. This is standard for new open-source software that does not use expensive enterprise digital certificates.

---

## 🛠️ Building from Source

### Prerequisites
- Windows 10 / 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 or VS Code (optional)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (for compiling setup installer)

### Build Commands
```bash
# Clone repository
git clone https://github.com/adrees20222/Pak-Android-Bloatware-Remover.git
cd Pak-Android-Bloatware-Remover/AndroidDebloater

# Build in Release mode
dotnet build -c Release

# Compile setup installer (.exe)
build_installer.bat
```

---

## 👨‍💻 Developer & Credits

Developed with ❤️ by **Muhammad Adrees**

- 🌐 **Portfolio**: [adrees2022.blogspot.com](https://adrees2022.blogspot.com/)
- 💬 **Support**: [Get Support](https://my-extension.blogspot.com/p/support.html)
- ☕ **Donate**: [Support the Project](https://my-extension.blogspot.com/p/donate.html)
- 📜 **Terms of Service**: [Terms](https://my-extension.blogspot.com/p/terms.html)
- 🔒 **Privacy Policy**: [Privacy](https://my-extension.blogspot.com/p/privacy-policy_15.html)

---

## 📄 License
This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.
