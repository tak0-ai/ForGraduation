using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace RuralTourism.Api.Migrations
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; } = default!;
        public DbSet<UserProfile> UserProfiles { get; set; } = default!;
        public DbSet<Resource> Resources { get; set; } = default!;
        public DbSet<Accommodation> Accommodations { get; set; } = default!;
        public DbSet<Attraction> Attractions { get; set; } = default!;
        public DbSet<Dining> Dinings { get; set; } = default!;
        public DbSet<FolkActivity> FolkActivities { get; set; } = default!;
        public DbSet<BeautifulVillage> BeautifulVillages { get; set; } = default!;
        public DbSet<Media> Medias { get; set; } = default!;
        public DbSet<ResourcePhoto> ResourcePhotos { get; set; } = default!;
        public DbSet<ResourceReview> ResourceReviews { get; set; } = default!;
        public DbSet<UserWallMessage> UserWallMessages { get; set; } = default!;
        public DbSet<Post> Posts { get; set; } = default!;
        public DbSet<PostBlock> PostBlocks { get; set; } = default!;
        public DbSet<Comment> Comments { get; set; } = default!;
        public DbSet<Reaction> Reactions { get; set; } = default!;
        public DbSet<UserFollow> UserFollows { get; set; } = default!;
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<Itinerary> Itineraries { get; set; } = default!;
        public DbSet<ItineraryItem> ItineraryItems { get; set; } = default!;
        public DbSet<InteractionEvent> InteractionEvents { get; set; } = default!;
        public DbSet<ChatRoom> ChatRooms { get; set; } = default!;
        public DbSet<ChatRequest> ChatRequests { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; } = default!;
        public DbSet<TourPlan> TourPlans { get; set; } = default!;
        public DbSet<TourPlanMember> TourPlanMembers { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserFollow: 复合主键 + 关系
            modelBuilder.Entity<UserFollow>()
                .HasKey(nameof(UserFollow.FollowerId), nameof(UserFollow.FollowingId));

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(uf => uf.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFollow>()
                .HasOne(uf => uf.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(uf => uf.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserProfile 一对一 (外键已在实体中标注)
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.UserId);

            // Post 相关集合
            modelBuilder.Entity<Post>()
                .HasMany(p => p.Blocks)
                .WithOne(b => b.Post)
                .HasForeignKey(b => b.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Reactions)
                .WithOne(r => r.Post)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasMany(c => c.Reactions)
                .WithOne(r => r.Comment)
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment 自引用（楼中楼）
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany()
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Media -> Uploader（若未定义反向集合，则用 WithMany()）
            modelBuilder.Entity<Media>()
                .HasOne(m => m.Uploader)
                .WithMany()
                .HasForeignKey(m => m.UploaderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Resource)
                .WithMany()
                .HasForeignKey(rp => rp.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Media)
                .WithMany()
                .HasForeignKey(rp => rp.MediaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResourcePhoto>()
                .HasOne(rp => rp.Uploader)
                .WithMany()
                .HasForeignKey(rp => rp.UploaderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ResourcePhoto>()
                .HasIndex(rp => new { rp.ResourceId, rp.CreatedAt });

            modelBuilder.Entity<ResourceReview>()
                .HasOne(rr => rr.Resource)
                .WithMany()
                .HasForeignKey(rr => rr.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResourceReview>()
                .HasOne(rr => rr.User)
                .WithMany()
                .HasForeignKey(rr => rr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResourceReview>()
                .HasIndex(rr => new { rr.ResourceId, rr.UserId })
                .IsUnique();

            modelBuilder.Entity<UserWallMessage>()
                .HasOne(m => m.TargetUser)
                .WithMany()
                .HasForeignKey(m => m.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserWallMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserWallMessage>()
                .HasIndex(m => new { m.TargetUserId, m.CreatedAt });

            // ChatMember 复合主键
            modelBuilder.Entity<ChatMember>()
                .HasKey(cm => new { cm.ChatRoomId, cm.UserId });

            // TourPlanMember 复合主键
            modelBuilder.Entity<TourPlanMember>()
                .HasKey(tm => new { tm.TourPlanId, tm.UserId });

            // TourPlan <-> ChatRoom 一对一
            modelBuilder.Entity<ChatRoom>()
                .HasOne(cr => cr.TravelPlan)
                .WithOne(tp => tp.ChatRoom)
                .HasForeignKey<ChatRoom>(cr => cr.TravelPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            // 根据需要添加索引（示例）
            modelBuilder.Entity<Resource>()
                .HasIndex(r => r.Tags);
            
            // AppUser UserNo 自动增长与唯一索引
            modelBuilder.Entity<AppUser>()
                .Property(u => u.UserNo)
                .ValueGeneratedOnAdd();
            
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.UserNo)
                .IsUnique();

            // ChatRoom RoomNo Auto-increment and Index
            modelBuilder.Entity<ChatRoom>()
                .Property(r => r.RoomNo)
                .ValueGeneratedOnAdd();
            
            modelBuilder.Entity<ChatRoom>()
                .HasIndex(r => r.RoomNo)
                .IsUnique();

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.TriggerUser)
                .WithMany()
                .HasForeignKey(n => n.TriggerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatRequest>()
                .HasOne(cr => cr.Requester)
                .WithMany()
                .HasForeignKey(cr => cr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRequest>()
                .HasOne(cr => cr.TargetUser)
                .WithMany()
                .HasForeignKey(cr => cr.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatRequest>()
                .HasOne(cr => cr.TargetGroup)
                .WithMany()
                .HasForeignKey(cr => cr.TargetGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
