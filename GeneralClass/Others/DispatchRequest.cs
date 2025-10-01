using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class DispatchRequest
    {
        public ClientInformation ClientInfo = new ClientInformation();
        public List<DispatchRequestList> DispatchRequests = new List<DispatchRequestList>();

    }

    public class DispatchRequestList
    {
        public string Refernce { get; set; }
        public string Loc { get; set; }
        public DateTime manifestdate { get; set; }
    }
}