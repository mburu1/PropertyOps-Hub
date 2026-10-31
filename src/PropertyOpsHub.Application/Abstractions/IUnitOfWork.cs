using System.Threading;
using System.Threading.Tasks;

namespace PropertyOpsHub.Application.Abstractions;

/// <summary>
/// Commits the work of one use case as a single transaction. Implemented by the EF Core
/// context; handlers never see <c>DbContext</c>.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
