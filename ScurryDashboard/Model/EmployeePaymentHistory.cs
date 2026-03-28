namespace OrderService.Model
{
    public class EmployeePaymentHistory
    {
        public int PayrollId { get; set; }
        public int PayMonth { get; set; }
        public int PayYear { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal NetSalary { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal Deductions { get; set; }
        public string PayrollStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public IEnumerable<PayrollWithBalance> Payrolls { get; set; }
            = new List<PayrollWithBalance>();
        public IEnumerable<SalaryPayment> Payments { get; set; }
            = new List<SalaryPayment>();
    }
}
