using LibraryDesk.Legacy;

Reader reader = new()
{
    Id = 7,
    Name = "Олена Коваль",
    Email = "olena@example.test",
    Category = "student",
};

List<BookCopy> books =
[
    new BookCopy
    {
        InventoryCode = "BK-001",
        Title = "Чистий код",
        Group = "regular",
        Price = 450m,
    },
    new BookCopy
    {
        InventoryCode = "BK-002",
        Title = "Рефакторинг",
        Group = "regular",
        Price = 520m,
    },
];

LoanManager manager = new();
LoanResult result = manager.Issue(
    reader,
    books,
    new DateTime(2026, 10, 1),
    new DateTime(2026, 10, 20),
    "student",
    sendEmail: true,
    printReceipt: false,
    operatorName: "demo");

Console.Write(result.Receipt);
Console.Write(manager.DumpLog());

