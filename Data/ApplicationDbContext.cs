using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ProducScan.Modelos;

namespace ProducScan.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Costo> Costos { get; set; }

    public virtual DbSet<Defecto> Defectos { get; set; }

    public virtual DbSet<DownTime> DownTimes { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Mandrel> Mandrels { get; set; }

    public virtual DbSet<MandrelsExt> MandrelsExts { get; set; }

    public virtual DbSet<Mandrile> Mandriles { get; set; }

    public virtual DbSet<Mesa> Mesas { get; set; }

    public virtual DbSet<RegistrodeDefecto> RegistrodeDefectos { get; set; }

    public virtual DbSet<RegistrodePiezasEscaneada> RegistrodePiezasEscaneadas { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=10.195.10.166,1433;Database=ScanSystemBD;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Costo>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Costo1).HasColumnName("Costo");
            entity.Property(e => e.Mandrel).HasMaxLength(50);
        });

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

        modelBuilder.Entity<DownTime>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DownTime__3214EC27C14751FC");

            entity.ToTable("DownTime");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Dt)
                .HasMaxLength(50)
                .HasColumnName("DT");
            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Hora).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Mesa).HasMaxLength(50);
            entity.Property(e => e.Tm)
                .HasMaxLength(50)
                .HasColumnName("TM");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Logs__3214EC078816C556");
        });

        modelBuilder.Entity<Mandrel>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.BarCode).HasMaxLength(50);
            entity.Property(e => e.Cantidadempaqueinspeccion)
                .HasMaxLength(50)
                .HasColumnName("CANTIDADEMPAQUEINSPECCION");
            entity.Property(e => e.Cantidadkanbanfinalcaja)
                .HasMaxLength(50)
                .HasColumnName("CANTIDADKANBANFINALCAJA");
            entity.Property(e => e.Diametroexterior)
                .HasMaxLength(50)
                .HasColumnName("DIAMETROEXTERIOR");
            entity.Property(e => e.Diametrointerior)
                .HasMaxLength(50)
                .HasColumnName("DIAMETROINTERIOR");
            entity.Property(e => e.Espesordepared)
                .HasMaxLength(50)
                .HasColumnName("ESPESORDEPARED");
            entity.Property(e => e.Kanbanfinal)
                .HasMaxLength(50)
                .HasColumnName("KANBANFINAL");
            entity.Property(e => e.Kanbanoven)
                .HasMaxLength(50)
                .HasColumnName("KANBANOVEN");
            entity.Property(e => e.Mandril)
                .HasMaxLength(50)
                .HasColumnName("MANDRIL");
            entity.Property(e => e.Num)
                .HasMaxLength(50)
                .HasColumnName("NUM");
            entity.Property(e => e.Nupartedemanguera)
                .HasMaxLength(50)
                .HasColumnName("NUPARTEDEMANGUERA");
            entity.Property(e => e.Nupartefinal)
                .HasMaxLength(50)
                .HasColumnName("NUPARTEFINAL");
            entity.Property(e => e.Nurackinspeccion)
                .HasMaxLength(50)
                .HasColumnName("NURACKINSPECCION");
            entity.Property(e => e.Nurackoven)
                .HasMaxLength(50)
                .HasColumnName("NURACKOVEN");
        });

        modelBuilder.Entity<MandrelsExt>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("MandrelsExt");

            entity.Property(e => e.BarCode).HasMaxLength(50);
            entity.Property(e => e.Cantidadempaqueinspeccion)
                .HasMaxLength(50)
                .HasColumnName("CANTIDADEMPAQUEINSPECCION");
            entity.Property(e => e.Cantidadkanbanfinalcaja)
                .HasMaxLength(50)
                .HasColumnName("CANTIDADKANBANFINALCAJA");
            entity.Property(e => e.Diametroexterior)
                .HasMaxLength(50)
                .HasColumnName("DIAMETROEXTERIOR");
            entity.Property(e => e.Diametrointerior)
                .HasMaxLength(50)
                .HasColumnName("DIAMETROINTERIOR");
            entity.Property(e => e.Espesordepared)
                .HasMaxLength(50)
                .HasColumnName("ESPESORDEPARED");
            entity.Property(e => e.Kanbanfinal)
                .HasMaxLength(50)
                .HasColumnName("KANBANFINAL");
            entity.Property(e => e.Kanbanoven)
                .HasMaxLength(50)
                .HasColumnName("KANBANOVEN");
            entity.Property(e => e.Mandril)
                .HasMaxLength(50)
                .HasColumnName("MANDRIL");
            entity.Property(e => e.Num)
                .HasMaxLength(50)
                .HasColumnName("NUM");
            entity.Property(e => e.Nupartedemanguera)
                .HasMaxLength(50)
                .HasColumnName("NUPARTEDEMANGUERA");
            entity.Property(e => e.Nupartefinal)
                .HasMaxLength(50)
                .HasColumnName("NUPARTEFINAL");
            entity.Property(e => e.Nurackinspeccion)
                .HasMaxLength(50)
                .HasColumnName("NURACKINSPECCION");
            entity.Property(e => e.Nurackoven)
                .HasMaxLength(50)
                .HasColumnName("NURACKOVEN");
        });

        modelBuilder.Entity<Mandrile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Mandrile__3214EC276ADA0B25");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Area).HasMaxLength(50);
            entity.Property(e => e.Barcode).HasMaxLength(50);
            entity.Property(e => e.CantidaddeEmpaque).HasMaxLength(50);
            entity.Property(e => e.CentrodeCostos).HasMaxLength(50);
            entity.Property(e => e.Kanban).HasMaxLength(50);
            entity.Property(e => e.Mandril).HasMaxLength(50);
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
                entity.HasKey(e => e.Id).HasName("PK__Registro__3214EC074C42116D");

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
            entity.HasKey(e => e.Id).HasName("PK__Registro__3214EC076EA88E68");

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
            entity
                .HasNoKey()
                .ToTable("User");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("ID");
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.NumerodeEmpleado).HasMaxLength(50);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usuarios__3214EC07FBCF523C");

            entity.Property(e => e.SecurityStamp).HasDefaultValue("");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
