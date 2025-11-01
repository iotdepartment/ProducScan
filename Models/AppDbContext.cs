﻿using System;
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
    public virtual DbSet<Mandrel> Mandrels { get; set; }
    public virtual DbSet<Mesa> Mesas { get; set; }
    public virtual DbSet<Registro> Registros { get; set; }
    public virtual DbSet<RegistrodeDefecto> RegistrodeDefectos { get; set; }
    public virtual DbSet<RegistrodePiezasEscaneada> RegistrodePiezasEscaneadas { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Log> Logs { get; set; }




    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=10.195.10.166,1433;Database=ScanSystemDb_Copy;Trusted_Connection=True;TrustServerCertificate=True;User ID=Produccion;Password=produ2025!tg.");

    //"Server=RMX-D4LZZV2;Database=ScanSystemDb;Trusted_Connection=True;TrustServerCertificate=True;User ID=eramirez3;Password=2022.Tgram2."

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





        OnModelCreatingPartial(modelBuilder);


    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
