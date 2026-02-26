# EasySave

EasySave is a backup software developed as part of the ProSoft software suite. It allows users to create and execute backup jobs easily — individually, sequentially, or in parallel — through a modern graphical interface (WPF) as well as a console-based application.

## Description

EasySave is a .NET application designed to manage file and directory backups. It supports full and differential backup strategies and provides real-time logging, state tracking, file encryption, and centralized log management.

Version 3 marks a major evolution of the product: the 5-job limit has been removed, parallel execution has been introduced, a WPF graphical interface has been added, and backups can now be monitored and controlled remotely via a centralized log server.

The project follows professional software development practices with a focus on maintainability, extensibility, and version control.

---

## Features

- **Unlimited backup jobs** (no job count restriction in V3)
- Full and differential backup strategies
- **Parallel execution** of multiple backup jobs simultaneously
- **Pause / Resume / Stop** controls per job during execution
- **Automatic pause** when a business-critical software is detected (configurable)
- **Large file transfer coordination** — prevents simultaneous transfer of large files across parallel jobs
- File **encryption** via the integrated CryptoSoft module (XOR-based, configurable file extensions)
- Real-time backup state tracking
- Daily backup activity logging in **JSON or XML** format (switchable)
- **Centralized log server** — log entries are forwarded to a remote HTTP endpoint (Docker-hosted)
- **WPF graphical interface** with multiple color themes (Caramel Profond, Mode Nuit)
- Console interface for command-line usage
- Multilingual support (**English / French**)
- Command-line execution support

---

## Architecture

The solution is organized into several projects:

| Project | Role |
|---|---|
| `EasySave.Core` | Core business logic, models, services, strategies |
| `EasySave.WPF` | WPF graphical user interface |
| `EasySave.Console` | Console-based interface |
| `EasyLog` | Logging library (JSON / XML strategies) |
| `CryptoSoft` | File encryption module |
| `EasySave.LogServer` | Centralized ASP.NET Core log server (Docker) |
| `EasySave.Tests` | Unit test suite (xUnit) |

---

## Technologies

- **Language:** C#
- **Framework:** .NET 10.0
- **UI:** WPF (Windows Presentation Foundation)
- **Log Server:** ASP.NET Core Web API
- **Containerization:** Docker / Docker Compose
- **Testing:** xUnit
- **IDE:** Visual Studio 2022 or later

---

## Getting Started

### Prerequisites

- Windows operating system
- .NET 10.0 SDK or higher installed
- Docker Desktop (optional, for the centralized log server)

### Installation

1. Clone the repository:

```bash
git clone https://github.com/lmrt0572/EasySave.git
```

2. Open the solution (`EasySave.slnx`) in Visual Studio 2022
3. Restore NuGet packages if needed
4. Build the solution
5. Run the desired project (`EasySave.WPF` for the graphical interface, or `EasySave.Console` for the CLI)

### Running the Log Server (optional)

A Docker Compose file is provided to spin up the centralized log server:

```bash
docker-compose up
```

The server will be available at `http://localhost:8080/api/logs` by default. This URL can be configured from the application settings.

---

## Usage

### Graphical Interface (WPF)

Launch `EasySave.WPF` and use the interface to:

- Create, edit, and delete backup jobs
- Start, pause, resume, or stop individual jobs
- Monitor real-time progress per job
- Configure log format, language, encryption extensions, and color theme

### Console Interface

The application can also be executed via command-line arguments:

```bash
EasySave.exe 1-3       # Run jobs 1 to 3
EasySave.exe 1 ;3      # Run jobs 1 and 3
EasySave.exe 1;3;5     # Run jobs 1, 3 and 5
EasySave.exe 2         # Run job 2
```

> In V3, job indices are no longer restricted to a maximum of 5. Any valid positive index is accepted.

---

## Configuration

The application settings include:

- **Log format:** JSON or XML
- **Language:** English or French
- **Encryption extensions:** list of file extensions to encrypt during backup (e.g. `.txt`, `.docx`)
- **Business software monitor:** process name to watch — backups are automatically paused when this process is running
- **Large file threshold:** size (in KB) above which only one large file transfer is allowed at a time across parallel jobs
- **Centralized log URL:** remote endpoint for Docker-hosted log aggregation

---

## Documentation

The project includes the following documentation:

- User manual
- Technical support documentation
- UML diagrams
- Release notes

All documentation is written in English to ensure international readability.

---

## Versioning

This project follows semantic versioning:

- **Major versions:** functional evolutions
- **Minor versions:** improvements and optimizations
- **Patch versions:** bug fixes

| Version | Highlights |
|---|---|
| V1 | Console app, up to 5 jobs, JSON logging, EN/FR |
| V2 | XML logging option, differential strategy improvements |
| V3 | WPF GUI, unlimited jobs, parallel execution, encryption, business software monitor, centralized log server |

---

## Authors

Developed by the EasySave project team at ProSoft.

---

## License

This project is developed for educational and professional purposes.
