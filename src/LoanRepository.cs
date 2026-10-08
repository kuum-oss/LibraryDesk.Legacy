using System.Text.Json;

namespace LibraryDesk.Legacy;

public class LoanRepository
{
    private const decimal FinePerBookPerDay = 2m;
    private const decimal ChildFineRate = 0.5m;
    private const decimal MaximumFine = 500m;

    private readonly List<Loan> _loans = new();
    private readonly string _path;

    public LoanRepository(string? path = null)
    {
        _path = path ?? "loans.json";
    }

    public void Save(Loan loan)
    {
        _loans.Add(loan);
        File.WriteAllText(_path, JsonSerializer.Serialize(_loans));
    }

    public Loan? Find(int id)
    {
        for (int i = 0; i < _loans.Count; i++)
        {
            if (_loans[i].Id == id)
            {
                return _loans[i];
            }
        }

        return null;
    }

    public List<Loan> FindByReader(int readerId)
    {
        List<Loan> result = new();
        foreach (Loan loan in _loans)
        {
            if (loan.Reader != null && loan.Reader.Id == readerId)
            {
                result.Add(loan);
            }
        }

        return result;
    }

    public List<Loan> FindByStatus(string status)
    {
        List<Loan> result = new();
        for (int i = 0; i < _loans.Count; i++)
        {
            if (_loans[i].Status == status)
            {
                result.Add(_loans[i]);
            }
        }

        return result;
    }

    public decimal SumOutstandingFines(DateTime onDate)
    {
        decimal total = 0m;
        foreach (Loan loan in _loans)
        {
            decimal fine = 0m;
            if (onDate.Date > loan.DueOn.Date)
            {
                int overdueDays = (onDate.Date - loan.DueOn.Date).Days;
                fine = overdueDays * FinePerBookPerDay * loan.Books.Count;
                if (loan.Reader != null && loan.Reader.Category == "child")
                {
                    fine = fine * ChildFineRate;
                }

                if (fine > MaximumFine)
                {
                    fine = MaximumFine;
                }
            }

            total += fine;
        }

        return total;
    }

    public void Clear()
    {
        try
        {
            File.Delete(_path);
        }
        catch
        {
        }

        _loans.Clear();
    }
}
