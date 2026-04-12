using IntegrationHub.Sources.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.Sources.SRP.Contracts
{
    public class GetCurrentIdByPeselResponse
    {
        [JsonPropertyName("dowod")]
        public DowodOsobisty? Dowod { get; set; }
    }
}
