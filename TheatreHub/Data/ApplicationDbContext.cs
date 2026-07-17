using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Models;

namespace TheatreHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

    public DbSet<Act> Acts => Set<Act>();

    public DbSet<Scene> Scenes => Set<Scene>();

    public DbSet<SceneRole> SceneRoles => Set<SceneRole>();

    public DbSet<TheatreTask> TheatreTasks =>
    Set<TheatreTask>();

    public DbSet<ProductionItem> ProductionItems =>
    Set<ProductionItem>();
    public DbSet<PerformanceShow> PerformanceShows =>
    Set<PerformanceShow>();
    public DbSet<TheatreTaskComment> TheatreTaskComments =>
    Set<TheatreTaskComment>();
    public DbSet<BudgetTransaction> BudgetTransactions { get; set; }
    public DbSet<NotificationReadState> NotificationReadStates { get; set; }
    public DbSet<UserActionLog> UserActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(user => user.Actor)
            .WithMany()
            .HasForeignKey(user => user.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NotificationReadState>()
      .HasIndex(state => new
      {
        state.UserId,
        state.NotificationKey
      })
      .IsUnique();

        modelBuilder.Entity<NotificationReadState>()
            .Property(state => state.NotificationKey)
            .HasMaxLength(500);

        modelBuilder.Entity<UserActionLog>()
    .HasIndex(log => log.UserId);

        modelBuilder.Entity<UserActionLog>()
            .HasIndex(log => log.ActionType);

        modelBuilder.Entity<UserActionLog>()
            .HasIndex(log => log.EntityType);

        modelBuilder.Entity<UserActionLog>()
            .HasIndex(log => log.CreatedAt);

        ConfigureCharacterRoles(modelBuilder);
        ConfigureRoleAssignments(modelBuilder);
        ConfigureRehearsals(modelBuilder);
        ConfigureRehearsalParticipants(modelBuilder);
        ConfigureVenuesAndHalls(modelBuilder);
        ConfigureActsAndScenes(modelBuilder);
        ConfigureSceneRoles(modelBuilder);
        ConfigureTheatreTasks(modelBuilder);
        ConfigureTheatreTaskComments(modelBuilder);
        ConfigureProductionItems(modelBuilder);
        ConfigurePerformanceShows(modelBuilder);
        ConfigureBudgetTransactions(modelBuilder);
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
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rehearsal>()
            .HasOne(rehearsal => rehearsal.Hall)
            .WithMany(hall => hall.Rehearsals)
            .HasForeignKey(rehearsal => rehearsal.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rehearsal>()
            .HasOne(rehearsal => rehearsal.Act)
            .WithMany(act => act.Rehearsals)
            .HasForeignKey(rehearsal => rehearsal.ActId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rehearsal>()
            .HasOne(rehearsal => rehearsal.Scene)
            .WithMany(scene => scene.Rehearsals)
            .HasForeignKey(rehearsal => rehearsal.SceneId)
            .OnDelete(DeleteBehavior.Restrict);
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
    private static void ConfigureActsAndScenes(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Act>()
            .HasOne(act => act.Performance)
            .WithMany(performance => performance.Acts)
            .HasForeignKey(act => act.PerformanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Act>()
            .HasIndex(act => new
            {
                act.PerformanceId,
                act.Number
            })
            .IsUnique();

        modelBuilder.Entity<Scene>()
            .HasOne(scene => scene.Act)
            .WithMany(act => act.Scenes)
            .HasForeignKey(scene => scene.ActId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Scene>()
            .HasIndex(scene => new
            {
                scene.ActId,
                scene.Number
            })
            .IsUnique();
    }
    private static void ConfigureSceneRoles(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SceneRole>()
            .HasKey(sceneRole => new
            {
                sceneRole.SceneId,
                sceneRole.CharacterRoleId
            });

        modelBuilder.Entity<SceneRole>()
            .HasOne(sceneRole => sceneRole.Scene)
            .WithMany(scene => scene.SceneRoles)
            .HasForeignKey(sceneRole => sceneRole.SceneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SceneRole>()
            .HasOne(sceneRole => sceneRole.CharacterRole)
            .WithMany(role => role.SceneRoles)
            .HasForeignKey(sceneRole =>
                sceneRole.CharacterRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SceneRole>()
            .HasIndex(sceneRole =>
                sceneRole.CharacterRoleId);
    }

    private static void ConfigureTheatreTasks(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TheatreTask>()
            .HasOne(task => task.Performance)
            .WithMany()
            .HasForeignKey(task => task.PerformanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TheatreTask>()
            .HasOne(task => task.Act)
            .WithMany()
            .HasForeignKey(task => task.ActId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TheatreTask>()
            .HasOne(task => task.Scene)
            .WithMany()
            .HasForeignKey(task => task.SceneId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TheatreTask>()
            .HasIndex(task => task.PerformanceId);

        modelBuilder.Entity<TheatreTask>()
            .HasIndex(task => task.ActId);

        modelBuilder.Entity<TheatreTask>()
            .HasIndex(task => task.SceneId);

        modelBuilder.Entity<TheatreTask>()
            .HasIndex(task => task.Status);

        modelBuilder.Entity<TheatreTask>()
            .HasIndex(task => task.Deadline);
    }

    private static void ConfigureProductionItems(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductionItem>()
            .HasOne(item => item.Performance)
            .WithMany()
            .HasForeignKey(item => item.PerformanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionItem>()
            .HasOne(item => item.Act)
            .WithMany()
            .HasForeignKey(item => item.ActId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionItem>()
            .HasOne(item => item.Scene)
            .WithMany()
            .HasForeignKey(item => item.SceneId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.PerformanceId);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.ActId);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.SceneId);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.Type);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.Status);

        modelBuilder.Entity<ProductionItem>()
            .HasIndex(item => item.NeededBy);
    }
    private static void ConfigureTheatreTaskComments(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TheatreTaskComment>()
            .HasOne(comment => comment.TheatreTask)
            .WithMany()
            .HasForeignKey(comment => comment.TheatreTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TheatreTaskComment>()
            .HasIndex(comment => comment.TheatreTaskId);

        modelBuilder.Entity<TheatreTaskComment>()
            .HasIndex(comment => comment.CreatedAt);
    }

    private static void ConfigurePerformanceShows(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PerformanceShow>()
            .HasOne(show => show.Performance)
            .WithMany()
            .HasForeignKey(show => show.PerformanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PerformanceShow>()
            .HasOne(show => show.Hall)
            .WithMany()
            .HasForeignKey(show => show.HallId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PerformanceShow>()
            .HasIndex(show => show.PerformanceId);

        modelBuilder.Entity<PerformanceShow>()
            .HasIndex(show => show.HallId);

        modelBuilder.Entity<PerformanceShow>()
            .HasIndex(show => show.StartDateTime);

        modelBuilder.Entity<PerformanceShow>()
            .HasIndex(show => show.Status);

        modelBuilder.Entity<PerformanceShow>()
            .HasIndex(show => show.Type);
    }

    private static void ConfigureBudgetTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BudgetTransaction>()
            .HasOne(transaction => transaction.Performance)
            .WithMany()
            .HasForeignKey(transaction => transaction.PerformanceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BudgetTransaction>()
            .HasOne(transaction => transaction.PerformanceShow)
            .WithMany()
            .HasForeignKey(transaction => transaction.PerformanceShowId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BudgetTransaction>()
            .HasOne(transaction => transaction.ProductionItem)
            .WithMany()
            .HasForeignKey(transaction => transaction.ProductionItemId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BudgetTransaction>()
            .Property(transaction => transaction.PlannedAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BudgetTransaction>()
            .Property(transaction => transaction.ActualAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BudgetTransaction>()
            .Property(transaction => transaction.TicketPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.PerformanceId);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.PerformanceShowId);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.ProductionItemId);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.Type);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.Category);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.Status);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.TransactionDate);

        modelBuilder.Entity<BudgetTransaction>()
            .HasIndex(transaction => transaction.Currency);
    }

}