using System.Windows;
using Drilling.Common.Log;

namespace Drilling.UI;

public partial class CApp : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterExceptionHandlers();

        try
        {
            base.OnStartup(e);

            CProgramOpenLog.Write("PROGRAM_OPEN", "Application startup started.");

            var window = (Window)LoadComponent(new Uri("CRootView.xaml", UriKind.Relative));
            window.DataContext = CAppStartup.CreateMainViewModel();
            MainWindow = window;
            window.Show();

            CProgramOpenLog.Write("PROGRAM_OPEN", "Main window opened.");
        }
        catch (Exception exception)
        {
            CProgramOpenLog.Write("PROGRAM_OPEN_FAILED", exception);
            MessageBox.Show(
                $"Program startup failed.{Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}Log: {CProgramOpenLog.LogPath}",
                "Laser Drilling",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            CProgramOpenLog.Write("DISPATCHER_UNHANDLED", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            CProgramOpenLog.Write(
                "APPDOMAIN_UNHANDLED",
                exception?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            CProgramOpenLog.Write("TASK_UNOBSERVED", args.Exception);
        };
    }
}


