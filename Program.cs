using mortage;

var mortageOptions = new MortageOptions
{
    MortgageSize = 8922450,
    MortgageInterest = 6,
    MortgageDate = new DateTime(2025,03,03),
    MonthsLeft = 360,
    ExtraPay = new ExtraPay
    {
        CountOfMoney = 200000,
        DepositInterest = 11,
        DepositMaxMonthKeep = 360,
        DateOfMoney = new DateTime(2026,02,03)
    }
};

Console.WriteLine($"CalculateAllPays: PercendPayed {Mortage.CalculateAllPays(mortageOptions)}");
Console.WriteLine($"CalculateWithOneExtraPay: PercendPayed {Mortage.CalculateWithOneExtraPay(mortageOptions)}");
Mortage.CalculateWithAllExtraPayVariations(mortageOptions);