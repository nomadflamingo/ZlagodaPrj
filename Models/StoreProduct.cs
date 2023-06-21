namespace ZlagodaPrj.Models
{
    public class StoreProduct
    {
        public const string TABLE_NAME = "\"StoreProduct\"";

        public const string COL_UPC = "upc";
        public const string COL_UPC_PROM = "upc_prom";
        public const string COL_PRODUCT_ID = "id_product";
        public const string COL_PRICE = "selling_price";
        public const string COL_AMOUNT = "products_number";
        public const string COL_IS_PROM = "promotional_product";

        public string Upc { get; set; }
        public string? UpcProm { get; set; }
        public int ProductId { get; set; }
        public decimal Price { get; set; }
        public int Amount { get; set; }
        public bool IsProm { get; set; }
    }
}
