using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTService
{
    public interface IDateRangeService
    {
        (DateTime ShiftBegin, DateTime ShiftEnd) GetCurrentShiftDateRange();

        (DateTime DayBegin, DateTime DayEnd) GetCurrentDayDateRange();

        (DateTime MonthBegin, DateTime MonthEnd) GetCurrentMonthDateRange();
    }

    public class DefaultShiftService : IDateRangeService
    {
        public (DateTime DayBegin, DateTime DayEnd) GetCurrentDayDateRange()
        {
            var today = DateTime.Today;
            return (today, today.AddDays(1));
        }

        public (DateTime MonthBegin, DateTime MonthEnd) GetCurrentMonthDateRange()
        {
            var now = DateTime.Now;
            DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            return (firstDayOfMonth, firstDayOfMonth.AddMonths(1));
        }

        public (DateTime ShiftBegin, DateTime ShiftEnd) GetCurrentShiftDateRange()
        {
            var now = DateTime.Now;
            return now.Hour >= 8 && now.Hour < 20 ? (now.Date.AddHours(8), now.Date.AddHours(20)) : now.Hour < 8 ? (now.Date.AddHours(-4), now.Date.AddHours(8)) : (now.Date.AddHours(20), now.Date.AddHours(32));
        }
    }
}