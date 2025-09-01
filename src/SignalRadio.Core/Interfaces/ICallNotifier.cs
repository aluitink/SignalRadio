using System.Threading;
using System.Threading.Tasks;

namespace SignalRadio.Core.Services;

/// <summary>
/// Abstraction used by lower layers to notify interested parties when a Call has been created/updated.
/// Implementations are expected to broadcast via SignalR or other mechanisms.
/// </summary>
public interface ICallNotifier
{
    /// <summary>
    /// Notify that a call has been created or updated. Implementations should be best-effort and not throw.
    /// </summary>
    Task NotifyCallUpdatedAsync(int callId, CancellationToken cancellationToken = default);
}
