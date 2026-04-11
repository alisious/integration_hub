// IntegrationHub.Sources.CEP.Udostepnianie.Helpers/PytanieOPodmiotEnvelope.cs
using System.Text;
using IntegrationHub.Sources.CEP.Udostepnianie.Contracts;

namespace IntegrationHub.Sources.CEP.Udostepnianie.Helpers
{
    public static class PytanieOPodmiotEnvelope
    {
        public static string Create(PytanieOPodmiotRequest body, string requestId)
        {
            var sb = new StringBuilder(1024);

            EnvelopeXml.BeginEnvelope(sb, "pytanieOPodmiot");
            EnvelopeXml.AppendParametryPytania(sb, requestId);

            sb.Append("<parametryPodmiotu>");

            // --- <parametryPodmiotu> (gniazdo z identyfikatorem) ---
            if (!string.IsNullOrWhiteSpace(body.IdentyfikatorSystemowyPodmiotu))
            {
                sb.Append("<parametryPodmiotu>");
                sb.Append($"<identyfikatorSystemowyPodmiotu>{EnvelopeXml.X(body.IdentyfikatorSystemowyPodmiotu)}</identyfikatorSystemowyPodmiotu>");
                sb.Append("</parametryPodmiotu>");
            }

            // --- <parametryOsoby> (zgodnie z TEMPLATE: elementy płaskie + zagnieżdżenia) ---
            if (body.ParametryOsoby is not null)
            {
                var o = body.ParametryOsoby;
                sb.Append("<parametryOsoby>");

                EnvelopeXml.AppendIfValue(sb, "PESEL", o.PESEL);
                EnvelopeXml.AppendIfValue(sb, "imiePierwsze", o.ImiePierwsze);
                EnvelopeXml.AppendIfValue(sb, "nazwisko", o.Nazwisko);
                EnvelopeXml.AppendIfValue(sb, "dataUrodzenia", o.DataUrodzenia);
                EnvelopeXml.AppendIfValue(sb, "miejsceUrodzenia", o.MiejsceUrodzenia);

                // miejsceUrodzeniaKod/kod
                if (!string.IsNullOrWhiteSpace(o.MiejsceUrodzeniaKod))
                {
                    sb.Append("<miejsceUrodzeniaKod>");
                    sb.Append($"<kod>{EnvelopeXml.X(o.MiejsceUrodzeniaKod)}</kod>");
                    sb.Append("</miejsceUrodzeniaKod>");
                }

                // parametryAdresu
                AppendParametryAdresu(
                    sb,
                    o.NazwaWojewodztwaStanu, o.NazwaPowiatuDzielnicy, o.NazwaGminy,
                    o.NazwaMiejscowosci, o.NazwaUlicy, o.NumerDomu, o.KodPocztowy
                );

                // parametryZagranicznegoDokumentuPotwierdzajacegoTozsamosc
                if (!string.IsNullOrWhiteSpace(o.SeriaNumerDokumentu) ||
                    !string.IsNullOrWhiteSpace(o.NazwaDokumentu) ||
                    !string.IsNullOrWhiteSpace(o.KodPanstwaWydajacegoDokument))
                {
                    sb.Append("<parametryZagranicznegoDokumentuPotwierdzajacegoTozsamosc>");
                    EnvelopeXml.AppendIfValue(sb, "seriaNumerDokumentu", o.SeriaNumerDokumentu);
                    EnvelopeXml.AppendIfValue(sb, "nazwaDokumentu", o.NazwaDokumentu);
                    if (!string.IsNullOrWhiteSpace(o.KodPanstwaWydajacegoDokument))
                    {
                        sb.Append("<nazwaPanstwaWydajacegoDokument>");
                        sb.Append($"<kod>{EnvelopeXml.X(o.KodPanstwaWydajacegoDokument)}</kod>");
                        sb.Append("</nazwaPanstwaWydajacegoDokument>");
                    }
                    sb.Append("</parametryZagranicznegoDokumentuPotwierdzajacegoTozsamosc>");
                }

                sb.Append("</parametryOsoby>");
            }

            // --- <parametryFirmy> (zgodnie z TEMPLATE: płaskie + zagnieżdżenia) ---
            if (body.ParametryFirmy is not null)
            {
                var f = body.ParametryFirmy;
                sb.Append("<parametryFirmy>");

                EnvelopeXml.AppendIfValue(sb, "REGON", f.REGON);
                EnvelopeXml.AppendIfValue(sb, "nazwaFirmyDrukowana", f.NazwaFirmyDrukowana);
                EnvelopeXml.AppendIfValue(sb, "identyfikatorSystemowyREGON", f.IdentyfikatorSystemowyREGON);

                // parametryFirmyZagranicznej / { krajZagranicznejWlasnosci/kod, zagranicznyNumerIdentyfikacyjny }
                if (!string.IsNullOrWhiteSpace(f.KodKrajuZagranicznejWlasnosci) ||
                    !string.IsNullOrWhiteSpace(f.ZagranicznyNumerIdentyfikacyjny))
                {
                    sb.Append("<parametryFirmyZagranicznej>");
                    if (!string.IsNullOrWhiteSpace(f.KodKrajuZagranicznejWlasnosci))
                    {
                        sb.Append("<krajZagranicznejWlasnosci>");
                        sb.Append($"<kod>{EnvelopeXml.X(f.KodKrajuZagranicznejWlasnosci!)}</kod>");
                        sb.Append("</krajZagranicznejWlasnosci>");
                    }
                    EnvelopeXml.AppendIfValue(sb, "zagranicznyNumerIdentyfikacyjny", f.ZagranicznyNumerIdentyfikacyjny);
                    sb.Append("</parametryFirmyZagranicznej>");
                }

                // parametryAdresu
                AppendParametryAdresu(
                    sb,
                    f.NazwaWojewodztwaStanu, 
                    f.NazwaPowiatuDzielnicy, 
                    f.NazwaGminy,
                    f.NazwaMiejscowosci, 
                    f.NazwaUlicy, 
                    f.NumerDomu, 
                    f.KodPocztowy
                );

                sb.Append("</parametryFirmy>");
            }

            sb.Append("</parametryPodmiotu>");

            EnvelopeXml.EndEnvelope(sb, "pytanieOPodmiot");
            return sb.ToString();
        }

