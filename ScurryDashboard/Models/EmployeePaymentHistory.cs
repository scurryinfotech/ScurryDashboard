namespace ScurryDashboard.Models
{
    public class EmployeePaymentHistory
    {
        public IEnumerable<PayrollWithBalance> Payrolls { get; set; }
            = new List<PayrollWithBalance>();
        public IEnumerable<SalaryPayment> Payments { get; set; }
            = new List<SalaryPayment>();
    }
}
