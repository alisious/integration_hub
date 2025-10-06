using System;
using System.Collections.Generic;

namespace IntegrationHub.Sources.CEP.Contracts
{
    /// <summary>
    /// Odpowiedź dla operacji pytanieOPojazd: nagłówek (meta) + lista pojazdów.
    /// </summary>
    public sealed class PytanieOPojazdResultDto
    {
        public PytanieMetaDto Meta { get; set; } = new();
        public List<PojazdDto> Pojazdy { get; set; } = new();
    }

    /// <summary>
    /// Meta-informacje o odpowiedzi (daneRezultatu) – nazwy po polsku.
    /// </summary>
    public sealed class PytanieMetaDto
    {
        /// <summary>Identyfikator transakcji zwrócony przez CEP.</summary>
        public string? IdTransakcji { get; set; }

        /// <summary>Ilość zwróconych rekordów (pojazdów).</summary>
        public int? LiczbaZwroconychRekordow { get; set; }

        /// <summary>Znacznik czasowy odpowiedzi.</summary>
        public DateTime? ZnacznikCzasowy { get; set; }

        /// <summary>Identyfikator użytkownika systemu zewnętrznego.</summary>
        public string? IdUzytkownikaZewnetrznego { get; set; }

        /// <summary>Identyfikator systemu zewnętrznego.</summary>
        public string? IdSystemuZewnetrznego { get; set; }

        /// <summary>Znak sprawy przekazany w zapytaniu.</summary>
        public string? ZnakSprawy { get; set; }

        /// <summary>Wnioskodawca przekazany w zapytaniu.</summary>
        public string? Wnioskodawca { get; set; }
    }

    /// <summary>
    /// Pojedynczy pojazd z odpowiedzi.
    /// </summary>
    public sealed class PojazdDto
    {
        /// <summary>Aktualny identyfikator pojazdu (ID pojazdu i ID stanu rejestracji).</summary>
        public AktualnyIdentyfikatorPojazduDto? AktualnyIdentyfikator { get; set; }

        /// <summary>Stan pojazdu w formie skrótowej/opisowej (np. kod DICT017_Z).</summary>
        public string? StanPojazdu { get; set; }

        /// <summary>Dane opisowe pojazdu (VIN, marka/model, parametry itp.).</summary>
        public DaneOpisowePojazduDto? DaneOpisowe { get; set; }

        /// <summary>Informacje o rejestracji (nr rej., data, organ, typ rejestracji).</summary>
        public RejestracjaPojazduDto? Rejestracja { get; set; }

        /// <summary>Dokument pojazdu (np. DR) – jeśli obecny.</summary>
        public DokumentPojazduDto? Dokument { get; set; }
    }

    public sealed class AktualnyIdentyfikatorPojazduDto
    {
        /// <summary>Identyfikator systemowy pojazdu.</summary>
        public string? IdPojazdu { get; set; }

        /// <summary>Identyfikator systemowy stanu rejestracji pojazdu.</summary>
        public string? IdStanuRejestracji { get; set; }
    }

    /// <summary>
    /// Uproszczone dane opisowe pojazdu (na podstawie bloku DaneOpisujacePojazdUdoUproszczone).
    /// </summary>
    public sealed class DaneOpisowePojazduDto
    {
        public string? VIN { get; set; }
        public SlownikDto? Marka { get; set; }
        public string? Model { get; set; }

        public SlownikDto? Rodzaj { get; set; }
        public SlownikDto? Podrodzaj { get; set; }
        public SlownikDto? Przeznaczenie { get; set; }

        public int? RokProdukcji { get; set; }

        public SlownikDto? Paliwo { get; set; }

        public int? PojemnoscSkokowa { get; set; }
        public int? MocNetto { get; set; }
        public int? MasaWlasna { get; set; }
        public int? DopuszczalnaMasaCalkowita { get; set; }
        public int? LiczbaMiejscSiedzacych { get; set; }
        public int? LiczbaMiejscStojacych { get; set; }

        /// <summary>Kod RPP (rodzaj/podrodzaj/przeznaczenie) jako wartość słownikowa.</summary>
        public SlownikDto? KodRPP { get; set; }
    }

    /// <summary>
    /// Dane rejestracyjne pojazdu.
    /// </summary>
    public sealed class RejestracjaPojazduDto
    {
        public string? NumerRejestracyjny { get; set; }
        public DateTime? DataRejestracji { get; set; }

        /// <summary>Organ rejestrujący (kod/nazwa/status/dataAktualizacji).</summary>
        public OrganDto? OrganRejestrujacy { get; set; }

        /// <summary>Typ rejestracji (słownik – kod i opisy).</summary>
        public SlownikDto? TypRejestracji { get; set; }
    }

    /// <summary>
    /// Dokument pojazdu (np. dowód rejestracyjny).
    /// </summary>
    public sealed class DokumentPojazduDto
    {
        public string? IdSystemowy { get; set; }
        public SlownikDto? TypDokumentu { get; set; }
        public string? SeriaNumer { get; set; }
        public bool? CzyWtornik { get; set; }
        public DateTime? DataWydania { get; set; }

        /// <summary>Organ wydający dokument. Zawiera rozszerzone pola (REGON, identyfikatory) w Dodatkowe.</summary>
        public OrganDto? OrganWydajacy { get; set; }
    }

    /// <summary>
    /// Struktura dla wartości słownikowych (kod + opisy + metadane).
    /// </summary>
    public sealed class SlownikDto
    {
        public string? Kod { get; set; }
        public string? Opis { get; set; }
        public string? OpisSkrocony { get; set; }
        public DateTime? DataAktualizacji { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// Dane organizacyjne (organ rejestrujący / organ wydający dokument).
    /// </summary>
    public sealed class OrganDto
    {
        public string? Kod { get; set; }
        public string? Nazwa { get; set; }
        public string? Status { get; set; }
        public DateTime? DataAktualizacji { get; set; }

        /// <summary>Dodatkowe pola identyfikacyjne organu (głównie dla organu wydającego dokument).</summary>
        public OrganExtraDto? Dodatkowe { get; set; }
    }

    /// <summary>
    /// Dodatkowe identyfikatory organu.
    /// </summary>
    public sealed class OrganExtraDto
    {
        public string? NumerEwidencyjny { get; set; }
        public string? Regon { get; set; }
        public string? IdRegon { get; set; }
    }
}
