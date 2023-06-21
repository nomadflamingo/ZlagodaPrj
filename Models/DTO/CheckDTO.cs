using ZlagodaPrj.Models.ViewModels.Sale;

namespace ZlagodaPrj.Models.DTO
{
    public class CheckDTO
    {
        public string Number { get; set; }
        public string CashierId { get; set; }
        public string? CardNumber { get; set; }
        public DateTime PrintDate { get; set; }
        public decimal SumTotal { get; set; }
        public decimal Vat { get; set; }
        public List<SaleInCheckDTO> Sales { get; set; } = new() { /*new CreateSaleVM() { Upc = "fff", Amount = 5, Price = 6 }*/ };
    }
}
