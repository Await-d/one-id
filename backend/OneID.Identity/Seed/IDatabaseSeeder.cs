using System.Threading.Tasks;

namespace OneID.Identity.Seed;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
