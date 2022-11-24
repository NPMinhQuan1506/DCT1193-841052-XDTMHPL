using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pay1193.Entity;
using Pay1193.Models;
using Pay1193.Services;
using Pay1193.Services.Implement;
using System.Reflection.Metadata.Ecma335;

namespace Pay1193.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPayService _payService;
        private readonly IEmployee _employeeService;
        private readonly ITaxService _taxService;
        private readonly INationalInsuranceService _nationalInsuranceService;
        private decimal overtionHrs;
        private decimal contractualEarnings;
        private decimal overtimeEarnings;
        private decimal totalEarnings;
        private decimal tax;
        private decimal unionFee;
        private decimal studentLoan;
        private decimal nationInsurance;
        private decimal totalDeduction;

        public PaymentController(IPayService payService, IEmployee employeeService, ITaxService taxService, INationalInsuranceService nationalInsuranceService)
        {
            _payService = payService;
            _employeeService = employeeService;
            _taxService = taxService;
            _nationalInsuranceService = nationalInsuranceService;
        }
        public IActionResult Index()
        {
            var payRecords = _payService.GetAll()
                .Select(pay => new PaymentRecordIndexViewModel
                 {
                     Id = pay.Id,
                     EmployeeId = pay.EmployeeId,
                     FullName = _employeeService.GetById(pay.EmployeeId).FullName,
                    PayDate = pay.DatePay,
                     PayMonth = pay.MonthPay,
                     TaxYearId = pay.TaxYearId,
                     Year = _payService.GetTaxYearById(pay.TaxYearId).YearOfTax,
                     TotalEarnings = pay.TotalEarnings,
                     TotalDeduction = pay.EarningDeduction,
                     NetPayment = pay.NetPayment,
                     Employee = pay.Employee
                 });
            return View(payRecords);
        }
        [HttpGet]
        public IActionResult Create()
        {
            //ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.employees = _employeeService.GetAll();
            ViewBag.taxYears = _payService.GetAllTaxYear();
            var model = new PaymentRecordCreateViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentRecordCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var payrecord = new PaymentRecord()
                {
                    Id = model.Id,
                    EmployeeId = model.EmployeeId,
                    DatePay = model.PayDate,
                    MonthPay = model.PayMonth,
                    TaxYearId = model.TaxYearId,
                    TaxCode = model.TaxCode,
                    HourlyRate = model.HourlyRate,
                    HourWorked = model.HourlyWorked,
                    ContractualHours = model.ContractualHours,
                    OvertimeHours = overtionHrs = _payService.OvertimeHours(model.HourlyWorked, model.ContractualHours),
                    ContractualEarnings = contractualEarnings = _payService.ContractualEarning(model.ContractualEarnings, model.HourlyWorked, model.HourlyRate),
                    OvertimeEarnings = overtimeEarnings = _payService.OvertimeEarnings(_payService.OvertimeRate(model.HourlyRate), overtionHrs),
                    TotalEarnings = totalEarnings = _payService.TotalEarnings(overtimeEarnings, contractualEarnings),
                    Tax = tax = _taxService.TaxAmount(totalEarnings),
                    UnionFee = unionFee = _employeeService.UnionFee(model.EmployeeId),
                    SLC = unionFee = _employeeService.UnionFee(model.EmployeeId),
                    NiC = nationInsurance = _employeeService.UnionFee(model.EmployeeId),
                    EarningDeduction = totalDeduction = _payService.TotalDeduction(tax, nationInsurance, studentLoan, unionFee),
                    NetPayment = _payService.NetPay(totalEarnings, totalDeduction)
                };
                await _payService.CreateAsync(payrecord);
                return RedirectToAction("Index");
            }
            ViewBag.employees = _employeeService.GetAll();
            ViewBag.taxYears = _payService.GetAllTaxYear();
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Detail(int id)
        {
            var paymentRecord = _payService.GetById(id);
            if (paymentRecord == null) return NotFound();

            var model = new PaymentRecordDetailViewModel()
            {
                Id = paymentRecord.Id,
                EmployeeId = paymentRecord.EmployeeId,
                FullName = _employeeService.GetById(paymentRecord.EmployeeId).FullName,
                NiNo = _employeeService.GetById(paymentRecord.EmployeeId).NationalInsuranceNo,
                PayDate = paymentRecord.DatePay,
                PayMonth = paymentRecord.MonthPay,
                TaxYearId = paymentRecord.TaxYearId,
                Year = _payService.GetTaxYearById(paymentRecord.TaxYearId).YearOfTax,
                TaxCode = paymentRecord.TaxCode,
                HourlyRate = paymentRecord.HourlyRate,
                HourlyWorked = paymentRecord.HourWorked,
                ContractualHours = paymentRecord.ContractualHours,
                OvertimeHours = paymentRecord.OvertimeHours,
                OvertimeRate = _payService.OvertimeRate(paymentRecord.HourlyRate),
                ContractualEarnings = paymentRecord.ContractualEarnings,
                OvertimeEarnings = paymentRecord.OvertimeEarnings,
                Tax = paymentRecord.Tax,
                NIC = paymentRecord.NiC,
                UnionFee = paymentRecord.UnionFee,
                SLC = paymentRecord.SLC,
                TotalEarnings = paymentRecord.TotalEarnings,
                TotalDeduction = paymentRecord.EarningDeduction,
                Employee = paymentRecord.Employee,
                TaxYear = paymentRecord.TaxYear,
                NetPayment = paymentRecord.NetPayment
            };
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Payslip(int id, int isPDF = 0)
        {
            var paymentRecord = _payService.GetById(id);
            if (paymentRecord == null) return NotFound();

            var model = new PaymentRecordDetailViewModel()
            {
                Id = paymentRecord.Id,
                EmployeeId = paymentRecord.EmployeeId,
                FullName = _employeeService.GetById(paymentRecord.EmployeeId).FullName,
                NiNo = _employeeService.GetById(paymentRecord.EmployeeId).NationalInsuranceNo,
                PayDate = paymentRecord.DatePay,
                PayMonth = paymentRecord.MonthPay,
                TaxYearId = paymentRecord.TaxYearId,
                Year = _payService.GetTaxYearById(paymentRecord.TaxYearId).YearOfTax,
                TaxCode = paymentRecord.TaxCode,
                HourlyRate = paymentRecord.HourlyRate,
                HourlyWorked = paymentRecord.HourWorked,
                ContractualHours = paymentRecord.ContractualHours,
                OvertimeHours = paymentRecord.OvertimeHours,
                OvertimeRate = _payService.OvertimeRate(paymentRecord.HourlyRate),
                ContractualEarnings = paymentRecord.ContractualEarnings,
                OvertimeEarnings = paymentRecord.OvertimeEarnings,
                Tax = paymentRecord.Tax,
                NIC = paymentRecord.NiC,
                UnionFee = paymentRecord.UnionFee,
                SLC = paymentRecord.SLC,
                TotalEarnings = paymentRecord.TotalEarnings,
                TotalDeduction = paymentRecord.EarningDeduction,
                Employee = _employeeService.GetById(paymentRecord.EmployeeId),
                TaxYear = paymentRecord.TaxYear,
                NetPayment = paymentRecord.NetPayment
            };
            ViewBag.IsPDF = isPDF;
            return View(model);
        }


    }
}
