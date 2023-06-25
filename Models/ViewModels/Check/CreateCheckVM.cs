using System.ComponentModel.DataAnnotations;
using ZlagodaPrj.Models.ViewModels.Sale;

namespace ZlagodaPrj.Models.ViewModels.Check
{
    public class CreateCheckVM
    {
        [Required]
        [MaxLength(10)]
        public string Number { get; set; }

        [MaxLength(10)]
        public string? CardNumber { get; set; }

        public List<CreateSaleVM> Sales { get; set; } = new();
    }
}
