using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryDesk.Legacy;

public class LoanRepository
{
    // TODO TD-02: додати тести сховища до ЛР-7; ціль покриття >= 70%.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
    };

    private readonly List<Loan> _loans = new();
    private readonly string _path;
    private readonly FineCalculator _fineCalculator = new();

    public LoanRepository(string? path = null)
    {
        // TODO TD-05: замінити прямий File API адаптером до ЛР-8.
        _path = path ?? "loans.json";
    }

    public void Save(Loan loan)
    {
        _loans.Add(loan);
        File.WriteAllText(_path, JsonSerializer.Serialize(_loans, JsonOptions));
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

    public List<Loan> FindByStatus(LoanStatus status)
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
            total += _fineCalculator.Calculate(loan, onDate);
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
