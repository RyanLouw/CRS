using CRS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CRS.Core.Data;

public sealed class CallRatingContext(DbContextOptions<CallRatingContext> options) : DbContext(options)
{
    public DbSet<CallRatingEntity> CallRatings => Set<CallRatingEntity>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<HelpDeskStatus> Statuses => Set<HelpDeskStatus>();
    public DbSet<HelpDeskReason> Reasons => Set<HelpDeskReason>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var rating = model.Entity<CallRatingEntity>();
        rating.ToTable("CallRating");
        rating.HasKey(x => x.CallRatingID);
        rating.Property(x => x.call_id).IsUnicode(false);
        rating.Property(x => x.CaptureDate).HasColumnType("datetime");
        rating.Property(x => x.CallDateTime).HasColumnType("datetime");
        rating.Property(x => x.ITUpdateDate).HasColumnType("datetime");

        var problem = model.Entity<Problem>();
        problem.ToTable("Lookup_Problems"); problem.HasKey(x => x.Id);
        problem.Property(x => x.Id).HasColumnName("ProblemsID"); problem.Property(x => x.Name).HasColumnName("Problems");
        var pharmacy = model.Entity<Pharmacy>();
        pharmacy.ToTable("Lookup_Pharmacy"); pharmacy.HasKey(x => x.Id);
        pharmacy.Property(x => x.Id).HasColumnName("PharmacyID"); pharmacy.Property(x => x.Name).HasColumnName("Pharmacy");
        var status = model.Entity<HelpDeskStatus>();
        status.ToTable("Lookup_HelpDeskStatus"); status.HasKey(x => x.Id);
        status.Property(x => x.Id).HasColumnName("HelpDeskStatusID"); status.Property(x => x.Name).HasColumnName("HelpDeskStatus");
        var reason = model.Entity<HelpDeskReason>();
        reason.ToTable("Lookup_HelpDeskReason"); reason.HasKey(x => x.Id);
        reason.Property(x => x.Id).HasColumnName("HelpDeskReasonID"); reason.Property(x => x.Name).HasColumnName("HelpDeskReason");
    }
}
