using IntegrationHub.Sources.ANPRS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Application.ANPRS
{
    public interface IANPRSSourceFacade
    {
        Task<IEnumerable<VehiclePhotoRowDto>> GetPhotosAsync(Guid id, CancellationToken ct = default);
        Task<(IReadOnlyList<VehiclePhotoRowDto> Photos, string PhotosComplete)> GetPhotosWithMetaAsync(Guid id, CancellationToken ct = default);
    }
}
