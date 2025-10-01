using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class ManifestShipmentDetailsAlt
    {
        public ManifestShipmentDetailsAlt()
        {
            CreateBooking = false;
            GeneratePiecesBarCodes = false;
        }

        public ClientInformation ClientInfo = new ClientInformation();
        public ConsigneeInformationAlt ConsigneeInfoAlt = new ConsigneeInformationAlt();
        public CommercialInvoice _CommercialInvoice = new CommercialInvoice();

        [System.ComponentModel.DefaultValue(0)]
        public int CurrenyID = 0;


        public int BillingType;
        public int PicesCount = 0;
        public double Weight = 0;
        //internal int OriginStationID;
        //internal int DestinationStationID;
        //internal int? ODAOriginStationID = null;
        //internal int? ODADestinationStationID = null;
        //public bool PickUpFromSeller = false;

        public string DeliveryInstruction = "";
        public double CODCharge = 0;

        [System.ComponentModel.DefaultValue(false)]
        public bool CreateBooking;
        public string PickUpPoint { get; set; } = "";

        [System.ComponentModel.DefaultValue(false)]
        public bool isRTO;

        [System.ComponentModel.DefaultValue(false)]
        public bool GeneratePiecesBarCodes;

        public DateTime? PromisedDeliveryDateFrom = null;
        public DateTime? PromisedDeliveryDateTo = null;

        public int LoadTypeID = 0;
        internal int ServiceTypeID = 0;
        internal int WaybillNo = 0;

        [System.ComponentModel.DefaultValue(0)]
        public double DeclareValue = 0;

        public string GoodDesc = "";
        public string Incoterm = "";
        public string IncotermsPlaceAndNotes = "";
        public string Latitude = "";
        public string Longitude = "";
        public string RefNo = "";
        [System.ComponentModel.DefaultValue(1)]
        public double Width = 1;
        [System.ComponentModel.DefaultValue(1)]
        public double Length = 1;
        [System.ComponentModel.DefaultValue(1)]
        public double Height = 1;
        [System.ComponentModel.DefaultValue(0.1)]
        internal double VolumetricWeight = 0.1;

        [System.ComponentModel.DefaultValue(0)]
        public double InsuredValue = 0;

        //[System.ComponentModel.DefaultValue(false)]
        //public bool IsInsurance = false;

        public string Reference1 = "";
        public string Reference2 = "";

        //[System.ComponentModel.DefaultValue(0)]
        //public double CustomDutyAmount = 0;

        [System.ComponentModel.DefaultValue(0)]
        public double GoodsVATAmount = 0;

        [System.ComponentModel.DefaultValue(false)]
        public bool IsCustomDutyPayByConsignee;

    }
}