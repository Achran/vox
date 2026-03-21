using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vox.Domain.Entities;

namespace Vox.Infrastructure.Data.Configurations;

public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.IconUrl).HasMaxLength(500);
        builder.HasMany(s => s.Channels).WithOne().HasForeignKey(c => c.ServerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Topic).HasMaxLength(1024);
        builder.Property(c => c.Type).HasConversion<string>();
        builder.HasMany(c => c.Messages).WithOne().HasForeignKey(m => m.ChannelId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Content).HasMaxLength(2000).IsRequired();
        builder.HasIndex(m => m.ChannelId);
        builder.HasIndex(m => m.AuthorId);
    }
}
