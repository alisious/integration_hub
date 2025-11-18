// IntegrationHub.Sources.CEP.UpKi/Mappers/DaneDokumentuResponseXmlMapper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IntegrationHub.Sources.CEP.UpKi.Contracts;

namespace IntegrationHub.Sources.CEP.UpKi.Mappers
{
    /// <summary>
    /// Mapper odpowiedzi SOAP (element DaneDokumentuResponse w soap:Body)
    /// na model <see cref="DaneDokumentuResponse"/>.
    ///
    /// Obsługuje tylko odpowiedzi poprawne (bez SOAP Fault).
    /// Jeżeli w kopercie znajduje się <Fault>/<cepikException> (np. kody UPKI-1000..UPKI-1030:
    /// problem z komunikacją, błąd walidacji, błąd słowników, brak danych),
    /// mapper rzuca <see cref="InvalidOperationException"/> z opisem błędu CEPIK.
    /// </summary>
    internal static class DaneDokumentuResponseXmlMapper
    {
        private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";

        /// <summary>
        /// Główny punkt wejścia – przyjmuje pełną kopertę SOAP i zwraca
        /// zmapowany model <see cref="DaneDokumentuResponse"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Gdy xml jest pusty.</exception>
        /// <exception cref="InvalidOperationException">
        /// Gdy wykryto cepikException (SOAP Fault) lub nie znaleziono DaneDokumentuResponse.
        /// </exception>
        public static DaneDokumentuResponse MapFromSoapEnvelope(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                throw new ArgumentException("SOAP XML cannot be null or empty.", nameof(soapXml));

            var document = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);

            var body = FindSoapBody(document)
                       ?? throw new InvalidOperationException("SOAP Body element not found in response.");

            // Najpierw sprawdzamy, czy to nie jest odpowiedź z błędem (SOAP Fault / cepikException).
            DetectAndThrowCepikException(body);

            var responseElement = body
                .Elements()
                .FirstOrDefault(e =>
                    string.Equals(e.Name.LocalName, "DaneDokumentuResponse", StringComparison.Ordinal));

            if (responseElement is null)
                throw new InvalidOperationException("Element DaneDokumentuResponse not found in SOAP Body.");

            return MapDaneDokumentuResponse(responseElement);
        }

        /// <summary>
        /// Mapowanie samego elementu upki:DaneDokumentuResponse (bez koperty SOAP).
        /// Przydatne, jeśli wyższa warstwa już „odkroiła” Body.
        /// </summary>
        public static DaneDokumentuResponse MapDaneDokumentuResponse(XElement daneDokumentuResponseElement)
        {
            if (daneDokumentuResponseElement is null)
                throw new ArgumentNullException(nameof(daneDokumentuResponseElement));

            var result = new DaneDokumentuResponse
            {
                DataZapytania = NormalizeDate(GetElementValue(daneDokumentuResponseElement, "dataZapytania")),
                Komunikat = GetElementValue(daneDokumentuResponseElement, "komunikat")
            };

            if (String.IsNullOrWhiteSpace(result.DataZapytania)) result.DataZapytania = DateTime.Today.ToString("yyyy-MM-dd");


            foreach (var docElement in GetElements(daneDokumentuResponseElement, "dokumentUprawnieniaKierowcy"))
            {
                var mapped = MapDokumentUprawnieniaKierowcy(docElement);
                if (mapped is not null)
                    result.DokumentyUprawnieniaKierowcy.Add(mapped);
            }

            return result;
        }

        #region Wykrywanie cepikException / SOAP Fault

