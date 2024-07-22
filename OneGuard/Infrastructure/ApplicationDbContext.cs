using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Endpoint = OneGuard.Core.Endpoint;

namespace OneGuard.Infrastructure;

internal sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Endpoint> Endpoints { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new EndpointTypeConfiguration());
        base.OnModelCreating(builder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }


    private class EndpointTypeConfiguration : IEntityTypeConfiguration<Core.Endpoint>
    {
        public void Configure(EntityTypeBuilder<Endpoint> builder)
        {
            builder.ToTable("endpoint")
                .HasKey(model => model.Id);


            builder.Property(endpoint => endpoint.Url)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .HasColumnName("url");

            builder.Property(endpoint => endpoint.Content)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .HasColumnName("content");

            builder.Property(endpoint => endpoint.Length)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .HasColumnName("length");

            builder.Property(endpoint => endpoint.OtpTtl)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .HasColumnName("otp_ttl");


            builder.Property(endpoint => endpoint.SecretTtl)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .HasColumnName("secret_ttl");
        }
    }
}