using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mezon_sdk.Messages
{
    public class MessageEntity
    {
        public string Id { get; set; } = null!;
        public string ChannelId { get; set; } = null!;
        public string? ClanId { get; set; }
        public string? SenderId { get; set; }
        public string? Content { get; set; }
        public string? Mentions { get; set; }
        public string? Attachments { get; set; }
        public string? Reactions { get; set; }
        public string? MsgReferences { get; set; }
        public string? TopicId { get; set; }
        public long? CreateTimeSeconds { get; set; }
    }

    public class MessageDbContext : DbContext
    {
        public DbSet<MessageEntity> Messages { get; set; }
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageEntity>()
                .HasKey(m => new { m.Id, m.ChannelId });

            modelBuilder.Entity<MessageEntity>()
                .HasIndex(m => m.ChannelId)
                .HasDatabaseName("idx_messages_channel_id");
        }
    }
}
