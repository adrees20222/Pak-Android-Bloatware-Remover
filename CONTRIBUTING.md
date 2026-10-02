# Contributing to Pak Android Bloatware Remover

Thank you for your interest in contributing to **Pak Android Bloatware Remover**!

## Ways to Contribute
1. **Adding Bloatware Packages**:
   Help expand the database in `Services/DatabaseService.cs` by adding new OEM bloatware packages for Samsung, Xiaomi, OnePlus, Realme, Vivo, Motorola, etc.
2. **Reporting Bugs**:
   Submit an issue describing the problem, your Android version, and device model.
3. **Feature Suggestions**:
   Open a discussion or feature request issue.

## Pull Request Guidelines
- Ensure the project builds cleanly without errors: `dotnet build -c Release`
- Keep code formatted cleanly using Segoe UI and WPF MVVM/Code-behind conventions.
- Test debloating and restoring on real or emulated devices before submitting.
