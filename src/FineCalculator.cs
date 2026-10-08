namespace LibraryDesk.Legacy;

public sealed class FineCalculator
{
    public decimal FinePerBookPerDay => 2m;

    public decimal ChildFineRate => 0.5m;

    public decimal MaximumFine => 500m;

    public decimal Calculate(Loan loan, DateTime onDate)
    {
        if (onDate.Date <= loan.DueOn.Date)
        {
            return 0m;
        }

        int overdueDays = (onDate.Date - loan.DueOn.Date).Days;
        decimal fine = overdueDays * FinePerBookPerDay * loan.Books.Count;
        if (loan.Reader?.Category == "child")
        {
            fine *= ChildFineRate;
        }

        return Math.Min(fine, MaximumFine);
    }
}
