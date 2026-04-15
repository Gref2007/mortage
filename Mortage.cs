using System.Globalization;

namespace mortage;

public record SweepPoint(int DepositMonths, double TotalInterest, double DepositGrown);
public record SweepResult(IReadOnlyList<SweepPoint> Points, SweepPoint Best);

public static class Mortage
{
    public static readonly NumberFormatInfo Nfi = new()
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2
    };

    public static double GetMonthPayment(double loanAmount, double interest, int monthsLeft)
    {
        var monthlyRate = interest / 100 / 12;
        var pow = Math.Pow(1 + monthlyRate, monthsLeft);
        var payment = loanAmount * (monthlyRate * pow) / (pow - 1);
        return Math.Round(payment, 2);
    }

    public static double CalculatePercentPayMonth(double loanAmount, double interest, DateTime payDate)
    {
        var beforeMonth = payDate.AddMonths(-1);
        var daysBetween = (payDate - beforeMonth).Days;
        var daysInYear = DateTime.IsLeapYear(beforeMonth.Year) ? 366 : 365;
        var percent = loanAmount * (interest * daysBetween) / (100 * daysInYear);
        return Math.Round(percent, 2);
    }

    public static double CalculateAllPays(MortageOptions options)
    {
        var loanAmount = (double)options.MortgageSize;
        var monthPayment = GetMonthPayment(loanAmount, options.MortgageInterest, options.MonthsLeft);
        var firstPay = options.MortgageDate;
        double percentPayed = 0;
        while (loanAmount > 0)
        {
            var percentPay = CalculatePercentPayMonth(loanAmount, options.MortgageInterest, firstPay);
            percentPayed += percentPay;
            var mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
            loanAmount = Math.Round(loanAmount - mainPay, 2);
            firstPay = firstPay.AddMonths(1);
        }
        return percentPayed;
    }

    public static double CalculateWithOneExtraPay(MortageOptions options)
    {
        var loanAmount = (double)options.MortgageSize;
        var monthPayment = GetMonthPayment(loanAmount, options.MortgageInterest, options.MonthsLeft);
        var firstPay = options.MortgageDate;
        double percentPayed = 0;
        while (loanAmount > 0)
        {
            var percentPay = CalculatePercentPayMonth(loanAmount, options.MortgageInterest, firstPay);
            percentPayed += percentPay;
            var mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
            loanAmount = Math.Round(loanAmount - mainPay, 2);
            if (options.ExtraPay.DateOfMoney.Year == firstPay.Year &&
                options.ExtraPay.DateOfMoney.Month == firstPay.Month)
            {
                loanAmount -= options.ExtraPay.CountOfMoney;
            }
            firstPay = firstPay.AddMonths(1);
        }
        return percentPayed;
    }

    public static SweepResult CalculateWithAllExtraPayVariations(MortageOptions options)
    {
        var points = new List<SweepPoint>(options.ExtraPay.DepositMaxMonthKeep);
        var monthPayment = GetMonthPayment(options.MortgageSize, options.MortgageInterest, options.MonthsLeft);

        for (var i = 1; i < options.ExtraPay.DepositMaxMonthKeep; i++)
        {
            var depositMonthLeft = i;
            var depositCountOfMoney = options.ExtraPay.CountOfMoney;
            var firstPay = options.MortgageDate;
            var loanAmount = (double)options.MortgageSize;

            double percentPayed = 0;
            while (loanAmount > 0)
            {
                var percentPay = CalculatePercentPayMonth(loanAmount, options.MortgageInterest, firstPay);
                percentPayed += percentPay;
                var mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
                loanAmount = Math.Round(loanAmount - mainPay, 2);

                if (firstPay > options.ExtraPay.DateOfMoney && depositMonthLeft > 0)
                {
                    depositCountOfMoney += depositCountOfMoney * options.ExtraPay.DepositInterest / 100 / 12;
                    depositMonthLeft--;
                }

                if (depositMonthLeft == 0)
                {
                    loanAmount -= depositCountOfMoney;
                    depositMonthLeft -= 100000;
                }

                firstPay = firstPay.AddMonths(1);
            }

            points.Add(new SweepPoint(i, percentPayed, depositCountOfMoney));
        }

        var best = points.MinBy(p => p.TotalInterest)!;
        return new SweepResult(points, best);
    }
}
