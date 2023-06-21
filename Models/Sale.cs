namespace ZlagodaPrj.Models
{
    public class Sale
    {
        public const string TABLE_NAME = "\"Sale\"";

        public const string COL_UPC = "upc";
        public const string COL_CHECK_NUMBER = "check_number";
        public const string COL_AMOUNT = "product_number";
        public const string COL_SELLING_PRICE = "selling_price";

        public string Upc { get; set; }
        public string CheckNumber { get; set; }
        public int Amount { get; set; }
        public decimal Price { get; set; }
    }
}
