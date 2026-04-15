# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Single-project C# console application (.NET 8, pinned by `global.json` to SDK `8.0.0` with `rollForward: latestMinor`) that computes total mortgage interest paid under three scenarios. Interactive TUI built on **Spectre.Console 0.55** — inputs come from prompts, results render as panels/tables/`BarChart`. No config file, no tests.

Note: the project name `mortage` / class `Mortage` is a typo for "mortgage" but is used consistently as the namespace and type name. Do not rename without updating the `.csproj`, `.sln`, and namespace together.

## Commands

```bash
dotnet build             # build
dotnet run               # build + interactive TUI
dotnet run -c Release    # optimized run
```

Requires an interactive TTY (Spectre.Console prompts call `ReadKey`). Piping stdin with `|` fails with "Failed to read input in non-interactive mode" — for automated runs wrap with `script -q /dev/null dotnet run --no-build` on macOS to allocate a pty.

Defaults baked into `PromptForOptions()` in `Program.cs` preserve the original hardcoded scenario (8 922 450 ₽ @ 6% / 360 mo, 200 000 ₽ deposit @ 11% from 03.02.2026). Press Enter to accept each prompt.

## Architecture

Three files, one namespace `mortage`:

- `Program.cs` — top-level entry. Spectre.Console UI: title, `PromptForOptions()` gathers inputs, `AnsiConsole.Status` runs calculations with a spinner, then three render helpers (`RenderInputSummary`, `RenderSummary`, `RenderSweepChart`) produce panels. Dates are prompted as strings and validated against `dd.MM.yyyy` to avoid culture-dependent parsing.
- `MortageOptions.cs` — two POCOs: `MortageOptions` (loan params) and `ExtraPay` (one-time lump sum + deposit parameters). `ExtraPay.DateOfMoney` is the date the lump sum becomes available.
- `Mortage.cs` — calculation logic as `static` methods. Public API returns data, not `Console.WriteLine`: `CalculateAllPays` / `CalculateWithOneExtraPay` return `double` total interest; `CalculateWithAllExtraPayVariations` returns `SweepResult(IReadOnlyList<SweepPoint> Points, SweepPoint Best)` where `SweepPoint = (DepositMonths, TotalInterest, DepositGrown)`. Rendering lives entirely in `Program.cs`.

### Calculation model

The core loop is annuity amortization iterated month-by-month until `loanAmount <= 0`:

1. `GetMonthPayment` — standard annuity formula `P * (r * (1+r)^n) / ((1+r)^n - 1)`, computed once at the start of each scenario (never recomputed after an extra payment — the fixed monthly amount continues, shortening the term).
2. `CalculatePercentPayMonth` — interest for one month, computed per actual day count between `payDate` and `payDate - 1 month`, divided by 365/366 depending on leap year. This is Russian-style "daily interest over actual days in period", not a flat `balance * r/12`.
3. Principal for the month = `monthPayment - interestPortion`, rounded with `MidpointRounding.ToEven`.

### The three scenarios

- `CalculateAllPays` — pure amortization with no extras. Returns total interest.
- `CalculateWithOneExtraPay` — same loop, but when the iterator's month matches `ExtraPay.DateOfMoney`'s year+month, the full `ExtraPay.CountOfMoney` is subtracted from the balance (immediate prepayment). Returns total interest.
- `CalculateWithAllExtraPayVariations` — sweeps `i` from 1 to `DepositMaxMonthKeep`. For each `i`, the lump sum sits on a deposit accruing `DepositInterest` compounded monthly for `i` months *after* `DateOfMoney`, then the grown amount is applied as a single prepayment. Returns all points plus the `i*` minimizing interest paid. This answers "how long should I keep the money on deposit before prepaying?". The sweep chart in `Program.cs` samples ~16 points across the full range so the curve shape is visible; the optimum bar is rendered in green.

Key subtlety: in `CalculateWithAllExtraPayVariations`, after applying the deposit, `depositMonthLeft` is set to `-100000` as a sentinel to ensure the one-shot prepayment fires exactly once. Preserve this guard if refactoring.

## Conventions

- UI strings (prompts, panel headers, summary text) are in Russian — this is a personal tool for a Russian-speaking user. Code comments should be English and lowercase per global rules.
- Number formatting uses `Mortage.Nfi` (space thousands separator, comma decimal — Russian locale convention). Reuse it for any monetary output; embed via `value.ToString("N", Mortage.Nfi)`.
- Monetary values are `double` throughout. `ExtraPay.CountOfMoney` / `DepositInterest` are `float`, and the compounding inside `CalculateWithAllExtraPayVariations` stays in `float` intentionally — changing to `double` would shift results slightly. Rounding is explicit at each step (`Math.Round(..., 2)` with `MidpointRounding.ToEven` for principal); don't introduce `decimal` piecemeal.
- Spectre markup tags (`[cyan1]...[/]`, `[bold green]...[/]`) are inside string literals — escape literal `[` as `[[` if ever needed.
