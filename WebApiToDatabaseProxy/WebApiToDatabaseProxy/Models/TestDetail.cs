using System.Runtime.Serialization;

namespace WebApiToDatabaseProxy.Models
{
    [DataContract]
    public class TestDetail
    {
        [DataMember(Order = 0)]
        public string ArtikelNr { get; set; }
    }
}