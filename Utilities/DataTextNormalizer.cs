using SistemaInstituto.Data;

namespace SistemaInstituto.Utilities
{
    public static class DataTextNormalizer
    {
        public static void NormalizeExistingData(InstitutoDbContext context)
        {
            var changed = false;

            foreach (var alumno in context.Alumnos)
            {
                changed |= SetIfChanged(alumno.Nombre, v => alumno.Nombre = v!);
                changed |= SetIfChanged(alumno.Notas, v => alumno.Notas = v, allowNull: true);
            }

            foreach (var pago in context.Pagos)
            {
                changed |= SetIfChanged(pago.Concepto, v => pago.Concepto = v!);
                changed |= SetIfChanged(pago.Notas, v => pago.Notas = v, allowNull: true);
            }

            foreach (var movimiento in context.Movimientos)
            {
                changed |= SetIfChanged(
                    movimiento.Concepto,
                    v => movimiento.Concepto = v!,
                    normalizer: TextNormalizer.ToSentenceCase);
            }

            foreach (var beneficio in context.Beneficios)
            {
                changed |= SetIfChanged(beneficio.Nombre, v => beneficio.Nombre = v!);
            }

            foreach (var curso in context.Cursos)
            {
                changed |= SetIfChanged(curso.Nombre, v => curso.Nombre = v!);
            }

            if (changed)
                context.SaveChanges();
        }

        private static bool SetIfChanged(
            string? current,
            Action<string?> setValue,
            bool allowNull = false,
            Func<string, string>? normalizer = null)
        {
            normalizer ??= TextNormalizer.ToTitleCase;

            var normalized = allowNull
                ? TextNormalizer.ToTitleCaseOrNull(current)
                : normalizer(current ?? string.Empty);

            if (current == normalized)
                return false;

            setValue(normalized);
            return true;
        }
    }
}