        /// <summary>
        /// Sprawdza, czy w &lt;Body&gt; znajduje się SOAP Fault / cepikException.
        /// Jeżeli tak – buduje wiadomość z typ/kod/komunikat/szczegoly i rzuca InvalidOperationException.
        /// </summary>
        private static void DetectAndThrowCepikException(XElement body)
        {
            if (body is null)
                return;

            XElement? cepikExceptionRoot = null;

            // 1) typowy SOAP 1.1 Fault: Body -> Fault -> detail -> cepikException
            var fault = body.Elements()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Fault", StringComparison.Ordinal));

            if (fault != null)
            {
                var detail = GetElement(fault, "detail");
                if (detail != null)
                {
                    cepikExceptionRoot = detail
                        .Descendants()
                        .FirstOrDefault(e => string.Equals(e.Name.LocalName, "cepikException", StringComparison.Ordinal));
                }
            }

            // 2) fallback – gdyby cepikException był bezpośrednio w Body (na wszelki wypadek)
            if (cepikExceptionRoot == null)
            {
                cepikExceptionRoot = body
                    .Descendants()
                    .FirstOrDefault(e => string.Equals(e.Name.LocalName, "cepikException", StringComparison.Ordinal));
            }

            if (cepikExceptionRoot == null)
                return;

            // Struktura wg exceptions.xsd:
            // <cepikException>
            //   <komunikaty>
            //     <typ>ERROR|WARN</typ>
            //     <kod>UPKI-1030</kod>
            //     <komunikat>...</komunikat>
            //     <szczegoly>...</szczegoly>
            //   </komunikaty>
            // </cepikException>
            var komunikatyElement = cepikExceptionRoot
                                        .Elements()
                                        .FirstOrDefault(e => e.Name.LocalName == "komunikaty")
                                    ?? cepikExceptionRoot; // fallback

            var typ = GetElementValue(komunikatyElement, "typ");
            var kod = GetElementValue(komunikatyElement, "kod");
            var komunikat = GetElementValue(komunikatyElement, "komunikat");
            var szczegoly = GetElementValue(komunikatyElement, "szczegoly");

            var headerParts = new List<string>();
            if (!string.IsNullOrEmpty(typ)) headerParts.Add($"typ={typ}");
            if (!string.IsNullOrEmpty(kod)) headerParts.Add($"kod={kod}");

            var header = headerParts.Count > 0
                ? $"CEPIK exception ({string.Join(", ", headerParts)})"
                : "CEPIK exception";

            var detailParts = new List<string>();
            if (!string.IsNullOrEmpty(komunikat)) detailParts.Add(komunikat);
            if (!string.IsNullOrEmpty(szczegoly)) detailParts.Add(szczegoly);

            var fullMessage = detailParts.Count > 0
                ? $"{header}: {string.Join(" | ", detailParts)}"
                : header;

            throw new InvalidOperationException(fullMessage);
        }

        #endregion

        #region DokumentUprawnieniaKierowcy

        private static DokumentUprawnieniaKierowcy? MapDokumentUprawnieniaKierowcy(XElement element)
        {
            if (element is null)
                return null;

            var doc = new DokumentUprawnieniaKierowcy
            {
                DokumentId = GetElementValue(element, "dokumentId"),
                TypDokumentu = MapWartoscSlownikowa(GetElement(element, "typDokumentu")),
                NumerDokumentu = GetElementValue(element, "numerDokumentu"),
                SeriaNumerDokumentu = GetElementValue(element, "seriaNumerDokumentu"),
                OrganWydajacyDokument = MapWartoscSlownikowa(GetElement(element, "organWydajacyDokument")),
                DataWaznosci = NormalizeDate(GetElementValue(element, "dataWaznosci")),
                DataWydania = NormalizeDate(GetElementValue(element, "dataWydania")),
                ParametrOsobaId = MapParametrOsobaId(GetElement(element, "parametrOsobaId")),
                StanDokumentu = MapStanDokumentu(GetElement(element, "stanDokumentu")),
                KomunikatBiznesowy = MapKomunikatBiznesowy(GetElement(element, "komunikatyBiznesowy"))
            };

            foreach (var zakazElement in GetElements(element, "daneZakazuCofniecia"))
            {
                var zakaz = MapDaneZakazuCofniecia(zakazElement);
                if (zakaz is not null)
                    doc.DaneZakazuCofniecia.Add(zakaz);
            }

            foreach (var ograniczenieElement in GetElements(element, "ograniczenie"))
            {
                var ograniczenie = MapOgraniczenie(ograniczenieElement);
                if (ograniczenie is not null)
                    doc.Ograniczenia.Add(ograniczenie);
            }

            foreach (var katElement in GetElements(element, "daneUprawnieniaKategorii"))
            {
                var kat = MapDaneUprawnieniaKategorii(katElement);
                if (kat is not null)
                    doc.DaneUprawnieniaKategorii.Add(kat);
            }

            return doc;
        }

        #endregion

