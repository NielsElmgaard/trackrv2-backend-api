using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using trackrv2_efc.Entities;


namespace trackrv2_efc;

public class TrackrContext : DbContext, IDataProtectionKeyContext
{

    public TrackrContext(DbContextOptions<TrackrContext> options) : base(options)
    {
    }
    public DbSet<Tracker> Trackers => Set<Tracker>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<TrackerEntry> TrackerEntries => Set<TrackerEntry>();
    public DbSet<EntryValue> EntryValues => Set<EntryValue>();

    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    public DbSet<User> Users => Set<User>();

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tracker>()
            .HasMany(t => t.Fields)
            .WithOne(f => f.Tracker)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tracker>()
            .HasMany(t => t.Entries)
            .WithOne(e => e.Tracker)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FieldDefinition>()
            .HasMany(f => f.Values)
            .WithOne(v => v.FieldDefinition)
            .HasForeignKey(v => v.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrackerEntry>()
            .HasMany(e => e.Values)
            .WithOne(v => v.TrackerEntry)
            .HasForeignKey(v => v.TrackerEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // User
        modelBuilder.Entity<User>()
       .HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(c => c.PhoneNumber).IsUnique();
        modelBuilder.Entity<User>()
        .HasMany(u => u.Trackers)
        .WithOne(t => t.User)
        .HasForeignKey(t => t.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        // Unique Tracker name per user
        modelBuilder.Entity<Tracker>()
            .HasIndex(t => new { t.Name, t.UserId })
            .IsUnique();

        // Follower
        modelBuilder.Entity<UserFollow>()
        .HasOne(uf => uf.Follower)
        .WithMany(u => u.Following)
        .HasForeignKey(uf => uf.FollowerId)
        .OnDelete(DeleteBehavior.Restrict);

        // Following
        modelBuilder.Entity<UserFollow>()
        .HasOne(uf => uf.Following)
        .WithMany(u => u.Followers)
        .HasForeignKey(uf => uf.FollowingId)
        .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<User>()
        .Property(u => u.Id)
        .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<EntryValue>()
        .Property(e => e.Id)
        .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<FieldDefinition>()
        .Property(f => f.Id)
        .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Tracker>()
        .Property(t => t.Id)
        .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<TrackerEntry>()
        .Property(t => t.Id)
        .HasDefaultValueSql("gen_random_uuid()");

        // Composite PK
        modelBuilder.Entity<UserFollow>().HasKey(u => new { u.FollowerId, u.FollowingId });
    }

    public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        // Automatically update audit fields
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdated = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.LastUpdated = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}