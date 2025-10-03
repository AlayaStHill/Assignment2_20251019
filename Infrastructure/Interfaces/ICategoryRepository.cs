using Infrastructure.Models;

namespace Infrastructure.Interfaces;
//REGISTRERA I DI
public interface ICategoryRepository
{
    Task<RepositoryResult<IEnumerable<Category>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<Category> categories, CancellationToken cancellationToken); 
}
