using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LibraryApi.Domain.Entities;
using StatusEntity = LibraryApi.Domain.Entities.Status;

namespace LibraryApi.Infrastructure.Persistence.Configurations;

public class StatusConfiguration : IEntityTypeConfiguration<StatusEntity>
{
    public void Configure(EntityTypeBuilder<StatusEntity> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasData(new StatusEntity
        {
            Id = 1,
            Value = "OK"
        });
    }
}



