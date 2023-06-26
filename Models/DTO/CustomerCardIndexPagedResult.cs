using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.DTO
{
    public class CustomerCardIndexPagedResult
    {
        public IEnumerable<CustomerCard> CustomerCards { get; set; }
        public string SurnameSearchString { get; set; }
        public int MinPercent { get; set; }
        public int MaxPercent { get; set; }
    }
}
