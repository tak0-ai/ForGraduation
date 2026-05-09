namespace RuralTourism.Api.Entities
{
    public class BeautifulVillage : Resource
    {
        public string? VillageType { get; set; } // e.g., "历史古村", "生态村", etc.
        public string? FamousFor { get; set; } // 著名景点或特色
    }
}