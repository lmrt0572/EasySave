using EasySave.Localization;
using EasySave.Services;
using EasySave.ViewModels;
using EasySave.Views;

var languageManager = new LanguageManager();
var mainViewModel = new MainViewModel(languageManager);
var view = new ConsoleView(mainViewModel);

// CLI mode (e.g. EasySave.exe 1-3) can be added here later
view.Run();
