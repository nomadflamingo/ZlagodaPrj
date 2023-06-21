using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.CustomerCard
{
    public class UpdateCustomerCardVM
    {
        [HiddenInput]
        [Required]
        public string OldNumber { get; set; }

        [Required]
        [MaxLength(13)]
        public string Number { get; set; }

        [Required]
        [MaxLength(50)]
        public string Surname { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string? Patronymic { get; set; }

        [Required]
        [MaxLength(13)]
        public string Phone { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Street { get; set; }

        [MaxLength(9)]
        public string? ZipCode { get; set; }

        [Required]
        public int Percent { get; set; }
    }
}
