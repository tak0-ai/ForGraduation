using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;

namespace RuralTourism.Api.Migrations
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Accommodation> Accommodations { get; set; } = null!;
        public DbSet<AppUser> AppUsers { get; set; } = null!;
        public DbSet<Attraction> Attractions { get; set; } = null!;
        public DbSet<BeautifulVillage> BeautifulVillages { get; set; } = null!;
        public DbSet<ChatMember> ChatMembers { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<ChatRequest> ChatRequests { get; set; } = null!;
        public DbSet<ChatRoom> ChatRooms { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Dining> Dinings { get; set; } = null!;
        public DbSet<FolkActivity> FolkActivities { get; set; } = null!;
        public DbSet<InteractionEvent> InteractionEvents { get; set; } = null!;
        public DbSet<Media> Medias { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Post> Posts { get; set; } = null!;
        public DbSet<PostBlock> PostBlocks { get; set; } = null!;
        public DbSet<Reaction> Reactions { get; set; } = null!;
        public DbSet<Resource> Resources { get; set; } = null!;
        public DbSet<ResourcePhoto> ResourcePhotos { get; set; } = null!;
        public DbSet<ResourceReview> ResourceReviews { get; set; } = null!;
        public DbSet<TourPlan> TourPlans { get; set; } = null!;
        public DbSet<UserFollow> UserFollows { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<UserWallMessage> UserWallMessages { get; set; } = null!;
        public DbSet<OperationLog> OperationLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatMember>()
                .HasKey(cm => new { cm.ChatRoomId, cm.UserId });

            modelBuilder.Entity<UserFollow>()
                .HasKey(uf => new { uf.FollowerId, uf.FollowingId });

            modelBuilder.Entity<ChatMember>()
                .HasOne(cm => cm.ChatRoom)
                .WithMany(cr => cr.Members)
                .HasForeignKey(cm => cm.ChatRoomId);

            modelBuilder.Entity<ChatMember>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.ChatMemberships)
                .HasForeignKey(cm => cm.UserId);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.TriggerUser)
                .WithMany()
                .HasForeignKey(n => n.TriggerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserWallMessage>()
                .HasOne(m => m.TargetUser)
                .WithMany()
                .HasForeignKey(m => m.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserWallMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany()
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Resource)
                .WithMany()
                .HasForeignKey(rp => rp.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Media)
                .WithMany()
                .HasForeignKey(rp => rp.MediaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Uploader)
                .WithMany()
                .HasForeignKey(rp => rp.UploaderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OperationLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ActionName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RequestPayload).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ActionName);
            });
        }
    }
}