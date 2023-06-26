using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.DTO
{
    public class ChecksIndexPagedResult
    {
        public IEnumerable<CheckDTO> Checks { get; set; }
        public string CurrentEmployeeIdSearchString { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }
    }
}
