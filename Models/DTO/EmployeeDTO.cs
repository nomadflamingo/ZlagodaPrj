namespace ZlagodaPrj.Models.DTO
{
	public class EmployeeDTO
	{
		public string Id { get; set; }
		public string Surname { get; set; }
		public string Name { get; set; }
		public string? Patronymic { get; set; }
		public string Role { get; set; }
		public decimal Salary { get; set; }
		public DateOnly BirthDate { get; set; }
		public DateOnly StartDate { get; set; }
		public string Phone { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public string ZipCode { get; set; }
	}
}
