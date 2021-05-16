using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Database.EF
{
    public class SignalRadioDbContext : DbContext, ISignalRadioDbContext
    {
        public DbSet<RadioRecorder> RadioRecorders { get; set; }
        public DbSet<RadioSystem> RadioSystems { get; set; }
        public DbSet<RadioGroup> RadioGroups { get; set; }
        public DbSet<TalkGroup> TalkGroups { get; set; }
        public DbSet<Stream> Streams { get; set; }
        public DbSet<TalkGroupStream> TalkGroupStreams { get; set; }
        public DbSet<RadioFrequency> RadioFrequencies { get; set; }
        public DbSet<RadioCall> RadioCalls { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MountPoint> MountPoints { get; set; }
        public DbSet<RadioRecorder> Recorders { get; set; }

        private string _connectionString;

        public SignalRadioDbContext() { }
        public SignalRadioDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SignalRadioDbContext(DbContextOptions<SignalRadioDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RadioRecorder>()
                .ToTable("RadioRecorders", "dbo");
            modelBuilder.Entity<RadioRecorder>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<RadioRecorder>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
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


            // modelBuilder.Entity<TalkGroup>()
            //     .HasOne(e => e.RadioGroup)
            //     .WithMany(e => e.TalkGroups)
            //     .HasForeignKey(e => e.RadioGroupId);
            // modelBuilder.Entity<TalkGroup>()
            //     .HasOne(e => e.RadioSystem)
            //     .WithMany(e => e.TalkGroups)
            //     .HasForeignKey(e => e.RadioSystemId);
            #endregion
            #region TalkGroupStream
            modelBuilder.Entity<TalkGroupStream>()
                .ToTable("TalkGroupStreams", "dbo");
            modelBuilder.Entity<TalkGroupStream>()
                .HasKey(tgs => new { tgs.TalkGroupId, tgs.StreamId });
            modelBuilder.Entity<TalkGroupStream>()
                .HasOne(tgs => tgs.Stream)
                .WithMany(tgs => tgs.StreamTalkGroups)
                .HasForeignKey(tgs => tgs.StreamId);
            modelBuilder.Entity<TalkGroupStream>()
                .HasOne(tgs => tgs.TalkGroup)
                .WithMany(tgs => tgs.TalkGroupStreams)
                .HasForeignKey(tgs => tgs.TalkGroupId);
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
            modelBuilder.Entity<RadioCall>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<RadioCall>()
            .HasOne(e => e.TalkGroup)
            .WithMany(e => e.RadioCalls);
            #endregion
            #region Streams
            modelBuilder.Entity<Stream>()
                .ToTable("Streams", "dbo");
            modelBuilder.Entity<Stream>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Stream>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Stream>()
                .HasOne(s => s.Mount)
                .WithOne(s => s.Stream)
                .HasForeignKey<Stream>(m => m.MountPointId);

            #endregion
            #region MountPoints
            modelBuilder.Entity<MountPoint>()
                .ToTable("MountPoints", "dbo");
            modelBuilder.Entity<MountPoint>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<MountPoint>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<MountPoint>()
                .HasOne(p => p.User)
                .WithMany(p => p.MountPoints)
                .HasForeignKey(p => p.UserId);

            #endregion
            #region Users
            modelBuilder.Entity<User>()
                .ToTable("Users", "dbo");
            modelBuilder.Entity<User>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<User>()
                .Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<User>()
                .HasMany(u => u.MountPoints)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId);
            modelBuilder.Entity<User>()
                .HasMany(u => u.Streams)
                .WithOne(s => s.Owner)
                .HasForeignKey(s => s.OwnerUserId);
            #endregion
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!string.IsNullOrEmpty(_connectionString))
                optionsBuilder.UseSqlite(_connectionString);
        }
    }
    
}