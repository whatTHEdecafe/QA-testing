using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QaAutomation.Core.Targets;
using QaAutomation.Infrastructure.Persistence;
using QaAutomation.Infrastructure.Targets;

namespace QaAutomation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QaAutomation")
            ?? throw new InvalidOperationException("Connection string 'QaAutomation' is not configured.");
        services.AddDbContext<QaAutomationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITargetService, TargetService>();
        return services;
    }
}
