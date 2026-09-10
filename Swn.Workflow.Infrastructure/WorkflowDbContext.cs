using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public class WorkflowDbContext
    : IdentityDbContext<ApplicationUser>
{
    public WorkflowDbContext(
        DbContextOptions<WorkflowDbContext> options)
        : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions =>
        Set<WorkflowDefinition>();

    public DbSet<WorkflowVersion> WorkflowVersions =>
        Set<WorkflowVersion>();

    public DbSet<TaskDefinition> TaskDefinitions =>
        Set<TaskDefinition>();

    public DbSet<TaskFieldDefinition> TaskFieldDefinitions =>
        Set<TaskFieldDefinition>();

    public DbSet<WorkflowInstance> WorkflowInstances =>
        Set<WorkflowInstance>();

    public DbSet<WorkflowInstanceProperty> WorkflowInstanceProperties =>
        Set<WorkflowInstanceProperty>();

    public DbSet<TaskInstance> TaskInstances =>
        Set<TaskInstance>();

    public DbSet<TaskInstanceFieldValue> TaskInstanceFieldValues =>
        Set<TaskInstanceFieldValue>();

    public DbSet<TaskInstanceSecret> TaskInstanceSecrets =>
        Set<TaskInstanceSecret>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(200);
        });

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasIndex(x => x.Key)
                .IsUnique();
        });

        modelBuilder.Entity<WorkflowVersion>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new
            {
                x.WorkflowDefinitionId,
                x.VersionNumber
            })
            .IsUnique();

            entity.HasOne<WorkflowDefinition>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.Phase)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.AssignedRoleKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(x => new
            {
                x.WorkflowVersionId,
                x.Key
            })
            .IsUnique();

            entity.HasOne<WorkflowVersion>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskFieldDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => new
            {
                x.TaskDefinitionId,
                x.Key
            })
            .IsUnique();

            entity.HasOne<TaskDefinition>()
                .WithMany()
                .HasForeignKey(x => x.TaskDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Subject)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(x => x.CreatedByUserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasOne<WorkflowVersion>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkflowInstanceProperty>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.WorkflowInstanceId,
                x.Key
            })
            .IsUnique();

            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskInstance>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AssignedRoleKey)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.AssignedUserId)
                .HasMaxLength(450);

            entity.Property(x => x.Comment)
                .HasMaxLength(2000);

            entity.Property(x => x.CompletedByUserId)
                .HasMaxLength(450);

            entity.HasIndex(x => x.AssignedRoleKey);

            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<TaskDefinition>()
                .WithMany()
                .HasForeignKey(x => x.TaskDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TaskInstanceFieldValue>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.TaskInstanceId,
                x.Key
            })
            .IsUnique();

            entity.HasOne<TaskInstance>()
                .WithMany()
                .HasForeignKey(x => x.TaskInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskInstanceSecret>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.EncryptedValue)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.TaskInstanceId,
                x.Key
            })
            .IsUnique();

            entity.HasOne<TaskInstance>()
                .WithMany()
                .HasForeignKey(x => x.TaskInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}