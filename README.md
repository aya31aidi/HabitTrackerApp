# HabitTracker

A habit tracking web application built with ASP.NET Core MVC and SQL Server. Habits are checked off in one click, streaks and completion rates are calculated automatically, and reminders pop up while the app is open. A small WinForms desktop app is included for managing users.

## Features

**Tracking**
- One-click daily check-in, saved instantly without reloading the page
- 7-day tracker on every habit card (past days can be ticked too)
- Activity calendar (heatmap) covering the last 26 weeks, clickable to toggle any day
- History of every record, with filters by habit and status
- Confetti when every habit of the day is done 🎉

**Statistics** (computed from the records, nothing typed by hand)
- Current streak, best streak, total check-ins and completion rate per habit
- 30-day trend, check-ins per category and per weekday (Chart.js)
- Leaderboard of the most consistent habits

**Organization**
- Categories with automatic icons and colors (Health, Sport, Reading, Work...)
- Reminders with quick time presets, in-app toasts and browser notifications
- Search on every list (press `/` to jump to it)

**Interface**
- Dashboard with today's progress ring, stat tiles and upcoming reminders
- Light and dark mode (follows the system setting, remembered per browser)
- Responsive layout with a slide-in menu on phones

**Accounts and security**
- Sign up / sign in with cookie authentication and "Remember me"
- Two roles: **Admin** (manages users and categories, sees every habit) and **User** (only sees their own data)
- Passwords hashed with ASP.NET Core Identity's `PasswordHasher`
- Ownership checks on every habit, record and reminder, anti-forgery tokens on every form

## Tech stack

| Layer | Technology |
|---|---|
| Web | ASP.NET Core MVC (.NET 8), Razor views |
| Data | Entity Framework Core 8, SQL Server |
| Front end | Bootstrap 5.3, Bootstrap Icons, Chart.js 4, plain JavaScript |
| Desktop | Windows Forms (.NET 8) |

## Project structure

```
HabitTrackerApp.sln
├── Domain/        Entities (Habit, HabitRecord, Category, Reminder, User...) and auth view models
├── Web/           The ASP.NET Core MVC application
│   ├── Controllers/   One controller per section + AppController base class
│   ├── Data/          WebContext (EF Core DbContext)
│   ├── Migrations/    EF Core migrations
│   ├── Models/        View models (dashboard, statistics, habit summaries...)
│   ├── Services/      Statistics, password hashing, category styling, claim helpers
│   ├── Views/         Razor views and shared partials
│   └── wwwroot/       site.css (design system) and site.js (dynamic behaviour)
├── desktop/       WinForms app to manage users
└── Data/          Legacy Entity Framework 6 context, no longer used by the web app
```

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer
- SQL Server 2014 or newer (Express and LocalDB work too)
- Visual Studio 2022 (optional)
- An internet connection: Bootstrap, the icons, the font and Chart.js load from CDNs

### 1. Configure the database connection

Open `Web/appsettings.json` and set `ConnectionStrings:HabitTrackerContext` to the SQL Server instance:

```json
"ConnectionStrings": {
    "HabitTrackerContext": "Server=localhost;Database=HabitTrackerDatabase;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

The database doesn't need to exist: it is created, and kept up to date, from the migrations when the app starts.

### 2. Run the app

From a terminal:

```powershell
cd Web
dotnet run
```

Then open **http://localhost:5245** (or `dotnet run --launch-profile https` for **https://localhost:7169**).

From Visual Studio: open `HabitTrackerApp.sln`, right-click the **Web** project → **Set as Startup Project**, then press **F5**.

### 3. Sign in

| Role | Email | Password |
|---|---|---|
| Admin | `admin@habittracker.com` | `Admin123` |
| User | Created from the **Create a free account** page | |

The admin account is defined in the `Admin` section of `Web/appsettings.json`. Changing that password before deploying anywhere is strongly recommended.

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:HabitTrackerContext` | SQL Server connection used by the app |
| `Admin:Name`, `Admin:Email`, `Admin:Password` | Built-in administrator account (not stored in the database) |

Any setting can also be overridden with an environment variable, for example:

```powershell
$env:ConnectionStrings__HabitTrackerContext = "Server=...;Database=...;"
```

## How the statistics are calculated

- **Total check-ins**: number of days marked as done.
- **Current streak**: consecutive done days ending today. A streak stays alive until the end of the day, so it can also end yesterday.
- **Best streak**: longest run of consecutive done days ever.
- **Completion rate**: done days ÷ days since the first record of the habit × 100.

The `HabitStatistics` table is refreshed after every change so the desktop app can read up-to-date numbers. The **Recalculate** button on the Statistics page rebuilds it for every habit.

## Database migrations

Migrations live in `Web/Migrations` and are applied automatically at startup. To add a new one after changing the entities:

```powershell
cd Web
dotnet ef migrations add NameOfTheChange
```

The `dotnet ef` tool can be installed with `dotnet tool install --global dotnet-ef`.

EF Core is configured with compatibility level 120 in `Program.cs`, so the generated SQL also runs on SQL Server 2014.

## Desktop app

The `desktop` project is a Windows Forms app to list, add, edit and delete users directly in the database. Its connection string is set in `desktop/Form1.cs`.

```powershell
dotnet run --project desktop
```

## Troubleshooting

| Problem | Fix |
|---|---|
| `Failed to bind to address ... address already in use` | Another instance is running: `Get-Process Web \| Stop-Process`, or pick another port with `dotnet run --urls http://localhost:5300` |
| `A network-related or instance-specific error occurred` | SQL Server isn't running or the server name in the connection string is wrong |
| Pages look unstyled | The CDN files couldn't load, check the internet connection |
| An existing user can't sign in | Accounts created before password hashing still work, they are upgraded to a hash on their first login. Otherwise an admin can set a new password from **Users → Edit** |