        #region ParametrOsobaId + DaneKierowcy + Adres

        private static ParametrOsobaId? MapParametrOsobaId(XElement? element)
        {
            if (element is null)
                return null;

            var result = new ParametrOsobaId
            {
                OsobaId = GetElementValue(element, "osobaId"),
                WariantId = GetElementValue(element, "wariantId"),
                TokenKierowcy = GetElementValue(element, "tokenKierowcy"),
                Idk = GetElementValue(element, "idk"),
                DaneKierowcy = MapDaneKierowcy(GetElement(element, "daneKierowcy"))
            };

            return result;
        }

        private static DaneKierowcy? MapDaneKierowcy(XElement? element)
        {
            if (element is null)
                return null;

            var result = new DaneKierowcy
            {
                NumerPesel = GetElementValue(element, "numerPesel"),
                ImiePierwsze = GetElementValue(element, "imiePierwsze"),
                Nazwisko = GetElementValue(element, "nazwisko"),
                DataUrodzenia = NormalizeDate(GetElementValue(element, "dataUrodzenia")),
                MiejsceUrodzenia = GetElementValue(element, "miejsceUrodzenia"),
                Adres = MapAdres(GetElement(element, "adres"))
            };

            return result;
        }

        private static Adres? MapAdres(XElement? element)
        {
            if (element is null)
                return null;

            var result = new Adres
            {
                Miejsce = MapMiejsce(GetElement(element, "miejsce")),
                NrLokalu = GetElementValue(element, "nrLokalu"),
                MiejscowoscPodstawowa = MapMiejscowoscPodstawowa(GetElement(element, "miejscowoscPodstawowa")),
                Kraj = MapWartoscSlownikowa(GetElement(element, "kraj")),
                Ulica = MapUlica(GetElement(element, "ulica"))
            };

            return result;
        }

        private static Miejsce? MapMiejsce(XElement? element)
        {
            if (element is null)
                return null;

            return new Miejsce
            {
                KodTeryt = GetElementValue(element, "kodTERYT"),
                KodWojewodztwa = GetElementValue(element, "kodWojewodztwa"),
                NazwaWojewodztwaStanu = GetElementValue(element, "nazwaWojewodztwaStanu"),
                KodPowiatu = GetElementValue(element, "kodPowiatu"),
                NazwaPowiatuDzielnicy = GetElementValue(element, "nazwaPowiatuDzielnicy"),
                KodGminy = GetElementValue(element, "kodGminy"),
                NazwaGminy = GetElementValue(element, "nazwaGminy"),
                KodMiejscowosci = GetElementValue(element, "kodMiejscowosci"),
                NazwaMiejscowosci = GetElementValue(element, "nazwaMiejscowosci"),
                KodPocztowyKrajowy = GetElementValue(element, "kodPocztowyKrajowy")
            };
        }

        private static MiejscowoscPodstawowa? MapMiejscowoscPodstawowa(XElement? element)
        {
            if (element is null)
                return null;

            return new MiejscowoscPodstawowa
            {
                KodMiejscowosciPodstawowej = GetElementValue(element, "kodMiejscowosciPodstawowej"),
                NazwaMiejscowosciPodstawowej = GetElementValue(element, "nazwaMiejscowosciPodstawowej")
            };
        }

        private static Ulica? MapUlica(XElement? element)
        {
            if (element is null)
                return null;

            return new Ulica
            {
                CechaUlicy = MapWartoscSlownikowa(GetElement(element, "cechaUlicy")),
                KodUlicy = GetElementValue(element, "kodUlicy"),
                NazwaUlicy = GetElementValue(element, "nazwaUlicy"),
                NazwaUlicyZDokumentu = GetElementValue(element, "nazwaUlicyZDokumentu"),
                NrDomu = GetElementValue(element, "nrDomu")
            };
        }

        #endregion

        #region StanDokumentu + zakazy + ograniczenia + kategorie

