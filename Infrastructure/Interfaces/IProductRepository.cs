using Infrastructure.Models;
// säger att den hanterar produkter, oavsett lagring
namespace Infrastructure.Interfaces;
public interface IProductRepository : IRepository<Product>
{
    // Metoder som är specifika för Product

}
