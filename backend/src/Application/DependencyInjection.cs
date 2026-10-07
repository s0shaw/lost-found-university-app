using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Claims;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case handlers and validators. Add new handlers here as you build features.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateReportValidator>();

        services.AddSingleton<AttemptGuard>();

        services.AddScoped<CreateReportHandler>();
        services.AddScoped<CreateClaimHandler>();
        services.AddScoped<SearchReportsHandler>();
        services.AddScoped<GetReportHandler>();
        services.AddScoped<TrackReportHandler>();
        services.AddScoped<ListCategoriesHandler>();
        services.AddScoped<ListLocationsHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<ListStaffReportsHandler>();
        services.AddScoped<GetStaffReportHandler>();
        services.AddScoped<ListStaffClaimsHandler>();
        services.AddScoped<GetStaffClaimHandler>();
        services.AddScoped<ReportActionsHandler>();
        services.AddScoped<DecideClaimHandler>();
        services.AddScoped<CancelOwnReportHandler>();

        return services;
    }
}
