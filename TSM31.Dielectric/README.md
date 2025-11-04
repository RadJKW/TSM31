# TestStation Template

.NET project template for creating Test Station applications with Blazor Web UI, WinForms desktop host, and shared business logic.

## Usage

Create a new Test Station application:

```bash
dotnet new teststation -o MyTestStation
cd MyTestStation
dotnet build
```

### Template-Specific Options

- **Target Framework**: .NET 9.0 (default)
- **Project Naming**: Use alphanumeric characters without hyphens or special characters
  - ✅ Good: `TestStation`, `MyApp`, `Station1`
  - ❌ Avoid: `My-Test-Station`, `Test_Station`

### Examples

Basic usage:
```bash
dotnet new teststation -o MyStation
```

Skip automatic restore:
```bash
dotnet new teststation -o MyStation --no-restore
cd MyStation
dotnet restore
```

---

## Getting Started

### 1. Install Node.js Dependencies

**Important:** Before running the application, you must install the required npm packages for TailwindCSS:

```bash
cd src/TSM31.Dielectric.App
npm install
```

This will install TailwindCSS and its dependencies required for styling the application.

### 2. Running the Applications

After installing npm dependencies, you can run either the Web or WinForms application:

#### Blazor Web Application

```bash
cd src/TSM31.Dielectric.Web
dotnet run
```

Then open your browser to: `https://localhost:5001` or `http://localhost:5000`

#### WinForms Desktop Application

```bash
cd src/TSM31.Dielectric.WinForm
dotnet run
```

## Project Structure

- **TSM31.Dielectric.Core** - Core business logic, services, and abstractions
- **TSM31.Dielectric.Sql** - Database layer with Entity Framework Core (SQLite)
- **TSM31.Dielectric.App** - Shared Blazor component library
- **TSM31.Dielectric.Web** - Blazor Web host application
- **TSM31.Dielectric.WinForm** - WinForms desktop application with embedded Blazor

## Development

### Building the Solution

```bash
dotnet build
```

### Running TailwindCSS in Watch Mode

For development with automatic CSS rebuilding:

```bash
cd src/TSM31.Dielectric.App
npm run tailwind
```

This watches for changes in your CSS files and automatically rebuilds the TailwindCSS output.

### Database

The application uses SQLite for data storage. The database file will be created automatically on first run at:
- Development: `C:\TestStation_Dev\Database\teststation.db`
- Production: `C:\TestStation\Database\teststation.db`

## Function Key Navigation

The template includes example function key configurations for keyboard-driven navigation:

### Global Keys (Available on All Screens)
- **F7** - Open Operator Login/Logout Dialog

### Home Screen
- **F1** - Navigate to Data Entry
- **F2** - Navigate to Data Review
- **F3** - Navigate to Testing

### Data Entry, Data Review, and Testing Screens
- **ESC** - Return to Home Page
- **F1/F2/F3** - Quick switch between pages

These examples are configured in `src/TSM31.Dielectric.App/StubFunctionKeyConfiguration.cs`. You can modify this file to add your own custom function key mappings.

## Technologies Used

- **.NET 9.0**
- **Blazor** - Modern web UI framework
- **FluentUI for Blazor** - UI component library
- **TailwindCSS** - Utility-first CSS framework
- **Entity Framework Core** - ORM with SQLite provider
- **Serilog** - Structured logging
- **RadUtils.SQLServer** - SQL Server utilities

## Configuration

Application settings can be configured in:
- `src/TSM31.Dielectric.Web/appsettings.json`
- `src/TSM31.Dielectric.WinForm/appsettings.json`

## Troubleshooting

### TailwindCSS styles not appearing

Make sure you've run `npm install` in the `src/TSM31.Dielectric.App` directory.

### Build errors related to missing packages

Restore NuGet packages:
```bash
dotnet restore
```

Restore npm packages:
```bash
cd src/TSM31.Dielectric.App
npm install
```
