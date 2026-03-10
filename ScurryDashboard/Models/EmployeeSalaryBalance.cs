namespace ScurryDashboard.Models
{
    public class EmployeeSalaryBalance
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = "";
        public string? Department { get; set; }
        public decimal BasicSalary { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalOutstanding { get; set; }
        public IEnumerable<PayrollBalance> PayrollBreakdown { get; set; }
            = new List<PayrollBalance>();
    }
}
