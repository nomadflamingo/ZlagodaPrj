namespace ZlagodaPrj.Models.DTO
{
    public class StoreProductsListPagedResult
    {
        public List<StoreProductDTO> StoreProducts { get; set; }

        public bool ShowOnlyOnSale { get; set; }
        public bool ShowOnlyNonSale { get; set; }
        public string UpcSearchString { get; set; }

    }
}
