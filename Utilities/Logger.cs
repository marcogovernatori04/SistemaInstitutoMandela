using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SistemaInstituto.Utilities
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "errores.log");

        public static void Log(string message, Exception ex = null)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine("---------------------------------");
                    writer.WriteLine($"Fecha: {DateTime.Now}");
                    writer.WriteLine($"Mensaje: {message}");
                    if (ex != null)
                    { 
                        writer.WriteLine($"Excepción: {ex.Message}");
                        writer.WriteLine($"StackTrace: {ex.StackTrace}");
                    }
                    writer.WriteLine("---------------------------------");
                    writer.WriteLine();
                }
            }
            catch
            {
                // Si ocurre un error al escribir en el archivo de log, no hacemos nada
            }
        }
    }
}
