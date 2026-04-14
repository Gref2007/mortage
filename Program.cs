using System.Globalization;
using mortage;
using Spectre.Console;
using Spectre.Console.Rendering;

AnsiConsole.Write(new FigletText("Mortgage").Centered().Color(Color.Cyan1));
AnsiConsole.MarkupLine("[grey]Калькулятор досрочного погашения: депозит vs прямое внесение.[/]");
AnsiConsole.WriteLine();

var options = PromptForOptions();

AnsiConsole.WriteLine();
AnsiConsole.Write(RenderInputSummary(options));
AnsiConsole.WriteLine();

double allPays = 0, oneExtra = 0;
SweepResult sweep = null!;

AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .SpinnerStyle(Style.Parse("cyan1"))
    .Start("Считаю сценарии...", _ =>
    {
        allPays = Mortage.CalculateAllPays(options);
        oneExtra = Mortage.CalculateWithOneExtraPay(options);
        sweep = Mortage.CalculateWithAllExtraPayVariations(options);
    });

AnsiConsole.Write(RenderSummary(allPays, oneExtra, sweep));
AnsiConsole.WriteLine();
AnsiConsole.Write(RenderSweepChart(allPays, sweep));
AnsiConsole.WriteLine();

static MortageOptions PromptForOptions()
{
    AnsiConsole.MarkupLine("[cyan1]Введите параметры[/] [grey](Enter — значение по умолчанию)[/]");

    var mortgageSize = AnsiConsole.Prompt(
        new TextPrompt<int>("Размер кредита, ₽:")
            .DefaultValue(8_922_450)
            .ValidationErrorMessage("[red]нужно целое положительное число[/]")
            .Validate(v => v > 0 ? ValidationResult.Success() : ValidationResult.Error()));

    var mortgageInterest = AnsiConsole.Prompt(
        new TextPrompt<double>("Ставка кредита, % годовых:")
            .DefaultValue(6.0)
            .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("[red]не может быть отрицательной[/]")));

    var mortgageDate = PromptDate("Дата первого платежа (ДД.ММ.ГГГГ):", new DateTime(2025, 3, 3));

    var monthsLeft = AnsiConsole.Prompt(
        new TextPrompt<int>("Осталось месяцев:")
            .DefaultValue(360)
            .Validate(v => v > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]должно быть больше нуля[/]")));

    var extraMoney = AnsiConsole.Prompt(
        new TextPrompt<float>("Сумма допплатежа, ₽:")
            .DefaultValue(200_000f)
            .Validate(v => v > 0 ? ValidationResult.Success() : ValidationResult.Error("[red]должна быть больше нуля[/]")));

    var depositInterest = AnsiConsole.Prompt(
        new TextPrompt<float>("Ставка депозита, % годовых:")
            .DefaultValue(11.0f)
            .Validate(v => v >= 0 ? ValidationResult.Success() : ValidationResult.Error("[red]не может быть отрицательной[/]")));

    var depositMax = AnsiConsole.Prompt(
        new TextPrompt<int>("Макс. срок депозита для перебора, мес.:")
            .DefaultValue(360)
            .Validate(v => v > 1 ? ValidationResult.Success() : ValidationResult.Error("[red]нужно больше 1[/]")));

    var dateOfMoney = PromptDate("Дата получения денег (ДД.ММ.ГГГГ):", new DateTime(2026, 2, 3));

    return new MortageOptions
    {
        MortgageSize = mortgageSize,
        MortgageInterest = mortgageInterest,
        MortgageDate = mortgageDate,
        MonthsLeft = monthsLeft,
        ExtraPay = new ExtraPay
        {
            CountOfMoney = extraMoney,
            DepositInterest = depositInterest,
            DepositMaxMonthKeep = depositMax,
            DateOfMoney = dateOfMoney
        }
    };
}

