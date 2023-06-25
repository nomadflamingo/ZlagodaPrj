namespace ZlagodaPrj.Models.DTO
{
    public class StoreProductsListPagedResult
    {
        public List<StoreProductDTO> StoreProducts { get; set; }

        public string SortBy { get; set; }

    }
}
