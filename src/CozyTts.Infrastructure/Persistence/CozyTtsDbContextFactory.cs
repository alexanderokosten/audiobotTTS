using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CozyTts.Infrastructure.Persistence;

public sealed class CozyTtsDbContextFactory : IDesignTimeDbContextFactory<CozyTtsDbContext>
{
    public CozyTtsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=cozytts;Username=cozytts;Password=cozytts";

        var options = new DbContextOptionsBuilder<CozyTtsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CozyTtsDbContext(options);
    }
}
