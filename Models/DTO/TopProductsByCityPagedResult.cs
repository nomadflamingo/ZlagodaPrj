namespace ZlagodaPrj.Models.DTO
{
    public class TopProductsByCityPagedResult
    {

        public string SearchCityString { get; set; }

        public List<ProductInfo> ProductsInfo { get; set; }

        public class ProductInfo
        {
            public string ProductName { get; set; }

            public long SalesCount { get; set; }
        }
    }
}
