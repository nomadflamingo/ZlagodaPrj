using System.ComponentModel.DataAnnotations;
using ZlagodaPrj.Models.ViewModels.Sale;

namespace ZlagodaPrj.Models.ViewModels.Check
{
    public class CreateUpdateCheckVM
    {
        [Required]
        [MaxLength(10)]
        public string Number { get; set; }

        [Required]
        [MaxLength(10)]
        public string CashierId { get; set; }

        [MaxLength(10)]
        public string? CardNumber { get; set; }

        // public List<CreateSaleVM> Sales { get; set; } = new(){ new CreateSaleVM() { Upc = "fff", Amount = 5, Price = 6 } };
    }
}
