using System.Globalization;

namespace LibraryDesk.Legacy;

// Адаптовано з LegacyLab4/LoanManager і доменної моделі LibraryDesk.
// Клас навмисно зберігає дефекти початкового навчального зрізу.
public class LoanManager
{
    private const int MaximumActiveLoans = 5;
    private const int StudentLoanDays = 14;
    private const int TeacherLoanDays = 30;
    private const int ChildLoanDays = 7;
    private const int ReadingRoomLoanDays = 0;
    private const int ShortLoanDays = 3;
    private const decimal FinePerBookPerDay = 2m;
    private const decimal ChildFineRate = 0.5m;
    private const decimal MaximumFine = 500m;

    private readonly LoanRepository _repository;
    private readonly List<string> _log = new();
    private int _nextId = 1;
    private decimal _lastFine;

    public LoanManager(LoanRepository? repository = null)
    {
        _repository = repository ?? new LoanRepository();
    }

    public LoanResult Issue(
        Reader? reader,
        List<BookCopy>? books,
        DateTime issuedOn,
        DateTime? returnedOn,
        string subscription,
        bool sendEmail,
        bool printReceipt,
        string operatorName)
    {
        LoanResult result = new();

        if (reader == null)
        {
            result.Error = "ERR: reader";
            return result;
        }

        if (reader.IsBlocked)
        {
            result.Error = "ERR: blocked";
            return result;
        }

        if (books == null)
        {
            result.Error = "ERR: null-books";
            return result;
        }

        if (books.Count == 0)
        {
            result.Error = "ERR: empty";
            return result;
        }

        if (reader.ActiveLoans >= MaximumActiveLoans && reader.Category != "staff")
        {
            result.Error = "ERR: limit";
            return result;
        }

        for (int i = 0; i < books.Count; i++)
        {
            if (!books[i].IsAvailable || books[i].IsReferenceOnly)
            {
                result.Error = "ERR: book";
                return result;
            }
        }

        int days = StudentLoanDays;
        if (subscription == "student")
        {
            days = StudentLoanDays;
        }
        else if (subscription == "teacher")
        {
            days = TeacherLoanDays;
        }
        else if (subscription == "child")
        {
            days = ChildLoanDays;
        }
        else if (subscription == "reading-room")
        {
            days = ReadingRoomLoanDays;
        }

        for (int i = 0; i < books.Count; i++)
        {
            if (books[i].Group == "short")
            {
                days = ShortLoanDays;
            }
        }

        Loan loan = new();
        loan.Id = _nextId++;
        loan.Reader = reader;
        loan.Books = books;
        loan.IssuedOn = issuedOn;
        loan.DueOn = issuedOn.Date.AddDays(days);
        loan.Status = "active";
        loan.CreatedBy = operatorName;

        _lastFine = 0m;
        if (returnedOn != null)
        {
            if (returnedOn.Value.Date > loan.DueOn.Date)
            {
                int overdueDays = (returnedOn.Value.Date - loan.DueOn.Date).Days;
                _lastFine = overdueDays * FinePerBookPerDay * books.Count;
                if (reader.Category == "child")
                {
                    _lastFine = _lastFine * ChildFineRate;
                }

                if (_lastFine > MaximumFine)
                {
                    _lastFine = MaximumFine;
                }

                loan.Status = "overdue";
            }
            else
            {
                loan.Status = "returned";
            }

            loan.ReturnedOn = returnedOn;
            loan.Fine = _lastFine;
        }

        string receipt = "Видача #" + loan.Id + Environment.NewLine;
        receipt += "Читач: " + reader.Name + Environment.NewLine;
        receipt += "Видано: " + issuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Environment.NewLine;
        receipt += "Повернути: " + loan.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Environment.NewLine;
        for (int i = 0; i < books.Count; i++)
        {
            receipt += books[i].InventoryCode + " — " + books[i].Title + Environment.NewLine;
            books[i].IsAvailable = false;
        }

        receipt += "Пеня: " + loan.Fine.ToString("0.00", CultureInfo.InvariantCulture) + Environment.NewLine;
        receipt += "Статус: " + loan.Status + Environment.NewLine;

        try
        {
            _repository.Save(loan);
        }
        catch
        {
            // Історична поведінка: помилка збереження не зупиняє видачу.
        }

        if (sendEmail)
        {
            if (reader.Email.Contains('@'))
            {
                _log.Add("mail -> " + reader.Email);
            }
        }

        if (printReceipt)
        {
            Console.Write(receipt);
        }

        reader.ActiveLoans++;
        result.Success = true;
        result.Loan = loan;
        result.Receipt = receipt;

        // Старий варіант обмежував видачу двома книгами.
        // if (books.Count > 2) return new LoanResult();
        return result;
    }

    public decimal PreviewFine(Loan loan, DateTime onDate)
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

        return fine;
    }

    public bool ChangeStatus(Loan loan, string next)
    {
        if (loan.Status == "new" && next == "active")
        {
            loan.Status = next;
            return true;
        }

        if (loan.Status == "active" && (next == "returned" || next == "overdue" || next == "lost"))
        {
            loan.Status = next;
            return true;
        }

        if (loan.Status == "overdue" && next == "returned")
        {
            loan.Status = next;
            return true;
        }

        return false;
    }

    public bool CanIssue(Reader? reader, List<BookCopy>? books)
    {
        if (reader == null || books == null || books.Count == 0)
        {
            return false;
        }

        if (reader.IsBlocked || (reader.ActiveLoans >= MaximumActiveLoans && reader.Category != "staff"))
        {
            return false;
        }

        foreach (BookCopy book in books)
        {
            if (!book.IsAvailable || book.IsReferenceOnly)
            {
                return false;
            }
        }

        return true;
    }

    public bool Cancel(Loan loan, string reason)
    {
        if (loan.Status == "new" || loan.Status == "active")
        {
            loan.Status = "cancelled";
            loan.Note = reason;
            foreach (BookCopy book in loan.Books)
            {
                book.IsAvailable = true;
            }

            return true;
        }

        return false;
    }

    public string DumpLog()
    {
        string result = "";
        foreach (string entry in _log)
        {
            result += entry + Environment.NewLine;
        }

        return result;
    }
}
