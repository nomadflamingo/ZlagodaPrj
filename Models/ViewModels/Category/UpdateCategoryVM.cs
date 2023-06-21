using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Category
{
    public class UpdateCategoryVM
    {
        [HiddenInput]
        [Required]
        public int Number { get; set; }
        
        [MaxLength(50)]
        [Required]
        public string? Name { get; set; }
    }
}
