using IntegrationHub.Sources.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.SRP.Contracts
{
    public class GetPersonByPeselResponse
    {
        public Osoba? daneOsoby { get; set; }
    }
}
