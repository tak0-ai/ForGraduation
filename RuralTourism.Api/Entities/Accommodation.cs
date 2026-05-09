namespace RuralTourism.Api.Entities
{
    public class Accommodation:Resource
    {
        public int StarRating { get; set; } //  5 (五星级)
        public string? Amenities { get; set; } // 设施，用逗号分隔，如 "Wi-Fi,停车场,早餐"
        public string? RoomTypes { get; set; } // 房型信息，可以是 JSON 字符串
    }
}
