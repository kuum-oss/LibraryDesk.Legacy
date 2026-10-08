namespace LibraryDesk.Legacy;

public class Reader
{
    public int Id;
    public string Name = "";
    public string Email = "";
    public string Category = "regular";
    public bool IsBlocked;
    public int ActiveLoans;
    public decimal UnpaidFine;
}

public class BookCopy
{
    public string InventoryCode = "";
    public string Title = "";
    public string Group = "regular";
    public decimal Price;
    public bool IsReferenceOnly;
    public bool IsAvailable = true;
}

public class Loan
{
    public int Id;
    public Reader? Reader;
    public List<BookCopy> Books = new();
    public DateTime IssuedOn;
    public DateTime DueOn;
    public DateTime? ReturnedOn;
    public string Status = "new";
    public decimal Fine;
    public string CreatedBy = "";
    public string Note = "";
}

public class LoanResult
{
    public bool Success;
    public string Error = "";
    public Loan? Loan;
    public string Receipt = "";
}

