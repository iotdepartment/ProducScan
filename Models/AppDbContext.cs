using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProducScan.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Defecto> Defectos { get; set; }
    public virtual DbSet<Mesa> Mesas { get; set; }
    public virtual DbSet<Registro> Registros { get; set; }
    public virtual DbSet<RegistrodeDefecto> RegistrodeDefectos { get; set; }
    public virtual DbSet<RegistrodePiezasEscaneada> RegistrodePiezasEscaneadas { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<Mandril> Mandriles { get; set; }





    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=10.195.10.166,1433;Database=ScanSystemDB;User Id=manu; Password=2022.Tgram2;TrustServerCertificate=True;");

    

    //"Server=RMX-D4LZZV2;Database=ScanSystem;Trusted_Connection=True;TrustServerCertificate=True;User ID=eramirez3;Password=2022.Tgram2."

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Defecto>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CodigodeDefecto).HasMaxLength(50);
            entity.Property(e => e.Defecto1)
                .HasMaxLength(100)
                .HasColumnName("Defecto");
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("ID");
        });

        modelBuilder.Entity<Mesa>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("ID");
            entity.Property(e => e.Mesas).HasMaxLength(50);
            entity.Property(e => e.NumerodeMesa).HasMaxLength(50);
        });

        modelBuilder.Entity<RegistrodeDefecto>(entity =>
        {
            modelBuilder.Entity<RegistrodeDefecto>().HasKey(m => m.Id);

            entity.Property(e => e.CodigodeDefecto).HasMaxLength(50);
            entity.Property(e => e.Defecto).HasMaxLength(50);
            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Hora).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Mandrel).HasMaxLength(50);
            entity.Property(e => e.NuMesa).HasMaxLength(50);
            entity.Property(e => e.Tm)
                .HasMaxLength(50)
                .HasColumnName("TM");
            entity.Property(e => e.Turno).HasMaxLength(50);
        });

        modelBuilder.Entity<RegistrodePiezasEscaneada>(entity =>
        {

            modelBuilder.Entity<RegistrodePiezasEscaneada>().HasKey(m => m.Id);

            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Hora).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Mandrel).HasMaxLength(50);
            entity.Property(e => e.Ndpiezas)
                .HasMaxLength(50)
                .HasColumnName("NDPiezas");
            entity.Property(e => e.NuMesa).HasMaxLength(50);
            entity.Property(e => e.Tm)
                .HasMaxLength(50)
                .HasColumnName("TM");
            entity.Property(e => e.Turno).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User"); // 👈 nombre exacto de la tabla
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumerodeEmpleado).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Mandril>(entity =>
        {
            entity.ToTable("Mandriles", "dbo");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MandrilNombre).HasColumnName("Mandril");
            entity.Property(e => e.CentrodeCostos).HasMaxLength(100);
            entity.Property(e => e.CantidaddeEmpaque);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Area).HasMaxLength(100);
            entity.Property(e => e.Kanban).HasMaxLength(100);
        });




        OnModelCreatingPartial(modelBuilder);


    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
