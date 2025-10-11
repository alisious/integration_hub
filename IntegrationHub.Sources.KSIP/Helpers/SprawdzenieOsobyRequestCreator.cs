using System;
using IntegrationHub.Sources.KSIP.Contracts;
using IntegrationHub.Sources.KSIP.SprawdzenieOsobyWRDService;

namespace IntegrationHub.Sources.KSIP.Helpers
{
    public static class SprawdzenieOsobyRequestCreator
    {
        /// <summary>
        /// Buduje obiekt proxy WCF do wywołania WRD.
        /// Stałe: SystemName="ŻW", ApplicationName="ŻW", ModuleName="ZW-KSIP", TerminalName="ZW-KSIP".
        /// SystemType = SystemType.ŻW
        /// </summary>
        public static SprawdzenieOsobyRequest Create(
            SprawdzenieOsobyWRuchuDrogowymRequest src,
            string requestId,
            string userId,
            string unitId)
        {
            if (src is null) throw new ArgumentNullException(nameof(src));
            if (string.IsNullOrWhiteSpace(requestId)) requestId = Guid.NewGuid().ToString("N");

            var header = new SprawdzenieOsobyRequestRequestHeader
            {
                RequestID = requestId,
                Timestamp = DateTime.UtcNow,
                AuditRecord = new SprawdzenieOsobyRequestRequestHeaderAuditRecord
                {
                    SystemName = SystemType.ŻW,
                    ApplicationName = "ŻW",
                    ModuleName = "ZW-KSIP",
                    TerminalName = "ZW-KSIP",
                    UserProfile = new SprawdzenieOsobyRequestRequestHeaderAuditRecordUserProfile
                    {
                        UserID = userId,
                        UnitID = unitId
                    }
                }
            };

            object bodyItem;
            if (!string.IsNullOrWhiteSpace(src.NrPesel))
            {
                // RequestBody: <NrPESEL>...</NrPESEL>
                bodyItem = src.NrPesel!;
            }
            else
            {
                // RequestBody: <Person>...</Person>
                bodyItem = new SprawdzenieOsobyRequestRequestBodyPerson
                {
                    PersonName = new SprawdzenieOsobyRequestRequestBodyPersonPersonName
                    {
                        FirstName = src.FirstName!,
                        LastName = src.LastName!
                    },
                    BirthDate = DateTime.Parse(src.BirthDate!) // assuming valid "yyyy-MM-dd" format 
                    //DateTime.SpecifyKind(
                    //    DateTime.ParseExact(src.BirthDate!, "yyyy-MM-dd", null),
                    //    DateTimeKind.Utc)
                };
            }

            return new SprawdzenieOsobyRequest
            {
                RequestHeader = header,
                RequestBody = new SprawdzenieOsobyRequestRequestBody { Item = bodyItem }
            };
        }
    }
}
