using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Categories;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<UniversityMember> UniversityMembers => Set<UniversityMember>();
    public DbSet<StaffAccount> StaffAccounts => Set<StaffAccount>();
    public DbSet<UniversityLocation> UniversityLocations => Set<UniversityLocation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ItemReport> ItemReports => Set<ItemReport>();
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<> in this assembly (see Configurations/).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
