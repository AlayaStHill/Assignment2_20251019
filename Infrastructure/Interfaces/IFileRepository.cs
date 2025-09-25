using Infrastructure.Models;

namespace Infrastructure.Interfaces;
public interface IFileRepository
{
    Task<IEnumerable<Product>> ReadAsync(CancellationToken cancellationToken);
    Task WriteAsync(IEnumerable<Product> products, CancellationToken cancellationToken);

}
