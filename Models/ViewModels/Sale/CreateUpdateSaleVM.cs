using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Sale
{
    public class CreateUpdateSaleVM
    {
        [Required]
        [MaxLength(12)]
        public string Upc { get; set; }

        [Required]
        public int Amount { get; set; }
    }
}
