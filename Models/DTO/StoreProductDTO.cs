namespace ZlagodaPrj.Models.DTO
{
    public class StoreProductDTO
    {
        public string Upc { get; set; }
        public string? UpcProm { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Amount { get; set; }
        public bool IsProm { get; set; }
        public string Characteristics { get; set; }
    }
}
