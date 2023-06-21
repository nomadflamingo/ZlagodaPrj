using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.StoreProduct
{
    public class CreateUpdateStoreProductVM
    {
        [HiddenInput]
        public string? OldUpc { get; set; }

        [Required]
        [MaxLength(12)]
        public string Upc { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProductName { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Amount { get; set; }

        [HiddenInput]
        public bool? OldIsProm { get; set; }

        [Required]
        public bool IsProm { get; set; }

    }
}
