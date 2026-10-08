using LibraryDesk.Legacy;
using Xunit;

namespace LibraryDesk.Legacy.Tests;

public sealed class LoanManagerCharacterizationTests : IDisposable
{
    private readonly string _storePath = Path.Combine(
        Path.GetTempPath(),
        $"sr02-{Guid.NewGuid():N}.json");

    [Fact]
    public void Issue_StudentSubscription_DueIn14Days()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), Books(), new DateTime(2026, 10, 1), null, SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.True(actual.Success);
        Assert.Equal(new DateTime(2026, 10, 15), actual.Loan!.DueOn);
    }

    [Fact]
    public void Issue_TeacherSubscription_DueIn30Days()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), Books(), new DateTime(2026, 10, 1), null, SubscriptionType.Teacher),
            false,
            false);

        // assert
        Assert.Equal(new DateTime(2026, 10, 31), actual.Loan!.DueOn);
    }

    [Fact]
    public void Issue_ChildSubscription_DueIn7Days()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(ReaderCategory.Child), Books(), new DateTime(2026, 10, 1), null, SubscriptionType.Child),
            false,
            false);

        // assert
        Assert.Equal(new DateTime(2026, 10, 8), actual.Loan!.DueOn);
    }

    [Fact]
    public void Issue_ReadingRoomSubscription_DueSameDay()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), Books(), new DateTime(2026, 10, 1), null, SubscriptionType.ReadingRoom),
            false,
            false);

        // assert
        Assert.Equal(new DateTime(2026, 10, 1), actual.Loan!.DueOn);
    }

    [Fact]
    public void Issue_ShortLoanBook_DueIn3Days()
    {
        // arrange
        LoanManager sut = CreateManager();
        List<BookCopy> books = Books(group: BookGroup.Short);

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), books, new DateTime(2026, 10, 1), null, SubscriptionType.Teacher),
            false,
            false);

        // assert
        Assert.Equal(new DateTime(2026, 10, 4), actual.Loan!.DueOn);
    }

    [Fact]
    public void Issue_FiveOverdueDaysForTwoBooks_FineIs20()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(
                Reader(), Books(count: 2), new DateTime(2026, 10, 1),
                new DateTime(2026, 10, 20), SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.Equal(20m, actual.Loan!.Fine);
        Assert.Equal(LoanStatus.Overdue, actual.Loan.Status);
    }

    [Fact]
    public void Issue_ChildReturnedTwoDaysLate_FineIsHalved()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(
                Reader(ReaderCategory.Child), Books(), new DateTime(2026, 10, 1),
                new DateTime(2026, 10, 10), SubscriptionType.Child),
            false,
            false);

        // assert
        Assert.Equal(2m, actual.Loan!.Fine);
    }

    [Fact]
    public void Issue_VeryLateReturn_FineIsCappedAt500()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(
                Reader(), Books(count: 2), new DateTime(2025, 1, 1),
                new DateTime(2026, 1, 1), SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.Equal(500m, actual.Loan!.Fine);
    }

    [Fact]
    public void Issue_NullReader_ReturnsReaderError()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(null, Books(), new DateTime(2026, 10, 1), null, SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.False(actual.Success);
        Assert.Equal("ERR: reader", actual.Error);
    }

    [Fact]
    public void Issue_EmptyBookList_ReturnsEmptyError()
    {
        // arrange
        LoanManager sut = CreateManager();

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), [], new DateTime(2026, 10, 1), null, SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.False(actual.Success);
        Assert.Equal("ERR: empty", actual.Error);
    }

    [Fact]
    public void Issue_UnavailableBook_ReturnsBookError()
    {
        // arrange
        LoanManager sut = CreateManager();
        List<BookCopy> books = Books();
        books[0].IsAvailable = false;

        // act
        LoanResult actual = sut.Issue(
            Request(Reader(), books, new DateTime(2026, 10, 1), null, SubscriptionType.Student),
            false,
            false);

        // assert
        Assert.False(actual.Success);
        Assert.Equal("ERR: book", actual.Error);
    }

    [Fact]
    public void ChangeStatus_ActiveToReturned_ChangesState()
    {
        // arrange
        LoanManager sut = CreateManager();
        Loan loan = new() { Status = LoanStatus.Active };

        // act
        bool actual = sut.ChangeStatus(loan, LoanStatus.Returned);

        // assert
        Assert.True(actual);
        Assert.Equal(LoanStatus.Returned, loan.Status);
    }

    [Fact]
    public void ChangeStatus_ReturnedToActive_LeavesStateUnchanged()
    {
        // arrange
        LoanManager sut = CreateManager();
        Loan loan = new() { Status = LoanStatus.Returned };

        // act
        bool actual = sut.ChangeStatus(loan, LoanStatus.Active);

        // assert
        Assert.False(actual);
        Assert.Equal(LoanStatus.Returned, loan.Status);
    }

    public void Dispose()
    {
        File.Delete(_storePath);
        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "loans.json"));
    }

    private LoanManager CreateManager()
    {
        // Reflection keeps the same test source compilable at the baseline tag,
        // where the optional repository seam did not exist yet.
        var repositoryConstructor = typeof(LoanRepository).GetConstructor([typeof(string)]);
        var managerConstructor = typeof(LoanManager).GetConstructor([typeof(LoanRepository)]);
        if (repositoryConstructor is null || managerConstructor is null)
        {
            return new LoanManager();
        }

        var repository = (LoanRepository)repositoryConstructor.Invoke([_storePath]);
        return (LoanManager)managerConstructor.Invoke([repository]);
    }

    private static IssueRequest Request(
        Reader? reader,
        List<BookCopy>? books,
        DateTime issuedOn,
        DateTime? returnedOn,
        SubscriptionType subscription)
    {
        return new IssueRequest(reader, books, issuedOn, returnedOn, subscription, "test");
    }

    private static Reader Reader(ReaderCategory category = ReaderCategory.Regular)
    {
        return new Reader
        {
            Id = 7,
            Name = "Олена Коваль",
            Email = "olena@example.test",
            Category = category,
        };
    }

    private static List<BookCopy> Books(int count = 1, BookGroup group = BookGroup.Regular)
    {
        List<BookCopy> books = new();
        for (int index = 0; index < count; index++)
        {
            books.Add(new BookCopy
            {
                InventoryCode = $"BK-{index + 1:000}",
                Title = $"Книга {index + 1}",
                Group = group,
                IsAvailable = true,
            });
        }

        return books;
    }
}
