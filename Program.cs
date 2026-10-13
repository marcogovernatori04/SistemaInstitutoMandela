using System.IO;
using Velopack;

namespace SistemaInstituto
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaInstituto"
            );
            Directory.CreateDirectory(carpeta);
            File.AppendAllText(Path.Combine(carpeta, "debug.log"), $"{DateTime.Now} - Main iniciado\n");

            VelopackApp.Build().Run();

            File.AppendAllText(Path.Combine(carpeta, "debug.log"), $"{DateTime.Now} - Velopack ok\n");

            var app = new App();
            app.InitializeComponent();

            File.AppendAllText(Path.Combine(carpeta, "debug.log"), $"{DateTime.Now} - InitializeComponent ok\n");

            app.Run();
        }
    }
}