namespace RuralTourism.Web.Client.Models;

public class ResourceDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AverageRating { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttractionDto : ResourceDto
{
    public string? OpeningHours { get; set; }
    public string? TicketPrice { get; set; }
}

public class AccommodationDto : ResourceDto
{
    public int StarRating { get; set; }
    public string? Amenities { get; set; }
    public string? RoomTypes { get; set; }
}

public class DiningDto : ResourceDto
{
    public string? CuisineType { get; set; }
    public string? PriceRange { get; set; }
    public string? SignatureDishes { get; set; }
}

public class FolkActivityDto : ResourceDto
{
    public DateTime? EventDate { get; set; }
    public string? Duration { get; set; }
    public string? Organizer { get; set; }
}

public class BeautifulVillageDto : ResourceDto
{
    public string? VillageType { get; set; }
    public string? FamousFor { get; set; }
}

public class RecommendedResourceDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public double AverageRating { get; set; }
    public string? CoverMediaId { get; set; }
    public string ResourceType { get; set; } = "attractions";
}

public class PopularBeautifulVillageDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? CoverMediaId { get; set; }
    public double AverageRating { get; set; }
    public double HeatScore { get; set; }
    public double TotalScore { get; set; }
}

public class ResourcePhotoDto
{
    public string Id { get; set; } = null!;
    public string ResourceId { get; set; } = null!;
    public string MediaId { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? UploaderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResourcePhotoCreateDto
{
    public string MediaId { get; set; } = null!;
}

public class ResourceReviewDto
{
    public string Id { get; set; } = null!;
    public string ResourceId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? UserNo { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ResourceReviewCreateDto
{
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class AttractionCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public string? OpeningHours { get; set; }
    public string? TicketPrice { get; set; }
}

public class AccommodationCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public int StarRating { get; set; }
    public string? Amenities { get; set; }
    public string? RoomTypes { get; set; }
}

public class DiningCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public string? CuisineType { get; set; }
    public string? PriceRange { get; set; }
    public string? SignatureDishes { get; set; }
}

public class FolkActivityCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public DateTime? EventDate { get; set; }
    public string? Duration { get; set; }
    public string? Organizer { get; set; }
}

public class BeautifulVillageCreateDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Tags { get; set; }
    public string? CoverMediaId { get; set; }
    public string? VillageType { get; set; }
    public string? FamousFor { get; set; }
}
