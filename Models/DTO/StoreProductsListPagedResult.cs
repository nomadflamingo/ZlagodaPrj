using System.ComponentModel.DataAnnotations;

namespace ZlagodaPrj.Models.DTO
{
    public class StoreProductsListPagedResult
    {
        public List<StoreProductDTO> StoreProducts { get; set; }

        public bool ShowOnlyOnSale { get; set; }
        public bool ShowOnlyNonSale { get; set; }
        public string UpcSearchString { get; set; }
        public bool IncludeTotalSold { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

    }
}
