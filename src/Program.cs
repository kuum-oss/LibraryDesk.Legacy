using LibraryDesk.Legacy;

Reader reader = new()
{
    Id = 7,
    Name = "Олена Коваль",
    Email = "olena@example.test",
    Category = ReaderCategory.Regular,
};

List<BookCopy> books =
[
    new BookCopy
    {
        InventoryCode = "BK-001",
        Title = "Чистий код",
        Group = BookGroup.Regular,
        Price = 450m,
    },
    new BookCopy
    {
        InventoryCode = "BK-002",
        Title = "Рефакторинг",
        Group = BookGroup.Regular,
        Price = 520m,
    },
];

LoanManager manager = new();
LoanResult result = manager.IssueAndNotify(
    new IssueRequest(
        reader,
        books,
        new DateTime(2026, 10, 1),
        new DateTime(2026, 10, 20),
        SubscriptionType.Student,
        "demo"));

Console.Write(result.Receipt);
Console.Write(manager.DumpLog());