        private static StanDokumentu? MapStanDokumentu(XElement? element)
        {
            if (element is null)
                return null;

            var result = new StanDokumentu
            {
                Stan = MapWartoscSlownikowa(GetElement(element, "stanDokumentu")),
                DataZmianyStanu = NormalizeDate(GetElementValue(element, "dataZmianyStanu")),
                PodmiotZmianyStanu = MapWartoscSlownikowa(GetElement(element, "podmiotZmianyStanu"))
            };

            foreach (var powElement in GetElements(element, "powodZmianyStanu"))
            {
                var powod = MapWartoscSlownikowa(powElement);
                if (powod is not null)
                    result.PowodZmianyStanu.Add(powod);
            }

            return result;
        }

        private static DaneZakazuCofniecia? MapDaneZakazuCofniecia(XElement? element)
        {
            if (element is null)
                return null;

            return new DaneZakazuCofniecia
            {
                TypZdarzenia = GetElementValue(element, "typZdarzenia"),
                DataDo = NormalizeDate(GetElementValue(element, "dataDo"))
            };
        }

        private static Ograniczenie? MapOgraniczenie(XElement? element)
        {
            if (element is null)
                return null;

            return new Ograniczenie
            {
                KodOgraniczenia = GetElementValue(element, "kodOgraniczenia"),
                WartoscOgraniczenia = GetElementValue(element, "wartoscOgraniczenia"),
                OpisKodu = GetElementValue(element, "opisKodu"),
                DataDo = NormalizeDate(GetElementValue(element, "dataDo"))
            };
        }

        private static DaneUprawnieniaKategorii? MapDaneUprawnieniaKategorii(XElement? element)
        {
            if (element is null)
                return null;

            var result = new DaneUprawnieniaKategorii
            {
                Kategoria = MapWartoscSlownikowa(GetElement(element, "kategoria")),
                DataWaznosci = NormalizeDate(GetElementValue(element, "dataWaznosci")),
                DataWydania = NormalizeDate(GetElementValue(element, "dataWydania"))
            };

            foreach (var zakazElement in GetElements(element, "daneZakazuCofniecia"))
            {
                var zakaz = MapDaneZakazuCofniecia(zakazElement);
                if (zakaz is not null)
                    result.DaneZakazuCofniecia.Add(zakaz);
            }

            foreach (var ograniczenieElement in GetElements(element, "ograniczenie"))
            {
                var ograniczenie = MapOgraniczenie(ograniczenieElement);
                if (ograniczenie is not null)
                    result.Ograniczenia.Add(ograniczenie);
            }

            return result;
        }

        #endregion

        #region WartoscSlownikowa + KomunikatBiznesowy

        private static WartoscSlownikowa? MapWartoscSlownikowa(XElement? element)
        {
            if (element is null)
                return null;

            return new WartoscSlownikowa
            {
                Kod = GetElementValue(element, "kod"),
                WartoscOpisowa = GetElementValue(element, "wartoscOpisowa")
            };
        }

        private static KomunikatBiznesowy? MapKomunikatBiznesowy(XElement? element)
        {
            if (element is null)
                return null;

            return new KomunikatBiznesowy
            {
                Kod = GetElementValue(element, "kod"),
                Opis = GetElementValue(element, "opis")
            };
        }

        #endregion

        #region Helpers

        private static XElement? FindSoapBody(XDocument document)
        {
            if (document.Root is null)
                return null;

            XNamespace soapNs = SoapEnvelopeNamespace;

            var body = document
                .Descendants()
                .FirstOrDefault(e =>
                    (e.Name.Namespace == soapNs && e.Name.LocalName == "Body") ||
                    e.Name.LocalName == "Body");

            return body;
        }

        private static XElement? GetElement(XElement parent, string localName)
        {
            return parent
                .Elements()
                .FirstOrDefault(e =>
                    string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal));
        }

        private static IEnumerable<XElement> GetElements(XElement parent, string localName)
        {
            return parent
                .Elements()
                .Where(e =>
                    string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal));
        }

        private static string? GetElementValue(XElement? parent, string localName)
        {
            if (parent is null) return null;

            var el = GetElement(parent, localName);
            if (el is null)
                return null;

            var value = (string?)el;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Normalizuje wartość xsd:date do formatu "yyyy-MM-dd".
        /// XSD dopuszcza strefę czasową (np. "1973-02-09+01:00") – w takim
        /// przypadku ucinamy wszystko poza częścią daty.
        /// </summary>
        private static string? NormalizeDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim();
            return raw.Length >= 10 ? raw[..10] : raw;
        }

        #endregion
    }
}
