using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Sale
{
    public class CreateSaleVM
    {
        [Required]
        [MaxLength(12)]
        public string Upc { get; set; }

        [Required]
        public int Amount { get; set; }
    }
}
