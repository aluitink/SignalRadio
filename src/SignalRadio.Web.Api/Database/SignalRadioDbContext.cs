using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Web.Api.Models;

namespace SignalRadio.Web.Api.Database
{
    public class SignalRadioDbContext: DbContext
    {
        public DbSet<RadioSystem> RadioSystems { get; set; }
        public DbSet<RadioGroup> RadioGroups { get; set; }
        public DbSet<TalkGroup> TalkGroups { get; set; }
        public DbSet<RadioFrequency> RadioFrequencies { get; set; }
        public DbSet<RadioCall> RadioCalls { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=SignalRadio.db", options => {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
            });
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region RadioSystems
            modelBuilder.Entity<RadioSystem>()
                .ToTable("RadioSystems", "dbo");
            modelBuilder.Entity<RadioSystem>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<RadioSystem>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<RadioSystem>()
                .HasMany(e => e.RadioGroups)
                .WithOne(e => e.RadioSystem)
                .HasForeignKey(e => e.RadioSystemId);
            modelBuilder.Entity<RadioSystem>()
                .HasMany(e => e.ControlFrequencies)
                .WithOne(e => e.RadioSystem)
                .HasForeignKey(e => e.RadioSystemId);
            modelBuilder.Entity<RadioSystem>()
                .HasMany(e => e.TalkGroups)
                .WithOne(e => e.RadioSystem)
                .HasForeignKey(e => e.RadioSystemId);
            #endregion
            #region RadioGroups
            modelBuilder.Entity<RadioGroup>()
                .ToTable("RadioGroups", "dbo");
            modelBuilder.Entity<RadioGroup>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<RadioGroup>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<RadioGroup>()
                .HasMany(e => e.TalkGroups)
                .WithOne(e => e.RadioGroup)
                .HasForeignKey(e => e.RadioGroupId);
            modelBuilder.Entity<RadioGroup>()
                .HasOne(e => e.RadioSystem)
                .WithMany(e => e.RadioGroups)
                .HasForeignKey(e => e.RadioSystemId);
            #endregion
            #region TalkGroups
            modelBuilder.Entity<TalkGroup>()
                .ToTable("TalkGroups", "dbo");
            modelBuilder.Entity<TalkGroup>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<TalkGroup>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<TalkGroup>()
                .HasOne(e => e.RadioGroup)
                .WithMany(e => e.TalkGroups)
                .HasForeignKey(e => e.RadioGroupId);
            modelBuilder.Entity<TalkGroup>()
                .HasOne(e => e.RadioSystem)
                .WithMany(e => e.TalkGroups)
                .HasForeignKey(e => e.RadioSystemId);
            #endregion
            #region RadioFrequencies
            modelBuilder.Entity<RadioFrequency>()
                .ToTable("RadioFrequencies", "dbo");
            modelBuilder.Entity<RadioFrequency>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<RadioFrequency>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<RadioFrequency>()
                .HasOne(e => e.RadioSystem)
                .WithMany(e => e.ControlFrequencies)
                .HasForeignKey(e => e.RadioSystemId);
            #endregion
            #region RadioCalls
            modelBuilder.Entity<RadioCall>()
                .ToTable("RadioCalls", "dbo");
            modelBuilder.Entity<RadioCall>()
                .HasKey(e => e.Id);
            #endregion
        }
    }
}