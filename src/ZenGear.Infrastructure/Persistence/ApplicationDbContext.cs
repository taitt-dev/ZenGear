using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Common;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence;

/// <summary>
/// Application database context with Identity integration.
/// Handles domain events, audit fields, and transaction management.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<long>, long>
{
    private readonly IMediator _mediator;
    private readonly IDateTime _dateTime;
    private readonly ICurrentUserService? _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMediator mediator,
        IDateTime dateTime)
        : base(options)
    {
        _mediator = mediator;
        _dateTime = dateTime;
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMediator mediator,
        IDateTime dateTime,
        ICurrentUserService currentUser)
        : base(options)
    {
        _mediator = mediator;
        _dateTime = dateTime;
        _currentUser = currentUser;
    }

    // DbSets for Identity-related entities
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

    // DbSets for domain entities (will be added in future features)
    // public DbSet<Product> Products => Set<Product>();
    // public DbSet<Category> Categories => Set<Category>();
    // ... other DbSets

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Handle audit fields for BaseAuditableEntity
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            var now = _dateTime.UtcNow;
            var userId = _currentUser?.UserId ?? 0;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(userId, now);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedBy(userId, now);
                    break;
            }
        }

        // Handle CreatedAt/UpdatedAt for ApplicationUser (not BaseAuditableEntity)
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            var now = _dateTime.UtcNow;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        // Save changes to database
        var result = await base.SaveChangesAsync(ct);

        // Dispatch domain events AFTER SaveChanges succeeds
        await DispatchDomainEventsAsync(ct);

        return result;
    }

    /// <summary>
    /// Dispatch domain events from entities.
    /// Called after SaveChanges to ensure events are only raised for persisted changes.
    /// </summary>
    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before publishing to avoid re-raising
        entities.ForEach(e => e.ClearDomainEvents());

        // Publish events to handlers
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }
    }
}
