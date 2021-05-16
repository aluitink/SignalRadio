using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SignalRadio.Public.Lib.Models;

namespace SignalRadio.Database.EF
{
    public interface ISignalRadioDbContext
    {
        DbSet<RadioRecorder> Recorders { get; set; }
        DbSet<RadioSystem> RadioSystems { get; set; }
        DbSet<RadioGroup> RadioGroups { get; set; }
        DbSet<TalkGroup> TalkGroups { get; set; }
        DbSet<Stream> Streams { get; set; }
        DbSet<TalkGroupStream> TalkGroupStreams { get; set; }
        DbSet<RadioFrequency> RadioFrequencies { get; set; }
        DbSet<RadioCall> RadioCalls { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<MountPoint> MountPoints { get; set; }

        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        DatabaseFacade Database { get; }
    }
}