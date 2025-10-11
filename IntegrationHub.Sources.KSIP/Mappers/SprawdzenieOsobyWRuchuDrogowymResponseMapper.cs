using System;
using System.Linq;
using IntegrationHub.Sources.KSIP.Contracts;
using IntegrationHub.Sources.KSIP.SprawdzenieOsobyWRDService;

namespace IntegrationHub.Sources.KSIP.Mappers
{
    public static class SprawdzenieOsobyWRuchuDrogowymResponseMapper
    {
        public static SprawdzenieOsobyWRuchuDrogowymResponse Map(SprawdzenieOsobyResponse src)
        {
            var dto = new SprawdzenieOsobyWRuchuDrogowymResponse
            {
                State = src?.State
            };

            if (src?.Person is { } p)
            {
                dto.Person = new PersonDto
                {
                    FirstName = p.PersonName?.FirstName,
                    LastName = p.PersonName?.LastName,
                    PeselNumber = p.PESELNumber,
                    BirthDate = p.BirthDateSpecified ? p.BirthDate : null
                };
            }

            if (src?.OffenseRecord is { Length: > 0 })
            {
                dto.OffenseRecords = src.OffenseRecord.Select(o => new OffenseRecordDto
                {
                    IncidentDate = o.IncidentDateSpecified ? o.IncidentDate : null,
                    FinePaymentDate = o.FinePaymentDateSpecified ? o.FinePaymentDate : null,
                    ValidationOfDecisionDate = o.ValidationOfDecisonDateSpecified ? o.ValidationOfDecisonDate : null,
                    Classifications = (o.PersonCriminalRecordClassification ?? Array.Empty<PersonCriminalRecordClassificationType>())
                        .Select(c => new ClassificationDto
                        {
                            LegalClassificationCode = c.LegalClassificationCode,
                            ClassificationCode = c.ClassificationCode,
                            Description = c.Description
                        }).ToList()
                }).ToList();
            }

            return dto;
        }
    }
}