static DateTime PromptDate(string prompt, DateTime defaultValue)
{
    const string fmt = "dd.MM.yyyy";
    var input = AnsiConsole.Prompt(
        new TextPrompt<string>(prompt)
            .DefaultValue(defaultValue.ToString(fmt))
            .Validate(s => DateTime.TryParseExact(s, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error("[red]формат: ДД.ММ.ГГГГ[/]")));
    return DateTime.ParseExact(input, fmt, CultureInfo.InvariantCulture);
}

static Panel RenderInputSummary(MortageOptions o)
{
    var table = new Table().Border(TableBorder.None).HideHeaders();
    table.AddColumn("");
    table.AddColumn("");
    table.AddRow("[grey]Кредит[/]",
        $"[white]{o.MortgageSize.ToString("N0", Mortage.Nfi)} ₽[/] под [yellow]{o.MortgageInterest}%[/] на [cyan1]{o.MonthsLeft}[/] мес.");
    table.AddRow("[grey]Первый платёж[/]", $"[white]{o.MortgageDate:dd.MM.yyyy}[/]");
    table.AddRow("[grey]Допплатёж[/]",
        $"[white]{o.ExtraPay.CountOfMoney.ToString("N0", Mortage.Nfi)} ₽[/] от [white]{o.ExtraPay.DateOfMoney:dd.MM.yyyy}[/]");
    table.AddRow("[grey]Депозит[/]",
        $"[yellow]{o.ExtraPay.DepositInterest}%[/] годовых, перебор до [cyan1]{o.ExtraPay.DepositMaxMonthKeep}[/] мес.");

    return new Panel(table)
        .Header(" [cyan1]Входные данные[/] ")
        .Border(BoxBorder.Rounded)
        .Expand();
}

static Panel RenderSummary(double allPays, double oneExtra, SweepResult sweep)
{
    var best = sweep.Best;
    var saveOne = allPays - oneExtra;
    var saveBest = allPays - best.TotalInterest;
    var gainOverOne = best.TotalInterest < oneExtra ? oneExtra - best.TotalInterest : 0;

    var table = new Table().Border(TableBorder.Rounded).Expand();
    table.AddColumn("Сценарий");
    table.AddColumn(new TableColumn("Проценты, ₽").RightAligned());
    table.AddColumn(new TableColumn("Экономия, ₽").RightAligned());

    table.AddRow(
        "[yellow]Без допплатежа[/]",
        $"[yellow]{allPays.ToString("N", Mortage.Nfi)}[/]",
        "[grey]—[/]");
    table.AddRow(
        "Один допплатёж сразу",
        oneExtra.ToString("N", Mortage.Nfi),
        $"[green]{saveOne.ToString("N", Mortage.Nfi)}[/]");
    table.AddRow(
        $"[bold green]Оптимум: {best.DepositMonths} мес. на депозите[/]",
        $"[bold green]{best.TotalInterest.ToString("N", Mortage.Nfi)}[/]",
        $"[bold green]{saveBest.ToString("N", Mortage.Nfi)}[/]");

    var footer = gainOverOne > 0
        ? $"[green]Подержать на депозите выгоднее, чем погасить сразу, на {gainOverOne.ToString("N", Mortage.Nfi)} ₽.[/]"
        : "[yellow]Погасить сразу — не хуже любого срока депозита.[/]";

    var grid = new Grid();
    grid.AddColumn();
    grid.AddRow(table);
    grid.AddRow(new Markup(footer));

    return new Panel(grid)
        .Header(" [cyan1]Итоги[/] ")
        .Border(BoxBorder.Rounded)
        .Expand();
}

static IRenderable RenderSweepChart(double baseline, SweepResult sweep)
{
    var selected = SampleSweep(sweep).ToList();

    var chart = new BarChart()
        .Label("[cyan1]Экономия vs «без допплатежа» в зависимости от срока депозита, тыс. ₽[/]")
        .CenterLabel()
        .WithMaxValue(Math.Round((baseline - sweep.Best.TotalInterest) / 1000));

    foreach (var p in selected)
    {
        var savingK = Math.Round((baseline - p.TotalInterest) / 1000);
        var color = p.DepositMonths == sweep.Best.DepositMonths ? Color.Green : Color.Cyan1;
        chart.AddItem($"{p.DepositMonths,3} мес.", savingK, color);
    }

    return new Panel(chart)
        .Header(" [cyan1]Свип по сроку депозита[/] ")
        .Border(BoxBorder.Rounded)
        .Expand();
}

static IEnumerable<SweepPoint> SampleSweep(SweepResult sweep)
{
    const int targetBars = 16;
    var byMonth = sweep.Points.ToDictionary(p => p.DepositMonths);
    var maxMonth = sweep.Points[^1].DepositMonths;
    var step = Math.Max(1, maxMonth / (targetBars - 1));

    var picked = new SortedSet<int>();
    for (var m = 1; m <= maxMonth; m += step)
        picked.Add(m);
    picked.Add(sweep.Best.DepositMonths);

    return picked.Where(byMonth.ContainsKey).Select(m => byMonth[m]);
}
