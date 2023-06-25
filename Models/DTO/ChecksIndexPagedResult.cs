namespace ZlagodaPrj.Models.DTO
{
    public class ChecksIndexPagedResult
    {
        public IEnumerable<CheckDTO> Checks { get; set; }
        public string CurrentEmployeeIdSearchString { get; set; }
    }
}
