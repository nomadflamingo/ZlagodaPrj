using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Product
{
    public class CreateUpdateProductVM
    {
        [HiddenInput]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; }

        [Required]
        [MaxLength(100)]
        public string Characteristics { get; set; }
    }
}
