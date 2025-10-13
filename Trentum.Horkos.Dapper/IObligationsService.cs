using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Trentum.Horkos;

public interface IObligationsService
{
    Task<int> ImportAnnualList(Stream csvStream, int horkosListId, int rok, CancellationToken ct = default);
    Task<int> ImportDischargedList(Stream csvStream, int horkosListId, int rok, string miesiac, CancellationToken ct = default);
}
