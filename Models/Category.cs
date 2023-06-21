namespace ZlagodaPrj.Models
{
    public class Category
    {
        public const string TABLE_NAME = "\"Category\"";

        public const string COL_NUMBER = "category_number";
        public const string COL_NAME = "category_name";

        public int Number { get; set; }

        public string Name { get; set; } = string.Empty;

    }
}
