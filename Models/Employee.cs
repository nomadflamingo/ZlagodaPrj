namespace ZlagodaPrj.Models
{
    public class Employee
    {
        public const string TABLE_NAME = "\"Employee\"";

        public const string COL_ID = "id_employee";
        public const string COL_SURNAME = "empl_surname";
        public const string COL_NAME = "empl_name";
        public const string COL_PATRONYMIC = "empl_patronymic";
        public const string COL_ROLE = "empl_role";
        public const string COL_SALARY = "salary";
        public const string COL_BIRTHDATE = "date_of_birth";
        public const string COL_STARTDATE = "date_of_start";
        public const string COL_PHONE = "phone_number";
        public const string COL_CITY = "city";
        public const string COL_STREET = "street";
        public const string COL_ZIP_CODE = "zip_code";
        public const string COL_PASSWORD_HASH = "password_hash";

        public string Id { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string? Patronymic { get; set; }
        public string Role { get; set; }
        public decimal Salary { get; set; }
        public DateOnly BirthDate { get; set; }
        public DateOnly StartDate { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string PasswordHash { get; set; }
    }
}
