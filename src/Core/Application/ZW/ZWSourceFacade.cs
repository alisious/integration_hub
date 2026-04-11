using IntegrationHub.Domain.Contracts.ZW;
using IntegrationHub.Domain.Interfaces.ZW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Application.ZW
{
    public class ZWSourceFacade : IZWSourceFacade
    {
        private readonly IZWWPMRepository _repo;

        public ZWSourceFacade(IZWWPMRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> CountVehiclesAsync(WPMRequest req, CancellationToken ct = default)
        {
            return await _repo.CountVehiclesAsync(req, ct);
        }

        public async Task<IEnumerable<WPMResponse>> SearchAsync(WPMRequest req, CancellationToken ct = default)
        {
            return await _repo.SearchAsync(req, ct);
        }   
    }
}
