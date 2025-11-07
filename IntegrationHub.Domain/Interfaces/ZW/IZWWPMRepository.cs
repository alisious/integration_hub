using IntegrationHub.Domain.Contracts.ZW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Domain.Interfaces.ZW
{
    public interface IZWWPMRepository
    {
        /// <summary>
        /// Wyszukanie pojazdów po którymkolwiek z pól: NrRejestracyjny / NumerPodwozia / NrSerProducenta / NrSerSilnika.
        /// Zwraca listę dopasowań (może być pusta). Wymagane jest podanie co najmniej jednego kryterium.
        /// </summary>
        Task<IEnumerable<WPMResponse>> SearchAsync(WPMRequest request, CancellationToken ct = default);
        Task<int> CountVehiclesAsync(WPMRequest request, CancellationToken ct = default);
    }
}
