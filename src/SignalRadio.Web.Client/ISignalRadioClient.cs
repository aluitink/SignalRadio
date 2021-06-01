using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using SignalRadio.Public.Lib.Models;
using Stream = SignalRadio.Public.Lib.Models.Stream;

namespace SignalRadio.Web.Client
{
    public interface ISignalRadioClient
    {
        Task<Collection<Stream>> GetStreamsByTalkGroupIdAsync(uint id, CancellationToken cancellationToken = default);
        Task<TalkGroup> GetTalkGroupByIdentifierAsync(ushort identifier, CancellationToken cancellationToken = default);
        Task<TalkGroupImportResults> ImportTalkgroupCsvAsync(string talkGroupCsvPath);
        Task<RadioCall> PostCallAsync(RadioCall radioCall, CancellationToken cancellationToken = default);
    }
}