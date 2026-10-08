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
    private readonly LoanRepository _repository;
    private readonly FineCalculator _fineCalculator = new();
    private readonly List<string> _log = new();
    private int _nextId = 1;

    public LoanManager(LoanRepository? repository = null)
    {
        _repository = repository ?? new LoanRepository();
    }

    public LoanResult Issue(IssueRequest request, bool sendEmail, bool printReceipt)
    {
        string error = ValidateIssue(request.Reader, request.Books);
        if (error.Length > 0)
        {
            return new LoanResult { Error = error };
        }

        int days = LoanDays(request.Subscription, request.Books!);
        Loan loan = CreateLoan(
            request.Reader!,
            request.Books!,
            request.IssuedOn,
            days,
            request.OperatorName);
        CompleteReturn(loan, request.ReturnedOn);
        string receipt = BuildReceipt(loan);
        SaveIgnoringFailure(loan);
        SendEmailIfRequested(request.Reader!, sendEmail);
        PrintIfRequested(receipt, printReceipt);
        request.Reader!.ActiveLoans++;

        return new LoanResult { Success = true, Loan = loan, Receipt = receipt };
    }

    private static string ValidateIssue(Reader? reader, List<BookCopy>? books)
    {
        if (reader == null) return "ERR: reader";
        if (reader.IsBlocked) return "ERR: blocked";
        if (books == null) return "ERR: null-books";
        if (books.Count == 0) return "ERR: empty";
        if (reader.ActiveLoans >= MaximumActiveLoans && reader.Category != "staff") return "ERR: limit";
        return books.Any(book => !book.IsAvailable || book.IsReferenceOnly) ? "ERR: book" : "";
    }

    private static int LoanDays(string subscription, List<BookCopy> books)
    {
        int days = subscription switch
        {
            "teacher" => TeacherLoanDays,
            "child" => ChildLoanDays,
            "reading-room" => ReadingRoomLoanDays,
            _ => StudentLoanDays,
        };

        return books.Any(book => book.Group == "short") ? ShortLoanDays : days;
    }

    private Loan CreateLoan(
        Reader reader,
        List<BookCopy> books,
        DateTime issuedOn,
        int days,
        string operatorName)
    {
        Loan loan = new()
        {
            Id = _nextId++,
            Reader = reader,
            IssuedOn = issuedOn,
            DueOn = issuedOn.Date.AddDays(days),
            Status = "active",
            CreatedBy = operatorName,
        };
        loan.SetBooks(books);
        return loan;
    }

    private void CompleteReturn(Loan loan, DateTime? returnedOn)
    {
        if (returnedOn == null) return;
        loan.Status = returnedOn.Value.Date > loan.DueOn.Date ? "overdue" : "returned";
        loan.Fine = _fineCalculator.Calculate(loan, returnedOn.Value);
        loan.ReturnedOn = returnedOn;
    }

    private static string BuildReceipt(Loan loan)
    {
        string receipt = "Видача #" + loan.Id + Environment.NewLine;
        receipt += "Читач: " + loan.Reader!.Name + Environment.NewLine;
        receipt += "Видано: " + loan.IssuedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Environment.NewLine;
        receipt += "Повернути: " + loan.DueOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + Environment.NewLine;
        foreach (BookCopy book in loan.Books)
        {
            receipt += book.InventoryCode + " — " + book.Title + Environment.NewLine;
            book.IsAvailable = false;
        }

        receipt += "Пеня: " + loan.Fine.ToString("0.00", CultureInfo.InvariantCulture) + Environment.NewLine;
        receipt += "Статус: " + loan.Status + Environment.NewLine;
        return receipt;
    }

    private void SaveIgnoringFailure(Loan loan)
    {
        try
        {
            _repository.Save(loan);
        }
        catch
        {
            // Історична поведінка: помилка збереження не зупиняє видачу.
        }
    }

    private void SendEmailIfRequested(Reader reader, bool sendEmail)
    {
        if (sendEmail && reader.Email.Contains('@'))
        {
            _log.Add("mail -> " + reader.Email);
        }
    }

    private static void PrintIfRequested(string receipt, bool printReceipt)
    {
        if (printReceipt)
        {
            Console.Write(receipt);
        }
    }

    public bool ChangeStatus(Loan loan, string next)
    {
        bool canFinishActiveLoan = loan.Status == "active"
            && (next == "returned" || next == "overdue" || next == "lost");

        if (loan.Status == "new" && next == "active")
        {
            loan.Status = next;
            return true;
        }

        if (canFinishActiveLoan)
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
