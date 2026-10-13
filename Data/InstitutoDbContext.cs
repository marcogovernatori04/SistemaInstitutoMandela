using Microsoft.EntityFrameworkCore;
using SistemaInstituto.Models;
using System.IO;
using System.Windows;

namespace SistemaInstituto.Data;

public partial class InstitutoDbContext : DbContext
{
    public InstitutoDbContext()
    {
    }

    public InstitutoDbContext(DbContextOptions<InstitutoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alumno> Alumnos { get; set; }
    public virtual DbSet<AlumnosBeneficio> AlumnosBeneficios { get; set; }
    public virtual DbSet<Arancel> Aranceles { get; set; }
    public virtual DbSet<Beneficio> Beneficios { get; set; }
    public virtual DbSet<Curso> Cursos { get; set; }
    public virtual DbSet<Estado> Estados { get; set; }
    public virtual DbSet<MetodoPago> MetodosPagos { get; set; }
    public virtual DbSet<Movimiento> Movimientos { get; set; }
    public virtual DbSet<Pago> Pagos { get; set; }
    public virtual DbSet<Profesor> Profesores { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var rutaDb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SistemaInstituto", "Instituto.sqlite");
        var carpeta = Path.GetDirectoryName(rutaDb);
        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        File.AppendAllText(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SistemaInstituto", "debug.log"),
            $"{DateTime.Now} - Ruta db: {rutaDb} - Existe: {File.Exists(rutaDb)}\n"
        );

        optionsBuilder.UseSqlite($"Data Source={rutaDb}");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Alumno
        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(64);
            entity.Property(e => e.Telefono).HasMaxLength(32);
            entity.Property(e => e.Notas).HasMaxLength(128);

            entity.HasOne(d => d.IdCursoNavigation).WithMany(p => p.Alumnos)
                .HasForeignKey(d => d.IdCurso)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Alumnos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // AlumnosBeneficio (1:1)
        modelBuilder.Entity<AlumnosBeneficio>(entity =>
        {
            entity.HasKey(e => e.IdAlumno);

            entity.HasOne(d => d.IdAlumnoNavigation).WithOne(a => a.AlumnosBeneficio)
                .HasForeignKey<AlumnosBeneficio>(d => d.IdAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdBeneficioNavigation).WithMany(b => b.AlumnosBeneficios)
                .HasForeignKey(d => d.IdBeneficio)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Arancel
        modelBuilder.Entity<Arancel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Concepto).HasMaxLength(64);
            entity.Property(e => e.Activo).HasDefaultValue(true);

            entity.Property(e => e.Monto).HasConversion<double>();

            entity.HasOne(d => d.IdCursoNavigation).WithMany(p => p.Aranceles)
                .HasForeignKey(d => d.IdCurso)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // Beneficio
        modelBuilder.Entity<Beneficio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(64);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // Curso
        modelBuilder.Entity<Curso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(32);
        });

        // Estado
        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(32);
        });

        // MetodoPago
        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(32);
        });

        // Movimiento
        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Concepto).HasMaxLength(255);

            entity.Property(e => e.Importe).HasConversion<double>();

            entity.HasOne(d => d.IdAlumnoNavigation).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.IdAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdPagoNavigation).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.IdPago);
        });

        // Pago
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Concepto).HasMaxLength(32);
            entity.Property(e => e.Notas).HasMaxLength(32);

            entity.Property(e => e.MontoPagado).HasConversion<double>();

            entity.HasOne(d => d.IdAlumnoNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdAlumno)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdArancelNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdArancel)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IdMetodoPagoNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdMetodoPago)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        // Profesor
        modelBuilder.Entity<Profesor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(32);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
