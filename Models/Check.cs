namespace ZlagodaPrj.Models
{
    public class Check
    {
        public const string TABLE_NAME = "\"Check\"";

        public const string COL_NUMBER = "check_number";
        public const string COL_CASHIER_ID = "id_employee";
        public const string COL_CARD_NUMBER = "card_number";
        public const string COL_PRINT_DATE = "print_date";
        public const string COL_SUM_TOTAL = "sum_total";
        public const string COL_VAT = "vat";

        public string Number { get; set; }
        public string CashierId { get; set; }
        public string? CardNumber { get; set; }
        public DateTime PrintDate { get; set; }
        public decimal SumTotal { get; set; }
        public decimal Vat { get; set; }
    }
}
