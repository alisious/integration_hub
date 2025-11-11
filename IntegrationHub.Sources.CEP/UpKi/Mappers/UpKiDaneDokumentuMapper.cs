using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationHub.Sources.CEP.UpKi.Contracts;
using UpKi.Reference;

namespace IntegrationHub.Sources.CEP.UpKi.Mappers
{
    public static class UpKiDaneDokumentuMapper
    {
        public static DaneDokumentuResponseDto ToDto(DaneDokumentuResponse? src)
        {
            if (src == null) return new DaneDokumentuResponseDto();

            return new DaneDokumentuResponseDto
            {
                DokumentUprawnieniaKierowcy = src.dokumentUprawnieniaKierowcy?.Select(Map).ToList(),
                Komunikat = src.komunikat,
                DataZapytania = src.dataZapytania == default ? null : src.dataZapytania
            };
        }

        // === poniżej prywatne mapowania pomocnicze ===

        private static DokumentUprawnieniaKierowcyDto Map(DokumentUprawnieniaKierowcy s) => new()
        {
            TypDokumentu = Map(s.typDokumentu),
            NumerDokumentu = s.numerDokumentu,
            SeriaNumerDokumentu = s.seriaNumerDokumentu,
            OrganWydajacyDokument = Map(s.organWydajacyDokument),
            DataWaznosci = s.dataWaznosci == default && !s.dataWaznosciSpecified ? null : s.dataWaznosci,
            DataWydania = s.dataWydania == default && !s.dataWydaniaSpecified ? null : s.dataWydania,
            ParametrOsobaId = Map(s.parametrOsobaId),
            StanDokumentu = Map(s.stanDokumentu),
            Ograniczenie = s.ograniczenie?.Select(Map).ToList(),
            DaneUprawnieniaKategorii = s.daneUprawnieniaKategorii?.Select(Map).ToList(),
            KomunikatyBiznesowe = Map(s.komunikatyBiznesowe)
        };

        private static DaneUprawnieniaKategoriiDto Map(DaneUprawnieniaKategorii s) => new()
        {
            Kategoria = Map(s.kategoria),
            DataWaznosci = s.dataWaznosci == default && !s.dataWaznosciSpecified ? null : s.dataWaznosci,
            DataWydania = s.dataWydania == default && !s.dataWydaniaSpecified ? null : s.dataWydania,
            DaneZakazuCofniecia = s.daneZakazuCofniecia?.Select(Map).ToList(),
            Ograniczenie = s.ograniczenie?.Select(Map).ToList()
        };

        private static DaneZakazuCofnieciaDto Map(DaneZakazuCofniecia s) => new()
        {
            TypZdarzenia = s.typZdarzenia,
            DataDo = s.dataDo == default && !s.dataDoSpecified ? null : s.dataDo
        };

        private static OgraniczenieDto Map(Ograniczenie s) => new()
        {
            KodOgraniczenia = s.kodOgraniczenia,
            WartoscOgraniczenia = s.wartoscOgraniczenia,
            OpisKodu = s.opisKodu,
            DataDo = s.dataDo == default && !s.dataDoSpecified ? null : s.dataDo
        };

        private static ParametrOsobaIdDto? Map(ParametrOsobaId? s)
        {
            if (s == null) return null;

            return new ParametrOsobaIdDto
            {
                OsobaId = s.osobaIdSpecified ? s.osobaId : null,
                WariantId = s.wariantIdSpecified ? s.wariantId : null,
                TokenKierowcy = s.tokenKierowcy,
                Idk = s.idk,
                DaneKierowcy = Map(s.daneKierowcy)
            };
        }

        private static DaneKierowcyDto? Map(DaneKierowcy? s)
        {
            if (s == null) return null;

            return new DaneKierowcyDto
            {
                NumerPesel = s.numerPesel,
                ImiePierwsze = s.imiePierwsze,
                Nazwisko = s.nazwisko,
                DataUrodzenia = s.dataUrodzenia == default && !s.dataUrodzeniaSpecified ? null : s.dataUrodzenia,
                MiejsceUrodzenia = s.miejsceUrodzenia,
                Adres = Map(s.adres)
            };
        }

        private static UpKiAdresDto? Map(Adres? s)
        {
            if (s == null) return null;

            return new UpKiAdresDto
            {
                Miejsce = Map(s.miejsce),
                NrLokalu = s.nrLokalu,
                MiejscowoscPodstawowa = Map(s.miejscowoscPodstawowa),
                Kraj = Map(s.kraj),
                Ulica = Map(s.ulica)
            };
        }

        private static MiejsceDto? Map(Miejsce? s)
        {
            if (s == null) return null;

            return new MiejsceDto
            {
                KodTERYT = s.kodTERYT,
                KodWojewodztwa = s.kodWojewodztwa,
                NazwaWojewodztwaStanu = s.nazwaWojewodztwaStanu,
                KodPowiatu = s.kodPowiatu,
                NazwaPowiatuDzielnicy = s.nazwaPowiatuDzielnicy,
                KodGminy = s.kodGminy,
                NazwaGminy = s.nazwaGminy,
                KodRodzajuGminy = s.kodRodzajuGminy,
                KodPocztowyKrajowy = s.kodPocztowyKrajowy,
                KodMiejscowosci = s.kodMiejscowosci,
                NazwaMiejscowosci = s.nazwaMiejscowosci
            };
        }

        private static MiejscowoscPodstawowaDto? Map(MiejscowoscPodstawowa? s)
        {
            if (s == null) return null;

            return new MiejscowoscPodstawowaDto
            {
                KodMiejscowosciPodstawowej = s.kodMiejscowosciPodstawowej,
                // Nazwa w referencji zaczyna się wielką literą – zachowujemy nazwę pola,
                // ale JsonPropertyName ustawiony w DTO wymusza "NazwaMiejscowosciPodstawowej".
                NazwaMiejscowosciPodstawowej = s.NazwaMiejscowosciPodstawowej
            };
        }

        private static UlicaDto? Map(Ulica? s)
        {
            if (s == null) return null;

            return new UlicaDto
            {
                CechaUlicy = Map(s.cechaUlicy),
                KodUlicy = s.kodUlicy,
                NazwaUlicy = s.nazwaUlicy,
                NazwaUlicyZDokumentu = s.nazwaUlicyZDokumentu,
                NrDomu = s.nrDomu
            };
        }

        private static UpKiWartoscSlownikowaDto? Map(WartoscSlownikowa? s)
        {
            if (s == null) return null;
            return new UpKiWartoscSlownikowaDto
            {
                Kod = s.kod,
                WartoscOpisowa = s.wartoscOpisowa
            };
        }

        private static KomunikatBiznesowyDto? Map(KomunikatBiznesowy? s)
        {
            if (s == null) return null;
            return new KomunikatBiznesowyDto
            {
                Kod = s.kod,
                Opis = s.opis
            };
        }
    }
}
