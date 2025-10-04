using Domain.Results;

namespace Domain.Interfaces;
// Istället för att duplicera ReadAsync, WriteAsync i tre olika interfaces (DRY).
public interface IRepository<T>
{
    Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
    Task<RepositoryResult<bool>> ExistsAsync(Func<T, bool> isMatch, CancellationToken cancellationToken);
    Task<RepositoryResult<T>> GetEntityAsync(Func<T, bool> predicate,CancellationToken cancellationToken);

}

