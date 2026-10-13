using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Utilities;
using SistemaInstituto.Views;
using System.Globalization;
using System.IO;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace SistemaInstituto;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        File.AppendAllText(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SistemaInstituto", "debug.log"),
            $"{DateTime.Now} - OnStartup iniciado\n"
        );


        using (var context = new SistemaInstituto.Data.InstitutoDbContext())
        {
            context.Database.EnsureCreated();
            context.Database.Migrate();
            DataTextNormalizer.NormalizeExistingData(context);
        }

        Task.Run(async () =>
        {
            try
            {
                var source = new Velopack.Sources.GithubSource(
                    "https://github.com/marcogovernatori04/SistemaInstitutoMandela",
                    null,
                    false
                );
                var mgr = new UpdateManager(source);
                var newVersion = await mgr.CheckForUpdatesAsync();
                if (newVersion != null)
                {
                    await mgr.DownloadUpdatesAsync(newVersion);
                    mgr.ApplyUpdatesAndRestart(newVersion);
                }
                else
                {
                    Logger.Log("No hay actualizaciones disponibles.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error al verificar actualizaciones.", ex);
            }
        });

        var cultura = new CultureInfo("es-AR");
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

        new MainWindow().Show(); // 👈
    }

    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var inner = e.Exception.InnerException?.Message ?? "sin inner exception";
        Logger.Log($"Excepción no controlada. Inner: {inner}", e.Exception);
        MessageBox.Show("Ocurrió un error inesperado. Por favor, contacte al soporte.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Log("Excepción no controlada en el dominio de la aplicación.", ex);
        }
    }
}
