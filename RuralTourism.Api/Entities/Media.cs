namespace RuralTourism.Api.Entities
{
    public class Media
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UploaderId { get; set; } // 关联到 AppUser
        public AppUser? Uploader { get; set; }

        public required string Url { get; set; } // 文件访问的公开 URL 
        public required string MimeType { get; set; } // 例如 "image/jpeg" 或 "video/mp4"
        public long FileSize { get; set; } // 文件大小 (Bytes)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
