using System.Windows;
using MistXI.Launcher.Services;

namespace MistXI.Launcher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Global exception handler
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var logger = new Logger("MistXI");
            logger.Write("UNHANDLED EXCEPTION", ex);
            
            MessageBox.Show(
                $"An unhandled error occurred:\n\n{ex?.Message}\n\n" +
                $"Check log: {logger.LogPath}",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        };
        
        DispatcherUnhandledException += (s, args) =>
        {
            var logger = new Logger("MistXI");
            logger.Write("UNHANDLED UI EXCEPTION", args.Exception);
            
            MessageBox.Show(
                $"An unhandled UI error occurred:\n\n{args.Exception.Message}\n\n" +
                $"Stack trace:\n{args.Exception.StackTrace}\n\n" +
                $"Check log: {logger.LogPath}",
                "UI Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            
            args.Handled = true;
        };
    }
}
