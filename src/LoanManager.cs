using System.Globalization;

namespace LibraryDesk.Legacy;

// Адаптовано з LegacyLab4/LoanManager і доменної моделі LibraryDesk.
// Клас навмисно зберігає дефекти початкового навчального зрізу.
public class LoanManager
{
    // TODO TD-03: fan-out = 11; розділити координацію до ЛР-8.
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

    public LoanResult Issue(IssueRequest request)
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
        request.Reader!.ActiveLoans++;

        return new LoanResult { Success = true, Loan = loan, Receipt = receipt };
    }

    public LoanResult IssueAndNotify(IssueRequest request)
    {
        LoanResult result = Issue(request);
        if (result.Success)
        {
            SendEmail(request.Reader!);
        }

        return result;
    }

    public LoanResult IssueAndPrint(IssueRequest request)
    {
        LoanResult result = Issue(request);
        if (result.Success)
        {
            PrintReceipt(result.Receipt);
        }

        return result;
    }

    public LoanResult IssueNotifyAndPrint(IssueRequest request)
    {
        LoanResult result = Issue(request);
        if (result.Success)
        {
            SendEmail(request.Reader!);
            PrintReceipt(result.Receipt);
        }

        return result;
    }

    private static string ValidateIssue(Reader? reader, List<BookCopy>? books)
    {
        if (reader == null) return "ERR: reader";
        if (reader.IsBlocked) return "ERR: blocked";
        if (books == null) return "ERR: null-books";
        if (books.Count == 0) return "ERR: empty";
        if (reader.ActiveLoans >= MaximumActiveLoans && reader.Category != ReaderCategory.Staff) return "ERR: limit";
        return books.Any(book => !book.IsAvailable || book.IsReferenceOnly) ? "ERR: book" : "";
    }

    private static int LoanDays(SubscriptionType subscription, List<BookCopy> books)
    {
        int days = subscription switch
        {
            SubscriptionType.Teacher => TeacherLoanDays,
            SubscriptionType.Child => ChildLoanDays,
            SubscriptionType.ReadingRoom => ReadingRoomLoanDays,
            _ => StudentLoanDays,
        };

        return books.Any(book => book.Group == BookGroup.Short) ? ShortLoanDays : days;
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
            Status = LoanStatus.Active,
            CreatedBy = operatorName,
        };
        loan.SetBooks(books);
        return loan;
    }

    private void CompleteReturn(Loan loan, DateTime? returnedOn)
    {
        if (returnedOn == null) return;
        loan.Status = returnedOn.Value.Date > loan.DueOn.Date
            ? LoanStatus.Overdue
            : LoanStatus.Returned;
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
        receipt += "Статус: " + loan.Status.ToString().ToLowerInvariant() + Environment.NewLine;
        return receipt;
    }

    private void SaveIgnoringFailure(Loan loan)
    {
        // TODO TD-01: не ковтати IOException; виправити окремим fix до ЛР-7.
        try
        {
            _repository.Save(loan);
        }
        catch
        {
            // Історична поведінка: помилка збереження не зупиняє видачу.
        }
    }

    private void SendEmail(Reader reader)
    {
        if (reader.Email.Contains('@'))
        {
            _log.Add("mail -> " + reader.Email);
        }
    }

    private static void PrintReceipt(string receipt)
    {
        Console.Write(receipt);
    }

    public bool ChangeStatus(Loan loan, LoanStatus next)
    {
        bool canFinishActiveLoan = loan.Status == LoanStatus.Active
            && next is LoanStatus.Returned or LoanStatus.Overdue or LoanStatus.Lost;

        if (loan.Status == LoanStatus.New && next == LoanStatus.Active)
        {
            loan.Status = next;
            return true;
        }

        if (canFinishActiveLoan)
        {
            loan.Status = next;
            return true;
        }

        if (loan.Status == LoanStatus.Overdue && next == LoanStatus.Returned)
        {
            loan.Status = next;
            return true;
        }

        return false;
    }

    public bool CanIssue(Reader? reader, List<BookCopy>? books)
    {
        // TODO TD-04: CC = 10; об'єднати з ValidateIssue на тижні 10.
        if (reader == null || books == null || books.Count == 0)
        {
            return false;
        }

        if (reader.IsBlocked || (reader.ActiveLoans >= MaximumActiveLoans && reader.Category != ReaderCategory.Staff))
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
        if (loan.Status is LoanStatus.New or LoanStatus.Active)
        {
            loan.Status = LoanStatus.Cancelled;
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
