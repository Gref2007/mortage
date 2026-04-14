using System.Globalization;

namespace mortage;

public class Mortage
{
    public static NumberFormatInfo nfi = new NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 2
    };
    
    
    
    public static double GetMonthPayment(double loanAmount, double interest, int monthsLeft)
    {
        double monthlyRate = interest / 100 / 12; // месячная ставка
        
        double pow = Math.Pow(1 + monthlyRate, monthsLeft);
        double payment = loanAmount * (monthlyRate * pow) / (pow - 1);
    
        return Math.Round(payment, 2);
    }
    
    public static double CalculatePercentPayMonth(double loanAmount, double  interest, DateTime payDate)
    {
        var beforeMonth = payDate.AddMonths(-1);
        int daysBetween = (payDate - beforeMonth).Days;
        //var dayInMonth = DateTime.DaysInMonth(beforeMonth.Year, beforeMonth.Month);
        var daysInYear = DateTime.IsLeapYear(beforeMonth.Year) ? 366 : 365;
        var percent =  loanAmount * (interest * daysBetween) / (100 * daysInYear);
        return Math.Round(percent, 2);
    }

    public static double CalculateAllPays(MortageOptions options)
    {
        return CalculateAllPays(options.MortgageSize, options.MortgageInterest, options.MortgageDate, options.MonthsLeft);
    }
    
    public static double CalculateAllPays(double loanAmount, double  interest, DateTime firstPay, int monthsLeft)
    {
        var monthPayment = GetMonthPayment(loanAmount, interest, monthsLeft);
        double percentPayed = 0;
        double mainPay = 0;
        while (loanAmount>0)
        {
            var percentPay = CalculatePercentPayMonth(loanAmount, interest, firstPay);
            percentPayed += percentPay;
            mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
            loanAmount =  Math.Round(loanAmount-mainPay,2);
            //Console.WriteLine($"{firstPay.ToString("d")}- {monthPayment}, {mainPay}, {percentPay}, {loanAmount.ToString("N",nfi)}");
            
            firstPay = firstPay.AddMonths(1);
        }
        return percentPayed;
    }

    public static double CalculateWithOneExtraPay(MortageOptions options)
    {
        return CalculateWithOneExtraPay(options.MortgageSize, options.MortgageInterest, options.MortgageDate, options.MonthsLeft, options.ExtraPay);
    }
    
    public static double CalculateWithOneExtraPay(double loanAmount, double  interest, DateTime firstPay, int monthsLeft, ExtraPay extraPay)
    {
        var monthPayment = GetMonthPayment(loanAmount, interest, monthsLeft);
        double percentPayed = 0;
        double mainPay = 0;
        while (loanAmount>0)
        {
            var percentPay = CalculatePercentPayMonth(loanAmount, interest, firstPay);
            percentPayed += percentPay;
            mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
            loanAmount =  Math.Round(loanAmount-mainPay,2);
            if (extraPay.DateOfMoney.Year == firstPay.Year && extraPay.DateOfMoney.Month == firstPay.Month)
            {
                loanAmount -= extraPay.CountOfMoney;
            }
            //Console.WriteLine($"{firstPay.ToString("d")}- {monthPayment}, {mainPay}, {percentPay}, {loanAmount.ToString("N",nfi)}");
            
            firstPay = firstPay.AddMonths(1);
        }
        return percentPayed;
    }

    public static void CalculateWithAllExtraPayVariations(MortageOptions options)
    {
        CalculateWithAllExtraPayVariations(options.MortgageSize, options.MortgageInterest, options.MortgageDate, options.MonthsLeft, options.ExtraPay);
    }
    
    public static void CalculateWithAllExtraPayVariations(double startLoanAmount, double  interest, DateTime firstLoandatePay, 
        int monthsLeft, ExtraPay extraPay)
    {
        double minpercentPay = 1000000000;
        int minDepositMonthByPercent = 1000;
        var monthPayment = GetMonthPayment(startLoanAmount, interest, monthsLeft);
        for (int i = 1; i < extraPay.DepositMaxMonthKeep; i++)
        {
            var depositMonthLeft = i;
            var depostiCountOfMonety = extraPay.CountOfMoney;
            var firstPay = firstLoandatePay;
            var loanAmount = startLoanAmount;
            
            double percentPayed = 0;
            double mainPay = 0;
            while (loanAmount>0)
            {
                var percentPay = CalculatePercentPayMonth(loanAmount, interest, firstPay);
                percentPayed += percentPay;
                mainPay = Math.Round(monthPayment - percentPay, 2, MidpointRounding.ToEven);
                loanAmount =  Math.Round(loanAmount-mainPay,2);
                
                if (firstPay>extraPay.DateOfMoney && depositMonthLeft >0)
                {
                    depostiCountOfMonety += depostiCountOfMonety * extraPay.DepositInterest/100 / 12;
                    depositMonthLeft--;
                }

                if (depositMonthLeft==0)
                {
                    loanAmount-= depostiCountOfMonety;
                    depositMonthLeft -= 100000;
                    
                }
            
                firstPay = firstPay.AddMonths(1);
            }
            
            if (minpercentPay > percentPayed)
            {
                minpercentPay = percentPayed;
                minDepositMonthByPercent = i;
            }
            
            Console.WriteLine($"monthCountDeposit:{i}, PercendPayed:{percentPayed.ToString("N",nfi)},  depostiCountOfMonety:{depostiCountOfMonety.ToString("N",nfi)} ");
        }
        Console.WriteLine($"minDepositMonthByPercent:{minDepositMonthByPercent}, minPercent:{minpercentPay.ToString("N",nfi)}");
    }
}