using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Category
{
    public class CreateCategoryVM
    {
        [MaxLength(50)]
        [Required]
        public string Name { get; set; }
    }
}
