namespace mortage;

public class MortageOptions
{
    public int MortgageSize { get; set; }
    public double MortgageInterest { get; set; }
    public DateTime MortgageDate { get; set; }
    public int MonthsLeft { get; set; }
    
    public ExtraPay ExtraPay { get; set; }
}

public class ExtraPay{
    public float CountOfMoney { get; set; }= 2000000;
    public float DepositInterest { get; set; }= 11.0f;
    
    public int DepositMaxMonthKeep{ get; set; }= 100;
    
    public DateTime DateOfMoney { get; set; } = new DateTime(2026,02,03);//date when i have money
}