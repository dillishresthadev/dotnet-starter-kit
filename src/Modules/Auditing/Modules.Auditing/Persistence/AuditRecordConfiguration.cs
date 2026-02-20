using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Auditing.Persistence;

public class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AuditRecords", "audit");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.Severity).HasConversion<byte>();
        builder.Property(x => x.Tags).HasConversion<long>();
     //   builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAtUtc);


        // Access the provider name from the model metadata
        var providerName = builder.Metadata.Model.GetProductVersion();
        // Note: If you want the actual Provider string, use this:
        var provider = builder.Metadata.Model["Relational:MaxIdentifierLength"];

        // THE MOST RELIABLE WAY inside IEntityTypeConfiguration:
        if (builder.Metadata.Model.FindAnnotation("Relational:MaxIdentifierLength") != null)
        {
            // We can check if we are using Npgsql specifically
            var isPostgres = builder.Metadata.Model.GetAnnotations()
                .Any(a => a.Value?.ToString()?.Contains("Npgsql") ?? false);

            if (isPostgres)
            {
                builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
            }
            else
            {
                // Default to MSSQL behavior
                builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
               // builder.Property(x => x.PayloadJson).HasColumnType("json");
            }
        }
    }
}
