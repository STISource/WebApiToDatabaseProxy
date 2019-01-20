using System;
using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class Warenausgang
    {
        [DataMember(Order = 0)]
        public string ArtNr { get; set; }

        [DataMember(Order = 1)]
        public string Bez { get; set; }

        [DataMember(Order = 2)]
        public string Lagerort { get; set; }

        [DataMember(Order = 3)]
        public double? LagerBestand { get; set; }

        [DataMember(Order = 4)]
        public double? BestandCharge { get; set; }

        [DataMember(Order = 5)]
        public string Charge { get; set; }

        [DataMember(Order = 6)]
        public string LetzteRechnung { get; set; }

        [DataMember(Order = 7)]
        public string ErsterWareneingang { get; set; }

        [DataMember(Order = 8)]
        public string HauptartikelDerStueckliste { get; set; }
         
      
    }
}