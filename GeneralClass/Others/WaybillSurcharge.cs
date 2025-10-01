using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class WaybillSurcharge
    {
        private int WaybillNo;
        private int ClientID;

        [System.ComponentModel.DefaultValue("")]
        private string Description;

        [System.ComponentModel.DefaultValue(0)]
        public List<int> SurchargeIDList;

        [System.ComponentModel.DefaultValue(1)]
        private int StatusID;
    }
}