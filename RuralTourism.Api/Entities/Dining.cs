namespace RuralTourism.Api.Entities
{
    public class Dining : Resource
    {
        public string? CuisineType { get; set; } // "淮扬菜", "农家菜"
        public string? PriceRange { get; set; } // "人均 ¥50-100"
        public string? SignatureDishes { get; set; } // "镇江肴肉,蟹黄汤包"
    }
}
