namespace ZlagodaPrj.Models
{
    public class CustomerCard
    {
        public const string TABLE_NAME = "\"CustomerCard\"";

        public const string COL_NUMBER = "card_number";
        public const string COL_SURNAME = "cust_surname";
        public const string COL_NAME = "cust_name";
        public const string COL_PATRONYMIC = "cust_patronymic";
        public const string COL_PHONE = "phone_number";
        public const string COL_CITY = "city";
        public const string COL_STREET = "street";
        public const string COL_ZIP_CODE = "zip_code";
        public const string COL_PERCENT = "percent";
        
        public string Number { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string? Patronymic { get; set; }
        public string Phone { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? ZipCode { get; set; }
        public int Percent { get; set; }
    }
}
