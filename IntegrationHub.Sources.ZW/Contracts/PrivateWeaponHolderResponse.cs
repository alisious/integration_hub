using System.Text.Json.Serialization;

namespace IntegrationHub.Sources.ZW.Contracts
{
    public sealed class PrivateWeaponHolderResponse
    {
        [JsonPropertyName("pesel")]
        public string Pesel { get; set; } = default!;

        [JsonPropertyName("adresy")]
        public IReadOnlyList<WeaponAddressDto> Adresy { get; set; } = new List<WeaponAddressDto>();
    }

    public sealed class WeaponAddressDto
    {
        [JsonPropertyName("miejscowosc")]
        public string Miejscowosc { get; set; } = default!;

        [JsonPropertyName("ulica")]
        public string Ulica { get; set; } = default!;

        [JsonPropertyName("numerDomu")]
        public string NumerDomu { get; set; } = default!;

        [JsonPropertyName("numerLokalu")]
        public string? NumerLokalu { get; set; }

        [JsonPropertyName("kodPocztowy")]
        public string? KodPocztowy { get; set; }

        [JsonPropertyName("poczta")]
        public string Poczta { get; set; } = default!;

        [JsonPropertyName("opis")]
        public string? Opis { get; set; }
    }
}
