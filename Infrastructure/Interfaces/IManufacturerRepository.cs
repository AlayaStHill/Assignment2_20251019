using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IManufacturerRepository
{
    Task<RepositoryResult<IEnumerable<Manufacturer>>> ReadAsync(CancellationToken cancellationToken);
    Task<RepositoryResult> WriteAsync(IEnumerable<Manufacturer> manufacturers, CancellationToken cancellationToken); 
}
