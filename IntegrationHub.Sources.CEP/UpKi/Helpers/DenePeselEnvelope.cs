using System;
using System.Text;
using IntegrationHub.Sources.CEP.UpKi.Contracts;

namespace IntegrationHub.Sources.CEP.UpKi.Helpers
{
    public static class DenePeselEnvelope
    {
        public static string Create(DaneDokumentuRequestDto body)
        {
            var sb = new StringBuilder(512);
            EnvelopeXml.BeginEnvelope(sb);

            if (body.DanePesel != null)
            {
                string dataZapytaniaFormatted = null;
                if (body.DanePesel.DataZapytania is DateTime dtPesel)
                {
                    dataZapytaniaFormatted = dtPesel.ToString("yyyy-MM-dd");
                }
                else if (body.DanePesel.DataZapytania != null)
                {
                    dataZapytaniaFormatted = body.DanePesel.DataZapytania.ToString();
                }

                EnvelopeXml.AppendDanePesel(sb, body.DanePesel.NumerPesel, dataZapytaniaFormatted);
            }
            else if (body.DaneOsoby != null)
            {
                string dataZapytaniaFormatted = null;
                if (body.DaneOsoby.DataZapytania is DateTime dtOsoby)
                {
                    dataZapytaniaFormatted = dtOsoby.ToString("yyyy-MM-dd");
                }
                else if (body.DaneOsoby.DataZapytania != null)
                {
                    dataZapytaniaFormatted = body.DaneOsoby.DataZapytania.ToString();
                }

                string dataUrodzeniaFormatted = null;
                if (body.DaneOsoby.DataUrodzenia is DateOnly duOsoby)
                {
                    dataUrodzeniaFormatted = duOsoby.ToString("yyyy-MM-dd");
                }
                else if (body.DaneOsoby.DataUrodzenia != null)
                {
                    dataUrodzeniaFormatted = body.DaneOsoby.DataUrodzenia.ToString();
                }

                EnvelopeXml.AppendDaneOsoby(sb, body.DaneOsoby.ImiePierwsze, body.DaneOsoby.Nazwisko, dataUrodzeniaFormatted, dataZapytaniaFormatted);
            }

            EnvelopeXml.EndEnvelope(sb);
            return sb.ToString();
        }
    }
}
