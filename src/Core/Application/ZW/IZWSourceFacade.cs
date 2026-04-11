using IntegrationHub.Domain.Contracts.ZW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Application.ZW
{
    public interface IZWSourceFacade
    {
        Task<IEnumerable<WPMResponse>> SearchAsync(WPMRequest req, CancellationToken ct = default);
        Task<int> CountVehiclesAsync(WPMRequest req, CancellationToken ct = default);

    }
}
