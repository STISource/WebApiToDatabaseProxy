﻿using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class KundenUmsatz
    {
        [DataMember(Order = 0)]
        public string KundenNr { get; set; }

        [DataMember(Order = 1)]
        public string Kunde { get; set; }

        [DataMember(Order = 2)]
        public double? VK_Netto_EUR { get; set; }

        [DataMember(Order = 3)]
        public double? EK_Artikel_EUR { get; set; }

        [DataMember(Order = 4)]
        public double? EK_UnterArt_EUR { get; set; }

        [DataMember(Order = 5)]
        public double? Marge_EUR { get; set; }

        [DataMember(Order = 6)]
        public double Marge_Prozent { get; set; }
    }
}