using System.Globalization;

namespace LibraryDesk.Legacy;

public class LoanReport
{
    private readonly FineCalculator _fineCalculator = new();

    public string BuildOverdueReport(List<Loan> loans, DateTime onDate, bool includeEmail)
    {
        string report = "id;reader;due;fine" + Environment.NewLine;
        foreach (Loan loan in loans)
        {
            decimal fine = _fineCalculator.Calculate(loan, onDate);
            if (fine <= 0m)
            {
                continue;
            }

            report += loan.Id + ";";
            report += loan.Reader?.Name + ";";
            report += loan.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ";";
            report += fine.ToString("0.00", CultureInfo.InvariantCulture);
            if (includeEmail)
            {
                report += ";" + loan.Reader?.Email;
            }

            report += Environment.NewLine;
        }

        return report;
    }

    public string BuildReaderCard(Reader reader, List<Loan> loans)
    {
        string text = reader.Name.Trim().ToUpperInvariant();
        text += " [" + reader.Category + "]";
        text += " active=" + reader.ActiveLoans;
        text += " fine=" + reader.UnpaidFine.ToString("0.00", CultureInfo.InvariantCulture);
        for (int i = 0; i < loans.Count; i++)
        {
            if (loans[i].Reader != null && loans[i].Reader!.Id == reader.Id)
            {
                text += Environment.NewLine + loans[i].Id + ":"
                    + loans[i].Status.ToString().ToLowerInvariant();
            }
        }

        return text;
    }

    public string BuildInventoryReport(List<BookCopy> books)
    {
        string report = "code;title;available" + Environment.NewLine;
        foreach (BookCopy book in books)
        {
            report += book.InventoryCode + ";";
            report += book.Title + ";";
            report += book.IsAvailable + Environment.NewLine;
        }

        return report;
    }

    public void SaveReport(string path, string report)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required", nameof(path));
        }

        File.WriteAllText(path, report);
        Console.WriteLine("Звіт збережено: " + path);
    }
}
