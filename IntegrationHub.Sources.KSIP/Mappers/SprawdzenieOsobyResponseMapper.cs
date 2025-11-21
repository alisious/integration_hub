using IntegrationHub.Sources.KSIP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IntegrationHub.Sources.KSIP.Mappers
{
    public static class SprawdzenieOsobyResponseMapper
    {
        public static SprawdzenieOsobyResponse MapFromSoapEnvelope(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("XML odpowiedzi nie może być pusty.", nameof(xml));

            var doc = XDocument.Parse(xml);

            var responseEl = doc
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "SprawdzenieOsobyResponse");

            if (responseEl == null)
            {
                throw new InvalidOperationException(
                    "Nie znaleziono elementu SprawdzenieOsobyResponse w odpowiedzi SOAP.");
            }

            var result = new SprawdzenieOsobyResponse();

            // State
            var stateEl = responseEl
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName == "State");

            if (stateEl != null && int.TryParse(stateEl.Value.Trim(), out var state))
            {
                result.State = state;
            }

            // Person
            var personEl = responseEl
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName == "Person");

            if (personEl != null)
            {
                var person = new SprawdzenieOsobyPerson();

                var personNameEl = personEl
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "PersonName");

                if (personNameEl != null)
                {
                    person.FirstName = personNameEl
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "FirstName")
                        ?.Value
                        .Trim();

                    person.LastName = personNameEl
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "LastName")
                        ?.Value
                        .Trim();
                }

                person.PeselNumber = personEl
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "PESELNumber")
                    ?.Value
                    .Trim();

                person.BirthDate = personEl
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "BirthDate")
                    ?.Value
                    .Trim();

                result.Person = person;
            }

            // OffenseRecord (0..n)
            var offenseEls = responseEl
                .Elements()
                .Where(e => e.Name.LocalName == "OffenseRecord");

            var offenseList = new List<SprawdzenieOsobyOffenseRecord>();

            foreach (var offEl in offenseEls)
            {
                var off = new SprawdzenieOsobyOffenseRecord
                {
                    IncidentDate = offEl
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "IncidentDate")
                        ?.Value
                        .Trim(),

                    FinePaymentDate = offEl
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "FinePaymentDate")
                        ?.Value
                        .Trim(),

                    ValidationOfDecisionDate = offEl
                        .Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "ValidationOfDecisonDate")
                        ?.Value
                        .Trim()
                };

                var classEl = offEl
                    .Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "PersonCriminalRecordClassification");

                if (classEl != null)
                {
                    var c = new SprawdzenieOsobyPersonCriminalRecordClassification
                    {
                        LegalClassificationCode = classEl
                            .Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "LegalClassificationCode")
                            ?.Value
                            .Trim(),

                        ClassificationCode = classEl
                            .Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "ClassificationCode")
                            ?.Value
                            .Trim(),

                        Description = classEl
                            .Elements()
                            .FirstOrDefault(e => e.Name.LocalName == "Description")
                            ?.Value
                            .Trim()
                    };

                    off.Classification = c;
                }

                offenseList.Add(off);
            }

            if (offenseList.Count > 0)
                result.OffenseRecords = offenseList;

            return result;
        }
    }
}
