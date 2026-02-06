# EasySave

EasySave is a backup software developed as part of the ProSoft software suite.  
It allows users to create and execute backup jobs easily, either individually or sequentially, through a console-based application.


## Description

EasySave is a .NET console application designed to manage file and directory backups.  
It supports full and differential backup strategies and provides real-time logging and state tracking.

The project follows professional software development practices with a focus on maintainability, extensibility, and version control.


## Features

- Create up to 5 backup jobs
- Full and differential backup strategies
- Execute a single backup job or multiple jobs sequentially
- Real-time backup state tracking
- Daily backup activity logging (JSON format)
- Multilingual support (English / French)
- Command-line execution support


## Technologies

- Language: **C#**
- Framework: **.NET 10.0**
- IDE: **Visual Studio 2022 or latest versions**


## Getting Started

### Prerequisites

- Windows operating system
- .NET 8.0 or higher installed

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/lmrt0572/EasySave.git
   ```
3. Open the solution in Visual Studio 2022
4. Restore NuGet packages if needed
5. Build the solution
6. Run the application


## Usage

The application can be executed:
- Interactively via the console interface
- Via command line arguments

Examples:

```bash
EasySave.exe 1-3
EasySave.exe 1;3
```


## Documentation

The project includes the following documentation:
- User manual (1 page)
- Technical support documentation
- UML diagrams
- Release notes

All documentation is written in English to ensure international readability.


## Versioning

This project follows semantic versioning:
- Major versions: functional evolutions
- Minor versions: improvements and optimizations
- Patch versions: bug fixes


## Authors

Developed by the EasySave project team at ProSoft.


## License

This project is developed for educational and professional purposes.
