using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AtelierManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.Data
{
    public class AtelierContext : DbContext
    {
        private readonly string _connectionString;

        public AtelierContext()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "atelier.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath};Foreign Keys=True;";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlite(_connectionString)
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine);
        }


        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<MeasurementSet> MeasurementSets => Set<MeasurementSet>();
        public DbSet<Status> Statuses => Set<Status>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ExtraElement> ExtraElements => Set<ExtraElement>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderService> OrderServices => Set<OrderService>();
        public DbSet<OrderServiceExtra> OrderServiceExtras => Set<OrderServiceExtra>();
        public DbSet<OrderMaterial> OrderMaterials => Set<OrderMaterial>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Employee> Employees => Set<Employee>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Person>().ToTable("Persons");
            modelBuilder.Entity<MeasurementSet>().ToTable("MeasurementSets");
            modelBuilder.Entity<Status>().ToTable("Statuses");
            modelBuilder.Entity<Service>().ToTable("Services");
            modelBuilder.Entity<ExtraElement>().ToTable("ExtraElements");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderService>().ToTable("OrderServices");
            modelBuilder.Entity<OrderServiceExtra>().ToTable("OrderServiceExtra");
            modelBuilder.Entity<OrderMaterial>().ToTable("OrderMaterials");
            modelBuilder.Entity<Material>().ToTable("Materials");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<Employee>().ToTable("Employees");


            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Login)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Employee)
                .WithMany(e => e.Orders)
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Persons)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MeasurementSet>()
                .HasOne(ms => ms.Person)
                .WithMany(p => p.MeasurementSets)
                .HasForeignKey(ms => ms.PersonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Status)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(os => os.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Service)
                .WithMany()
                .HasForeignKey(os => os.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.Person)
                .WithMany()
                .HasForeignKey(os => os.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.MeasurementSet)
                .WithMany()
                .HasForeignKey(os => os.MeasurementSetId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderServiceExtra>()
                .HasOne(e => e.OrderService)
                .WithMany(os => os.Extras)
                .HasForeignKey(e => e.OrderServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderServiceExtra>()
                .HasOne(e => e.ExtraElement)
                .WithMany()
                .HasForeignKey(e => e.ExtraElementId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExtraElement>()
                .HasOne(x => x.Service)
                .WithMany(s => s.ExtraElements)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderMaterial>()
                .HasOne(om => om.Order)
                .WithMany(o => o.OrderMaterials)
                .HasForeignKey(om => om.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderMaterial>()
                .HasOne(om => om.Material)
                .WithMany()
                .HasForeignKey(om => om.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
