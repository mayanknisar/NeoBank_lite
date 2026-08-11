using AccountService.Models;
using Microsoft.EntityFrameworkCore;

public class SqliteDbContext : DbContext
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<KycStatus> KycStatuses { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(a => a.AccountId);
            entity.Property(a => a.AccountId)
                .HasColumnName("account_id")
                .HasColumnType("TEXT")
                .HasConversion<string>();
            entity.Property(a => a.CustomerId)
                .HasColumnName("customer_id")
                .HasColumnType("TEXT")
                .HasConversion<string>();
            entity.Property(a => a.AccountNumber).HasColumnName("account_number");
            entity.Property(a => a.AccountType).HasColumnName("account_type");
            entity.Property(a => a.Balance).HasColumnName("balance");
            entity.Property(a => a.Status).HasColumnName("status");
            entity.Property(a => a.Version).HasColumnName("version");

        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(c => c.CustomerId);
            entity.Property(c => c.CustomerId).HasColumnName("customer_id");
            entity.Property(c => c.FullName).HasColumnName("full_name");
            entity.Property(c => c.Email).HasColumnName("email");
            entity.Property(c => c.Phone).HasColumnName("phone");
            entity.Property(c => c.DateOfBirth).HasColumnName("date_of_birth");
        });

        modelBuilder.Entity<KycStatus>(entity =>
        {
            entity.ToTable("kyc_status");
            entity.HasKey(k => k.KycId);
            entity.Property(k => k.KycId).HasColumnName("kyc_id");
            entity.Property(k => k.CustomerId).HasColumnName("customer_id");
            entity.Property(k => k.Status).HasColumnName("status");
            entity.Property(k => k.VerifiedAt).HasColumnName("verified_at");
        });
    }
}
