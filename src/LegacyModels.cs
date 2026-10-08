namespace LibraryDesk.Legacy;

public class Reader
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public ReaderCategory Category { get; set; } = ReaderCategory.Regular;
    public bool IsBlocked { get; set; }
    public int ActiveLoans { get; set; }
    public decimal UnpaidFine { get; set; }
}

public class BookCopy
{
    public string InventoryCode { get; set; } = "";
    public string Title { get; set; } = "";
    public BookGroup Group { get; set; } = BookGroup.Regular;
    public decimal Price { get; set; }
    public bool IsReferenceOnly { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class Loan
{
    private readonly List<BookCopy> _books = new();

    public int Id { get; set; }
    public Reader? Reader { get; set; }
    public IReadOnlyList<BookCopy> Books => _books.AsReadOnly();
    public DateTime IssuedOn { get; set; }
    public DateTime DueOn { get; set; }
    public DateTime? ReturnedOn { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.New;
    public decimal Fine { get; set; }
    public string CreatedBy { get; set; } = "";
    public string Note { get; set; } = "";

    public void SetBooks(IEnumerable<BookCopy> books)
    {
        _books.Clear();
        _books.AddRange(books);
    }
}

public class LoanResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = "";
    public Loan? Loan { get; set; }
    public string Receipt { get; set; } = "";
}
