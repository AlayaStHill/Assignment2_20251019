using Domain.Results;

namespace Domain.Interfaces;
// Istället för att duplicera ReadAsync, WriteAsync i tre olika repositories (DRY).
public interface IRepository<T>
{
    Task<RepositoryResult<IEnumerable<T>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<T> entities, CancellationToken cancellationToken);
}

