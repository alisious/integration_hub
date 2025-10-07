// IntegrationHub.Sources.CEP.Udostepnianie.Contracts/FaultResponse.cs
namespace IntegrationHub.Sources.CEP.Udostepnianie.Contracts
{
    /// <summary>
    /// Reprezentacja błędu SOAP/Fault zwracanego przez CEP/CEPIK.
    /// </summary>
    public sealed class FaultResponse
    {
        // Pola ogólne SOAP 1.1
        public string? FaultCode { get; set; }      // <faultcode>
        public string? FaultString { get; set; }    // <faultstring>

        // Szczegóły CEPIK (detail -> exc:cepikException -> komunikaty)
        public string? Typ { get; set; }            // <typ> (np. ERROR)
        public string? Kod { get; set; }            // <kod> (np. UDO-1006)
        public string? Komunikat { get; set; }      // <komunikat>
        public string? Szczegoly { get; set; }      // <szczegoly> (może być puste)
    }
}
