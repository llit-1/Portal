using System;
using System.Collections.Generic;
using System.Globalization;
namespace Portal.ViewModels.Salary
{
    public class PersonMonthSalaryViewModel
    {
        public Guid PersonGuid { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; }
        public string PersonName { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<PersonMonthSalarySummaryCardViewModel> SummaryCards { get; set; } = new List<PersonMonthSalarySummaryCardViewModel>();
        public List<PersonMonthSalaryPeriodBonusViewModel> PeriodBonusItems { get; set; } = new List<PersonMonthSalaryPeriodBonusViewModel>();
        public List<PersonMonthSalaryJobCardViewModel> JobCards { get; set; } = new List<PersonMonthSalaryJobCardViewModel>();
        public string PeriodLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(MonthName) && Year > 0)
                {
                    return $"{CultureInfo.GetCultureInfo("ru-RU").TextInfo.ToTitleCase(MonthName.Trim())} {Year}";
                }
                return Month > 0 && Year > 0
                    ? CultureInfo
                        .GetCultureInfo("ru-RU")
                        .TextInfo
                        .ToTitleCase(new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")))
                    : "Выбранный период";
            }
        }
    }
    public class PersonMonthSalarySummaryCardViewModel
    {
        public string Title { get; set; }
        public decimal? Amount { get; set; }
        public string Description { get; set; }
    }
    public class PersonMonthSalaryPeriodBonusViewModel
    {
        public string Name { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Begin { get; set; }
        public DateTime? End { get; set; }
        public string Label
        {
            get
            {
                var title = string.IsNullOrWhiteSpace(Name) ? "Периодический бонус" : Name;
                if (!Quantity.HasValue || Quantity.Value <= 0)
                {
                    return title;
                }
                var roundedQuantity = Math.Round(Quantity.Value, 2);
                var quantityText = roundedQuantity % 1 == 0
                    ? ((int)roundedQuantity).ToString()
                    : roundedQuantity.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
                return $"{title} ({quantityText})";
            }
        }
    }
    public class PersonMonthSalaryJobCardViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public decimal? TotalAmount { get; set; }
        public string BackgroundStyle { get; set; }
        public List<PersonMonthSalaryJobIncomeItemViewModel> Items { get; set; } = new List<PersonMonthSalaryJobIncomeItemViewModel>();
    }
    public class PersonMonthSalaryJobIncomeItemViewModel
    {
        public string Name { get; set; }
        public double Hours { get; set; }
        public decimal? Amount { get; set; }
        public string Label
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return "-";
                }
                var roundedHours = Math.Round(Hours, 2);
                var hoursText = roundedHours % 1 == 0
                    ? ((int)roundedHours).ToString()
                    : roundedHours.ToString("0.##", CultureInfo.GetCultureInfo("ru-RU"));
                return $"{Name} ({hoursText} ч.)";
            }
        }
    }
}
