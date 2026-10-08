namespace LibraryDesk.Legacy;

public sealed record IssueRequest(
    Reader? Reader,
    List<BookCopy>? Books,
    DateTime IssuedOn,
    DateTime? ReturnedOn,
    string Subscription,
    string OperatorName);

