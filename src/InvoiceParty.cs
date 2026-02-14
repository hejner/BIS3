namespace Taskr.Bis3;

public class InvoiceParty
{
    public required string Name { get; set; }
    public string? VatId { get; set; }
    public string? EndpointId { get; set; }
    public required InvoiceAddress Address { get; set; }
}
