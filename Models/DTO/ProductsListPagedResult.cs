namespace ZlagodaPrj.Models.DTO
{
    public class ProductsListPagedResult
    {
        public IEnumerable<ProductDTO> Products { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public string ProductNameSearchString { get; set; }
        public string CategoryName { get; set; }
    }
}
