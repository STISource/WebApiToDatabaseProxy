using System;
using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class Stueckliste
    {
        [DataMember(Order = 0)]
        public string HauptArtikel { get; set; }

        [DataMember(Order = 1)]
        public string UnterartikelNr { get; set; }

        [DataMember(Order = 2)]
        public double? LagerBestand { get; set; }

    }
}