namespace LibraryDesk.Legacy;

public sealed class FineCalculator
{
    public decimal FinePerBookPerDay => 2m;

    public decimal ChildFineRate => 0.5m;

    public decimal MaximumFine => 500m;
}

