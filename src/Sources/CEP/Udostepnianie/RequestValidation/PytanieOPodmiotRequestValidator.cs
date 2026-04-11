// IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation/PytanieOPodmiotRequestValidator.cs
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.RequestValidation
{
    internal sealed class PytanieOPodmiotRequestValidator : IRequestValidator<PytanieOPodmiotRequest>
    {
        public ValidationResult ValidateAndNormalize(PytanieOPodmiotRequest body)
        {
            // Trim – ID
            body.IdentyfikatorSystemowyPodmiotu = T(body.IdentyfikatorSystemowyPodmiotu);

            // Trim – gniazdo OSOBA
            if (body.ParametryOsoby is not null)
            {
                body.ParametryOsoby.ImiePierwsze = T(body.ParametryOsoby.ImiePierwsze);
                body.ParametryOsoby.Nazwisko = T(body.ParametryOsoby.Nazwisko);
                body.ParametryOsoby.MiejsceUrodzenia = T(body.ParametryOsoby.MiejsceUrodzenia);
                body.ParametryOsoby.MiejsceUrodzeniaKod = T(body.ParametryOsoby.MiejsceUrodzeniaKod);
                body.ParametryOsoby.PESEL = T(body.ParametryOsoby.PESEL);
                body.ParametryOsoby.NazwaDokumentu = T(body.ParametryOsoby.NazwaDokumentu);
                body.ParametryOsoby.SeriaNumerDokumentu = T(body.ParametryOsoby.SeriaNumerDokumentu);
            }

            // Trim – gniazdo FIRMA
            if (body.ParametryFirmy is not null)
            {
                body.ParametryFirmy.REGON = T(body.ParametryFirmy.REGON);
                body.ParametryFirmy.NazwaFirmyDrukowana = T(body.ParametryFirmy.NazwaFirmyDrukowana);
                body.ParametryFirmy.IdentyfikatorSystemowyREGON = T(body.ParametryFirmy.IdentyfikatorSystemowyREGON);
                body.ParametryFirmy.ZagranicznyNumerIdentyfikacyjny = T(body.ParametryFirmy.ZagranicznyNumerIdentyfikacyjny);
                body.ParametryFirmy.NazwaWojewodztwaStanu = T(body.ParametryFirmy.NazwaWojewodztwaStanu);
                body.ParametryFirmy.NazwaMiejscowosci = T(body.ParametryFirmy.NazwaMiejscowosci);
                body.ParametryFirmy.KodPocztowy = T(body.ParametryFirmy.KodPocztowy);
                body.ParametryFirmy.NazwaUlicy = T(body.ParametryFirmy.NazwaUlicy);
                body.ParametryFirmy.NumerDomu = T(body.ParametryFirmy.NumerDomu);
            }

            // 0) Nowa zasada „wejściowa”: ID albo ParametryOsoby albo ParametryFirmy
            if (!HasAnyTopLevelCriteria(body))
                return ValidationResult.Fail("Podaj: identyfikatorSystemowyPodmiotu albo parametryOsoby albo parametryFirmy.");

            // 1) Jeżeli jest identyfikator – OK
            if (!string.IsNullOrEmpty(body.IdentyfikatorSystemowyPodmiotu))
                return ValidationResult.Ok();

            // 2) Minimalne kryteria – identyczne, ale tylko w gniazdach
            bool firmaOk = IsFirmaOk(body.ParametryFirmy);
            bool osobaOk = IsOsobaOk(body.ParametryOsoby);

            if (firmaOk || osobaOk)
                return ValidationResult.Ok();

            // 3) Komunikat jak wcześniej (zestawy bez zmian)
            return ValidationResult.Fail(
                "Minimalne kryteria zapytania nie zostały spełnione. " +
                "Dla firmy: identyfikatorSystemowyPodmiotu lub REGON lub nazwaFirmyDrukowana lub identyfikatorSystemowyREGON lub " +
                "zagranicznyNumerIdentyfikacyjny lub (nazwa województwa, miejscowość, kod pocztowy i nr domu) " +
                "lub (nazwa województwa, miejscowość, nazwa ulicy i nr domu). " +
                "Dla osoby: identyfikatorSystemowyPodmiotu lub (imiePierwsze, miejsceUrodzenia i nazwisko) " +
                "lub (imiePierwsze, miejsceUrodzeniaKod i nazwisko) lub PESEL lub (nazwaDokumentu i seriaNumerDokumentu).");
        }

        // --- helpers ---
        private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        private static bool All(params string?[] xs) => xs.All(x => !string.IsNullOrWhiteSpace(x));

        private static bool HasAnyTopLevelCriteria(PytanieOPodmiotRequest b)
            => !string.IsNullOrEmpty(b.IdentyfikatorSystemowyPodmiotu)
               || b.ParametryOsoby is not null
               || b.ParametryFirmy is not null;

        private static bool IsFirmaOk(ParametryFirmyDto? f) =>
               f is not null && (
                    !string.IsNullOrEmpty(f.REGON)
                 || !string.IsNullOrEmpty(f.NazwaFirmyDrukowana)
                 || !string.IsNullOrEmpty(f.IdentyfikatorSystemowyREGON)
                 || !string.IsNullOrEmpty(f.ZagranicznyNumerIdentyfikacyjny)
                 || All(f.NazwaWojewodztwaStanu, f.NazwaMiejscowosci, f.KodPocztowy, f.NumerDomu)
                 || All(f.NazwaWojewodztwaStanu, f.NazwaMiejscowosci, f.NazwaUlicy, f.NumerDomu)
               );

        private static bool IsOsobaOk(ParametryOsobyDto? o) =>
               o is not null && (
                    All(o.ImiePierwsze, o.MiejsceUrodzenia, o.Nazwisko)
                 || All(o.ImiePierwsze, o.MiejsceUrodzeniaKod, o.Nazwisko)
                 || !string.IsNullOrEmpty(o.PESEL)
                 || All(o.NazwaDokumentu, o.SeriaNumerDokumentu)
               );
    }
}
