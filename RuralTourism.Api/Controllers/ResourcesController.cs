using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;
using System.Text.Json;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResourcesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id}/photos")]
    public async Task<ActionResult<List<ResourcePhotoDto>>> GetResourcePhotos(string id)
    {
        var exists = await _db.Resources.AnyAsync(x => x.Id == id);
        if (!exists) return NotFound();

        var items = await _db.ResourcePhotos
            .Where(x => x.ResourceId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ResourcePhotoDto
            {
                Id = x.Id,
                ResourceId = x.ResourceId,
                MediaId = x.MediaId,
                Url = x.Media != null ? x.Media.Url : string.Empty,
                UploaderId = x.UploaderId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id}/photos")]
    [Authorize]
    public async Task<ActionResult<ResourcePhotoDto>> AddResourcePhoto(string id, [FromBody] ResourcePhotoCreateDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound("资源不存在");

        var media = await _db.Medias.FindAsync(dto.MediaId);
        if (media == null) return BadRequest("媒体不存在");
        if (media.UploaderId != userId) return Forbid();

        var entity = new ResourcePhoto
        {
            ResourceId = id,
            MediaId = dto.MediaId,
            UploaderId = userId
        };

        _db.ResourcePhotos.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new ResourcePhotoDto
        {
            Id = entity.Id,
            ResourceId = entity.ResourceId,
            MediaId = entity.MediaId,
            Url = media.Url,
            UploaderId = entity.UploaderId,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpDelete("{id}/photos/{photoId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteResourcePhoto(string id, string photoId)
    {
        var photo = await _db.ResourcePhotos
            .FirstOrDefaultAsync(x => x.Id == photoId && x.ResourceId == id);

        if (photo == null) return NotFound();

        _db.ResourcePhotos.Remove(photo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/reviews")]
    public async Task<ActionResult<List<ResourceReviewDto>>> GetResourceReviews(string id)
    {
        var exists = await _db.Resources.AnyAsync(x => x.Id == id);
        if (!exists) return NotFound();

        var items = await _db.ResourceReviews
            .Where(x => x.ResourceId == id)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ResourceReviewDto
            {
                Id = x.Id,
                ResourceId = x.ResourceId,
                UserId = x.UserId,
                UserNo = x.User != null ? x.User.UserNo.ToString("D6") : null,
                UserName = x.User != null ? (x.User.Nickname ?? x.User.UserName) : "用户",
                AvatarUrl = x.User != null ? x.User.AvatarUrl : null,
                Rating = x.Rating,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{id}/reviews")]
    [Authorize]
    public async Task<ActionResult<ResourceReviewDto>> UpsertResourceReview(string id, [FromBody] ResourceReviewCreateDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        if (dto.Rating < 1 || dto.Rating > 5) return BadRequest("评分范围应为 1-5");

        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound("资源不存在");

        var entity = await _db.ResourceReviews.FirstOrDefaultAsync(x => x.ResourceId == id && x.UserId == userId);
        if (entity == null)
        {
            entity = new ResourceReview
            {
                ResourceId = id,
                UserId = userId,
                Rating = dto.Rating,
                Content = dto.Content ?? string.Empty
            };
            _db.ResourceReviews.Add(entity);
        }
        else
        {
            entity.Rating = dto.Rating;
            entity.Content = dto.Content ?? string.Empty;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await RecalculateResourceRatingAsync(id);

        var user = await _db.AppUsers.FindAsync(userId);
        return Ok(new ResourceReviewDto
        {
            Id = entity.Id,
            ResourceId = entity.ResourceId,
            UserId = entity.UserId,
            UserNo = user?.UserNo.ToString("D6"),
            UserName = user?.Nickname ?? user?.UserName ?? "用户",
            AvatarUrl = user?.AvatarUrl,
            Rating = entity.Rating,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpDelete("{id}/reviews/{reviewId}")]
    [Authorize]
    public async Task<IActionResult> DeleteResourceReview(string id, string reviewId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var review = await _db.ResourceReviews.FirstOrDefaultAsync(x => x.Id == reviewId && x.ResourceId == id);
        if (review == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && review.UserId != userId) return Forbid();

        _db.ResourceReviews.Remove(review);
        await _db.SaveChangesAsync();
        await RecalculateResourceRatingAsync(id);
        return NoContent();
    }

    private async Task RecalculateResourceRatingAsync(string resourceId)
    {
        var resource = await _db.Resources.FindAsync(resourceId);
        if (resource == null) return;

        var avg = await _db.ResourceReviews
            .Where(x => x.ResourceId == resourceId)
            .Select(x => (double?)x.Rating)
            .AverageAsync();

        resource.AverageRating = avg ?? 5;
        await _db.SaveChangesAsync();
    }

    #region Attractions
    [HttpGet("attractions")]
    public async Task<ActionResult<List<AttractionDto>>> GetAttractions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Attractions.AsQueryable();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AttractionDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                Tags = x.Tags,
                CoverMediaId = x.CoverMediaId,
                CreatedAt = x.CreatedAt,
                OpeningHours = x.OpeningHours,
                TicketPrice = x.TicketPrice,
                BestVisitTime = x.BestVisitTime
            }).ToListAsync();

        items = items.Where(x => IsRuralResourceEntry(x.Name, x.Description, x.Address, x.Tags)).ToList();
        return Ok(items);
    }

    [HttpGet("attractions/{id}")]
    public async Task<ActionResult<AttractionDto>> GetAttraction(string id)
    {
        var x = await _db.Attractions.FindAsync(id);
        if (x == null) return NotFound();
        return Ok(new AttractionDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
            Tags = x.Tags,
            CoverMediaId = x.CoverMediaId,
            CreatedAt = x.CreatedAt,
            OpeningHours = x.OpeningHours,
            TicketPrice = x.TicketPrice,
            BestVisitTime = x.BestVisitTime
        });
    }

    [HttpPost("attractions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AttractionDto>> CreateAttraction([FromBody] AttractionCreateDto dto)
    {
        try
        {
            if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
            {
                return BadRequest("景区资源必须以镇江乡村旅游为主，请补充村落、田园、农事、民宿、非遗等乡村属性。");
            }

            var item = new Attraction
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Tags = dto.Tags,
                CoverMediaId = dto.CoverMediaId,
                OpeningHours = dto.OpeningHours,
                TicketPrice = dto.TicketPrice,
                BestVisitTime = dto.BestVisitTime
            };
            _db.Attractions.Add(item);
            await _db.SaveChangesAsync();

            var resultDto = new AttractionDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AverageRating = item.AverageRating,
                Tags = item.Tags,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                OpeningHours = item.OpeningHours,
                TicketPrice = item.TicketPrice,
                BestVisitTime = item.BestVisitTime
            };
            return CreatedAtAction(nameof(GetAttraction), new { id = item.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("attractions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAttraction(string id)
    {
        var item = await _db.Attractions.FindAsync(id);
        if (item == null) return NotFound();
        _db.Attractions.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("attractions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AttractionDto>> UpdateAttraction(string id, [FromBody] AttractionCreateDto dto)
    {
        var item = await _db.Attractions.FindAsync(id);
        if (item == null) return NotFound();

        if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
        {
            return BadRequest("景区资源必须以镇江乡村旅游为主，请补充村落、田园、农事、民宿、非遗等乡村属性。");
        }

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.Latitude = dto.Latitude;
        item.Longitude = dto.Longitude;
        item.Tags = dto.Tags;
        item.CoverMediaId = dto.CoverMediaId;
        item.OpeningHours = dto.OpeningHours;
        item.TicketPrice = dto.TicketPrice;
        item.BestVisitTime = dto.BestVisitTime;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new AttractionDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            AverageRating = item.AverageRating,
            Tags = item.Tags,
            CoverMediaId = item.CoverMediaId,
            CreatedAt = item.CreatedAt,
            OpeningHours = item.OpeningHours,
            TicketPrice = item.TicketPrice,
            BestVisitTime = item.BestVisitTime
        });
    }
    #endregion

    #region Accommodations
    [HttpGet("accommodations")]
    public async Task<ActionResult<List<AccommodationDto>>> GetAccommodations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Accommodations.AsQueryable();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AccommodationDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                Tags = x.Tags,
                CoverMediaId = x.CoverMediaId,
                CreatedAt = x.CreatedAt,
                StarRating = x.StarRating,
                Amenities = x.Amenities,
                RoomTypes = x.RoomTypes
            }).ToListAsync();

        items = items.Where(x => IsRuralResourceEntry(x.Name, x.Description, x.Address, x.Tags)).ToList();
        return Ok(items);
    }

    [HttpGet("accommodations/{id}")]
    public async Task<ActionResult<AccommodationDto>> GetAccommodation(string id)
    {
        var x = await _db.Accommodations.FindAsync(id);
        if (x == null) return NotFound();
        return Ok(new AccommodationDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
            Tags = x.Tags,
            CoverMediaId = x.CoverMediaId,
            CreatedAt = x.CreatedAt,
            StarRating = x.StarRating,
            Amenities = x.Amenities,
            RoomTypes = x.RoomTypes
        });
    }

    [HttpPost("accommodations")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccommodationDto>> CreateAccommodation([FromBody] AccommodationCreateDto dto)
    {
        try
        {
            if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
            {
                return BadRequest("住宿资源必须为乡村民宿、农庄或田园住宿。");
            }

            var item = new Accommodation
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Tags = dto.Tags,
                CoverMediaId = dto.CoverMediaId,
                StarRating = dto.StarRating,
                Amenities = dto.Amenities,
                RoomTypes = dto.RoomTypes
            };
            _db.Accommodations.Add(item);
            await _db.SaveChangesAsync();

            var resultDto = new AccommodationDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AverageRating = item.AverageRating,
                Tags = item.Tags,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                StarRating = item.StarRating,
                Amenities = item.Amenities,
                RoomTypes = item.RoomTypes
            };
            return CreatedAtAction(nameof(GetAccommodation), new { id = item.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("accommodations/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAccommodation(string id)
    {
        var item = await _db.Accommodations.FindAsync(id);
        if (item == null) return NotFound();
        _db.Accommodations.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("accommodations/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AccommodationDto>> UpdateAccommodation(string id, [FromBody] AccommodationCreateDto dto)
    {
        var item = await _db.Accommodations.FindAsync(id);
        if (item == null) return NotFound();

        if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
        {
            return BadRequest("住宿资源必须为乡村民宿、农庄或田园住宿。");
        }

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.Latitude = dto.Latitude;
        item.Longitude = dto.Longitude;
        item.Tags = dto.Tags;
        item.CoverMediaId = dto.CoverMediaId;
        item.StarRating = dto.StarRating;
        item.Amenities = dto.Amenities;
        item.RoomTypes = dto.RoomTypes;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new AccommodationDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            AverageRating = item.AverageRating,
            Tags = item.Tags,
            CoverMediaId = item.CoverMediaId,
            CreatedAt = item.CreatedAt,
            StarRating = item.StarRating,
            Amenities = item.Amenities,
            RoomTypes = item.RoomTypes
        });
    }
    #endregion

    #region Dining
    [HttpGet("dining")]
    public async Task<ActionResult<List<DiningDto>>> GetDinings([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Dinings.AsQueryable();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DiningDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                Tags = x.Tags,
                CoverMediaId = x.CoverMediaId,
                CreatedAt = x.CreatedAt,
                CuisineType = x.CuisineType,
                PriceRange = x.PriceRange,
                SignatureDishes = x.SignatureDishes
            }).ToListAsync();

        items = items.Where(x => IsRuralResourceEntry(x.Name, x.Description, x.Address, x.Tags)).ToList();
        return Ok(items);
    }

    [HttpGet("dining/{id}")]
    public async Task<ActionResult<DiningDto>> GetDining(string id)
    {
        var x = await _db.Dinings.FindAsync(id);
        if (x == null) return NotFound();
        return Ok(new DiningDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
            Tags = x.Tags,
            CoverMediaId = x.CoverMediaId,
            CreatedAt = x.CreatedAt,
            CuisineType = x.CuisineType,
            PriceRange = x.PriceRange,
            SignatureDishes = x.SignatureDishes
        });
    }

    [HttpPost("dining")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DiningDto>> CreateDining([FromBody] DiningCreateDto dto)
    {
        try
        {
            if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
            {
                return BadRequest("餐饮资源必须为乡土餐饮、农家菜、村宴等乡村餐饮。");
            }

            var item = new Dining
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Tags = dto.Tags,
                CoverMediaId = dto.CoverMediaId,
                CuisineType = dto.CuisineType,
                PriceRange = dto.PriceRange,
                SignatureDishes = dto.SignatureDishes
            };
            _db.Dinings.Add(item);
            await _db.SaveChangesAsync();

            var resultDto = new DiningDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AverageRating = item.AverageRating,
                Tags = item.Tags,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                CuisineType = item.CuisineType,
                PriceRange = item.PriceRange,
                SignatureDishes = item.SignatureDishes
            };

            return CreatedAtAction(nameof(GetDining), new { id = item.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("dining/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDining(string id)
    {
        var item = await _db.Dinings.FindAsync(id);
        if (item == null) return NotFound();
        _db.Dinings.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("dining/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DiningDto>> UpdateDining(string id, [FromBody] DiningCreateDto dto)
    {
        var item = await _db.Dinings.FindAsync(id);
        if (item == null) return NotFound();

        if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
        {
            return BadRequest("餐饮资源必须为乡土餐饮、农家菜、村宴等乡村餐饮。");
        }

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.Latitude = dto.Latitude;
        item.Longitude = dto.Longitude;
        item.Tags = dto.Tags;
        item.CoverMediaId = dto.CoverMediaId;
        item.CuisineType = dto.CuisineType;
        item.PriceRange = dto.PriceRange;
        item.SignatureDishes = dto.SignatureDishes;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new DiningDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            AverageRating = item.AverageRating,
            Tags = item.Tags,
            CoverMediaId = item.CoverMediaId,
            CreatedAt = item.CreatedAt,
            CuisineType = item.CuisineType,
            PriceRange = dto.PriceRange,
            SignatureDishes = item.SignatureDishes
        });
    }
    #endregion

    #region FolkActivities
    [HttpGet("folkactivities")]
    public async Task<ActionResult<List<FolkActivityDto>>> GetFolkActivities([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.FolkActivities.AsQueryable();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new FolkActivityDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                Tags = x.Tags,
                CoverMediaId = x.CoverMediaId,
                CreatedAt = x.CreatedAt,
                EventDate = x.EventDate,
                Duration = x.Duration,
                Organizer = x.Organizer
            }).ToListAsync();

        items = items.Where(x => IsRuralResourceEntry(x.Name, x.Description, x.Address, x.Tags)).ToList();
        return Ok(items);
    }

    [HttpGet("folkactivities/{id}")]
    public async Task<ActionResult<FolkActivityDto>> GetFolkActivity(string id)
    {
        var x = await _db.FolkActivities.FindAsync(id);
        if (x == null) return NotFound();
        return Ok(new FolkActivityDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
            Tags = x.Tags,
            CoverMediaId = x.CoverMediaId,
            CreatedAt = x.CreatedAt,
            EventDate = x.EventDate,
            Duration = x.Duration,
            Organizer = x.Organizer
        });
    }

    [HttpPost("folkactivities")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FolkActivityDto>> CreateFolkActivity([FromBody] FolkActivityCreateDto dto)
    {
        try
        {
            if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
            {
                return BadRequest("民俗活动必须体现乡村文化、非遗体验、节庆活动等内容。");
            }

            var item = new FolkActivity
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Tags = dto.Tags,
                CoverMediaId = dto.CoverMediaId,
                EventDate = dto.EventDate,
                Duration = dto.Duration,
                Organizer = dto.Organizer
            };
            _db.FolkActivities.Add(item);
            await _db.SaveChangesAsync();

            var resultDto = new FolkActivityDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AverageRating = item.AverageRating,
                Tags = item.Tags,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                EventDate = item.EventDate,
                Duration = item.Duration,
                Organizer = item.Organizer
            };

            return CreatedAtAction(nameof(GetFolkActivity), new { id = item.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("folkactivities/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFolkActivity(string id)
    {
        var item = await _db.FolkActivities.FindAsync(id);
        if (item == null) return NotFound();
        _db.FolkActivities.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("folkactivities/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FolkActivityDto>> UpdateFolkActivity(string id, [FromBody] FolkActivityCreateDto dto)
    {
        var item = await _db.FolkActivities.FindAsync(id);
        if (item == null) return NotFound();

        if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
        {
            return BadRequest("民俗活动必须体现乡村文化、非遗体验、节庆活动等内容。");
        }

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.Latitude = dto.Latitude;
        item.Longitude = dto.Longitude;
        item.Tags = dto.Tags;
        item.CoverMediaId = dto.CoverMediaId;
        item.EventDate = dto.EventDate;
        item.Duration = dto.Duration;
        item.Organizer = dto.Organizer;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new FolkActivityDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            AverageRating = item.AverageRating,
            Tags = item.Tags,
            CoverMediaId = item.CoverMediaId,
            CreatedAt = item.CreatedAt,
            EventDate = item.EventDate,
            Duration = item.Duration,
            Organizer = item.Organizer
        });
    }
    #endregion

    #region BeautifulVillages
    [HttpGet("beautifulvillages")]
    public async Task<ActionResult<List<BeautifulVillageDto>>> GetBeautifulVillages([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.BeautifulVillages.AsQueryable();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new BeautifulVillageDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                Tags = x.Tags,
                CoverMediaId = x.CoverMediaId,
                CreatedAt = x.CreatedAt,
                VillageType = x.VillageType,
                FamousFor = x.FamousFor
            }).ToListAsync();

        items = items.Where(x => IsRuralResourceEntry(x.Name, x.Description, x.Address, x.Tags)).ToList();
        return Ok(items);
    }

    [HttpGet("beautifulvillages/{id}")]
    public async Task<ActionResult<BeautifulVillageDto>> GetBeautifulVillage(string id)
    {
        var x = await _db.BeautifulVillages.FindAsync(id);
        if (x == null) return NotFound();
        return Ok(new BeautifulVillageDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Address = x.Address,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
            Tags = x.Tags,
            CoverMediaId = x.CoverMediaId,
            CreatedAt = x.CreatedAt,
            VillageType = x.VillageType,
            FamousFor = x.FamousFor
        });
    }

    [HttpPost("beautifulvillages")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BeautifulVillageDto>> CreateBeautifulVillage([FromBody] BeautifulVillageCreateDto dto)
    {
        try
        {
            if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
            {
                return BadRequest("美丽乡村资源必须体现乡村、田园、古村、生态村等属性。");
            }

            var item = new BeautifulVillage
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Tags = dto.Tags,
                CoverMediaId = dto.CoverMediaId,
                VillageType = dto.VillageType,
                FamousFor = dto.FamousFor
            };
            _db.BeautifulVillages.Add(item);
            await _db.SaveChangesAsync();

            var resultDto = new BeautifulVillageDto
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AverageRating = item.AverageRating,
                Tags = item.Tags,
                CoverMediaId = item.CoverMediaId,
                CreatedAt = item.CreatedAt,
                VillageType = item.VillageType,
                FamousFor = item.FamousFor
            };

            return CreatedAtAction(nameof(GetBeautifulVillage), new { id = item.Id }, resultDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpDelete("beautifulvillages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBeautifulVillage(string id)
    {
        var item = await _db.BeautifulVillages.FindAsync(id);
        if (item == null) return NotFound();
        _db.BeautifulVillages.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("beautifulvillages/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BeautifulVillageDto>> UpdateBeautifulVillage(string id, [FromBody] BeautifulVillageCreateDto dto)
    {
        var item = await _db.BeautifulVillages.FindAsync(id);
        if (item == null) return NotFound();

        if (!IsRuralResourceEntry(dto.Name, dto.Description, dto.Address, dto.Tags))
        {
            return BadRequest("美丽乡村资源必须体现乡村、田园、古村、生态村等属性。");
        }

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Address = dto.Address;
        item.Latitude = dto.Latitude;
        item.Longitude = dto.Longitude;
        item.Tags = dto.Tags;
        item.CoverMediaId = dto.CoverMediaId;
        item.VillageType = dto.VillageType;
        item.FamousFor = dto.FamousFor;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new BeautifulVillageDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Address = item.Address,
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            AverageRating = item.AverageRating,
            Tags = item.Tags,
            CoverMediaId = item.CoverMediaId,
            CreatedAt = item.CreatedAt,
            VillageType = item.VillageType,
            FamousFor = item.FamousFor
        });
    }

    private static bool IsRuralResourceEntry(string? name, string? description, string? address, string? tags)
    {
        var text = string.Join(' ', new[] { name, description, address, tags }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(text)) return false;

        if (ContainsVillageTag(tags)) return true;

        string[] ruralKeywords =
        [
            "乡村", "村", "田园", "农", "农家", "采摘", "果园", "茶园", "稻田", "民宿", "古村", "非遗", "民俗", "研学", "生态", "庄园", "农庄"
        ];

        return ruralKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsVillageTag(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return false;

        return tags
            .Split([',', '，', ';', '；', '|', '/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => x.StartsWith("village:", StringComparison.OrdinalIgnoreCase));
    }
    #endregion
}
