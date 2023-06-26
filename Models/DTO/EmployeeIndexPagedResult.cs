using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.DTO
{
    public class EmployeeIndexPagedResult
    {
        public IEnumerable<EmployeeDTO> Employees { get; set; }
        public string SurnameSearchString { get; set; }
        public bool IncludeTotalSold { get; set; }
        public long TotalSold { get; set; }
        public string IdSearchString { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }
    }
}
