namespace ZlagodaPrj.Models.DTO
{
    public class EmployeeIndexPagedResult
    {
        public IEnumerable<EmployeeDTO> Employees { get; set; }
        public string SurnameSearchString { get; set; }
    }
}
