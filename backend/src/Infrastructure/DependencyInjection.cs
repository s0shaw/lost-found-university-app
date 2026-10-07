using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Repositories;
using UniversityLostFound.Infrastructure.Security;
using UniversityLostFound.Infrastructure.Time;

namespace UniversityLostFound.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Copy .env.example to .env or set ConnectionStrings__{ConnectionStringName}.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUniversityMemberRepository, UniversityMemberRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IItemReportRepository, ItemReportRepository>();
        services.AddScoped<IStaffAccountRepository, StaffAccountRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(JwtSettings.Read(configuration));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IStaffTokenIssuer, JwtStaffTokenIssuer>();

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}
