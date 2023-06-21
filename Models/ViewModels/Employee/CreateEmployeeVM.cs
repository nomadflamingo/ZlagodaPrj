using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.ViewModels.Employee
{
    public class CreateEmployeeVM
    {
        [Required]
        [MaxLength(10)]
        public string Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Surname { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string? Patronymic { get; set; }

        [Required]
        [MaxLength(10)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public decimal Salary { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.PhoneNumber)]
        [MaxLength(13)]
        public string Phone { get; set; }

        [Required]
        [MaxLength(50)]
        public string City { get; set; }

        [Required]
        [MaxLength(50)]
        public string Street { get; set; }

        [Required]
        [MaxLength(9)]
        [DataType(DataType.PostalCode)]
        public string ZipCode { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string PasswordConfirm { get; set; }
    }
}
