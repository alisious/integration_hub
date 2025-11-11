using IntegrationHub.Common.Primitives;
using IntegrationHub.Sources.CEP.UpKi.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.CEP.UpKi.Services
{
    public interface IUpKiService
    {
        Task<Result<DaneDokumentuResponseDto, Error>> GetDriverPermissionsAsync(DaneDokumentuRequestDto body, CancellationToken ct = default);

    }
}
