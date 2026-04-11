using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Helpers
{
    public static class EnvelopeXmlHelper
    {
        public static string X(string? value) =>
            SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;

        public static void AppendIfValue(StringBuilder sb, string tag, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            sb.Append('<').Append(tag).Append('>')
              .Append(X(value))
              .Append("</").Append(tag).Append('>');
        }
    }
}
