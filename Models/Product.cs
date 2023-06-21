namespace ZlagodaPrj.Models
{
    public class Product
    {
        public const string TABLE_NAME = "\"Product\"";

        public const string COL_ID = "id_product";
        public const string COL_NAME = "product_name";
        public const string COL_CHARACTERISTICS = "characteristics";
        public const string COL_CATEGORY_NUMBER = "category_number";

        public int Id { get; set; }
        public string Name { get; set; }
        public string Characteristics { get; set; }
        public int CategoryNumber { get; set; }
    }
}
