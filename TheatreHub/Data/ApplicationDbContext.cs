using Microsoft.EntityFrameworkCore;
using TheatreHub.Models;

namespace TheatreHub.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Performance> Performances =>
        Set<Performance>();

    public DbSet<Actor> Actors =>
        Set<Actor>();

    public DbSet<CharacterRole> CharacterRoles =>
        Set<CharacterRole>();

    public DbSet<RoleAssignment> RoleAssignments =>
        Set<RoleAssignment>();

    public DbSet<Rehearsal> Rehearsals =>
        Set<Rehearsal>();

    public DbSet<RehearsalParticipant> RehearsalParticipants =>
        Set<RehearsalParticipant>();

    public DbSet<Venue> Venues =>
        Set<Venue>();

    public DbSet<Hall> Halls =>
        Set<Hall>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCharacterRoles(modelBuilder);
        ConfigureRoleAssignments(modelBuilder);
        ConfigureRehearsals(modelBuilder);
        ConfigureRehearsalParticipants(modelBuilder);
        ConfigureVenuesAndHalls(modelBuilder);
    }

    private static void ConfigureCharacterRoles(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CharacterRole>()
            .HasOne(role => role.Performance)
            .WithMany(performance => performance.CharacterRoles)
            .HasForeignKey(role => role.PerformanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CharacterRole>()
            .HasIndex(role => new
            {
                role.PerformanceId,
                role.Name
            });
    }

    private static void ConfigureRoleAssignments(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleAssignment>()
            .HasOne(assignment => assignment.CharacterRole)
            .WithMany(role => role.Assignments)
            .HasForeignKey(assignment => assignment.CharacterRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoleAssignment>()
            .HasOne(assignment => assignment.Actor)
            .WithMany(actor => actor.RoleAssignments)
            .HasForeignKey(assignment => assignment.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleAssignment>()
            .HasIndex(assignment => assignment.CharacterRoleId);

        modelBuilder.Entity<RoleAssignment>()
            .HasIndex(assignment => assignment.ActorId);

        modelBuilder.Entity<RoleAssignment>()
            .HasIndex(assignment => new
            {
                assignment.CharacterRoleId,
                assignment.ActorId,
                assignment.StartDate
            })
            .IsUnique();
    }

    private static void ConfigureRehearsals(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rehearsal>()
            .HasOne(rehearsal => rehearsal.Performance)
            .WithMany(performance => performance.Rehearsals)
            .HasForeignKey(rehearsal => rehearsal.PerformanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rehearsal>()
            .HasOne(rehearsal => rehearsal.Hall)
            .WithMany(hall => hall.Rehearsals)
            .HasForeignKey(rehearsal => rehearsal.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rehearsal>()
            .HasIndex(rehearsal => rehearsal.PerformanceId);

        modelBuilder.Entity<Rehearsal>()
            .HasIndex(rehearsal => rehearsal.HallId);

        modelBuilder.Entity<Rehearsal>()
            .HasIndex(rehearsal => rehearsal.StartDateTime);
    }

    private static void ConfigureRehearsalParticipants(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RehearsalParticipant>()
            .HasKey(participant => new
            {
                participant.RehearsalId,
                participant.ActorId
            });

        modelBuilder.Entity<RehearsalParticipant>()
            .HasOne(participant => participant.Rehearsal)
            .WithMany(rehearsal => rehearsal.Participants)
            .HasForeignKey(participant => participant.RehearsalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RehearsalParticipant>()
            .HasOne(participant => participant.Actor)
            .WithMany(actor => actor.RehearsalParticipants)
            .HasForeignKey(participant => participant.ActorId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureVenuesAndHalls(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hall>()
            .HasOne(hall => hall.Venue)
            .WithMany(venue => venue.Halls)
            .HasForeignKey(hall => hall.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hall>()
            .HasIndex(hall => new
            {
                hall.VenueId,
                hall.Name
            })
            .IsUnique();

        modelBuilder.Entity<Hall>()
            .Property(hall => hall.RentalCost)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Venue>()
            .HasIndex(venue => venue.Name);
    }
}