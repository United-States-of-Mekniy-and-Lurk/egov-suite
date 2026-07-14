using Ego.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ego.Infrastructure.Persistence;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(person => person.Id);
        builder.Property(person => person.Id)
            .HasColumnName("id");

        builder.Property(person => person.IdentitySubject)
            .HasColumnName("identity_subject")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(person => person.IdentitySubject)
            .IsUnique();

        builder.Property(person => person.PreferredUsername)
            .HasColumnName("preferred_username")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(person => person.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(person => person.Email)
            .HasColumnName("email")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(person => person.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(person => person.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(person => person.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
