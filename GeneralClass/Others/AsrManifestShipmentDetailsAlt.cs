using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class AsrManifestShipmentDetailsAlt
    {
        public AsrManifestShipmentDetailsAlt()
        {
        }

        public ClientInformation ClientInfo = new ClientInformation();
        public ConsigneeInformationAlt ConsigneeInfoAlt = new ConsigneeInformationAlt();
        public CommercialInvoice _CommercialInvoice = new CommercialInvoice();

        internal double CODCharge = 0;
        internal bool IsCOD = false;
        internal int OriginStationID;
        internal int DestinationStationID;
        internal string OriginCityCode;
        internal string DestinationCityCode;
        internal int? ODAOriginStationID = null;
        internal int? ODADestinationStationID = null;
        internal int ServiceTypeID = 0;
        internal bool CreateBooking = false;
        internal bool isRTO = false;
        internal bool GeneratePiecesBarCodes = false;
        internal DateTime? PromisedDeliveryDateFrom = null;
        internal DateTime? PromisedDeliveryDateTo = null;
        [System.ComponentModel.DefaultValue(1)]
        internal double Width = 1;
        [System.ComponentModel.DefaultValue(1)]
        internal double Length = 1;
        [System.ComponentModel.DefaultValue(1)]
        internal double Height = 1;
        [System.ComponentModel.DefaultValue(0.1)]
        internal double VolumetricWeight = 0.1;

        public int PicesCount = 1;
        public int WaybillNo = 0;
        public int OriginWaybillNo = 0;
        public string RefNo = "";
        [System.ComponentModel.DefaultValue(1)]
        public int BillingType = 1;
        public int LoadTypeID = 0;
        public double DeclareValue = 0;
        public int CurrencyID = 4;
        public DateTime PickUpDate = DateTime.Now.AddDays(DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? 2 : 1);
        public WaybillSurcharge WaybillSurcharge = null;
        public string GoodDesc = "";
        public string DeliveryInstruction = "";
        public string Latitude = "";
        public string Longitude = "";
        [System.ComponentModel.DefaultValue(0)]
        public double Weight = 0;
        public double InsuredValue = 0;
        public string Reference1 = "";
        public string Reference2 = "";
        public double GoodsVATAmount = 0;
        [System.ComponentModel.DefaultValue(false)]
        public bool IsCustomDutyPayByConsignee = false;

    }
}