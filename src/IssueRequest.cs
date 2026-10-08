namespace LibraryDesk.Legacy;

public sealed record IssueRequest(
    Reader? Reader,
    List<BookCopy>? Books,
    DateTime IssuedOn,
    DateTime? ReturnedOn,
    SubscriptionType Subscription,
    string OperatorName);
