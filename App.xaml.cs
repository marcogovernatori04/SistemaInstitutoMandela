using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace SistemaInstituto;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var cultura = new CultureInfo("es-AR");
        Thread.CurrentThread.CurrentCulture = cultura;  
        Thread.CurrentThread.CurrentUICulture = cultura;
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));
    }
}