        // ===== helpers =====
       

        private static void AppendParametryAdresu(
            StringBuilder sb,
            string? woj, string? powiat, string? gmina,
            string? miejsc, string? ulica, string? nrDomu, string? kodPoczt)
        {
            if (string.IsNullOrWhiteSpace(woj) && string.IsNullOrWhiteSpace(powiat) &&
                string.IsNullOrWhiteSpace(gmina) && string.IsNullOrWhiteSpace(miejsc) &&
                string.IsNullOrWhiteSpace(ulica) && string.IsNullOrWhiteSpace(nrDomu) &&
                string.IsNullOrWhiteSpace(kodPoczt))
                return;

            sb.Append("<parametryAdresu>");
            EnvelopeXml.AppendIfValue(sb, "nazwaWojewodztwaStanu", woj);
            EnvelopeXml.AppendIfValue(sb, "nazwaPowiatuDzielnicy", powiat);
            EnvelopeXml.AppendIfValue(sb, "nazwaGminy", gmina);
            EnvelopeXml.AppendIfValue(sb, "nazwaMiejscowosci", miejsc);
            EnvelopeXml.AppendIfValue(sb, "nazwaUlicy", ulica);
            EnvelopeXml.AppendIfValue(sb, "numerDomu", nrDomu);
            EnvelopeXml.AppendIfValue(sb, "kodPocztowy", kodPoczt);
            sb.Append("</parametryAdresu>");
        }
    }
}
