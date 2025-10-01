using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.BusinessObjects
{
    public class rpCustomerBarCode
    {
        public int ID { get; set; }
        public int ClientID { get; set; }
        public string ClientContactName { get; set; }
        public string ClientName { get; set; }
        public string ClientContactPhoneNumber { get; set; }
        public string ClientContactMobile { get; set; }
        public string ClientContactFirstAddress { get; set; }
        public string ClientContactSecondAddress { get; set; }
        public string ClientContactLocation { get; set; }
        public string ClientContactPOBox { get; set; }
        public string ClientContactEmail { get; set; }
        public string ClientContactZipCode { get; set; }
        public string ClientContactFax { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeCompanyName { get; set; }
        public string ConsigneePhoneNumber { get; set; }
        public string ConsigneeFirstAddress { get; set; }
        public string ConsigneeMobile { get; set; }
        public string ConsigneeEmail { get; set; }
        public string ConsigneeFax { get; set; }
        public string ConsigneeNear { get; set; }
        public string ConsigneeNationalID { get; set; }
        public int LoadTypeID { get; set; }
        public int WaybillNo { get; set; }
        public double DeclaredValue { get; set; }
        public string DeclareValueCurrency { get; set; }
        public double ExchangeRate { get; set; }
        public int PicesCount { get; set; }
        public double Weight { get; set; }
        public int OriginStationID { get; set; }
        public int DestinationStationID { get; set; }
        public string DeliveryInstruction { get; set; }
        public string GoodDescription { get; set; }
        public double CODCharge { get; set; }
        public double InsuredValue { get; set; }
        public string RefNo { get; set; }
        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }
        public DateTime PickUpDate { get; set; }
        public string BarCode { get; set; }
        public string CustomerPieceBarCode { get; set; }
        public string ClientCityName { get; set; }
        public string ConsigneeCityName { get; set; }
        public string ClientCountryName { get; set; }
        public string ConsigneeCountryName { get; set; }
        public string OrgCode { get; set; }
        public string OrgName { get; set; }
        public string DestCode { get; set; }
        public string DestName { get; set; }
        public int ServiceTypeID { get; set; }
        public int ClientBusinessTypeID { get; set; }
        public int BatchNo { get; set; }
        public string PODType { get; set; }
        public int ClientContactID { get; set; }
        public string ProductCode { get; set; }
        public float VolumeWeight { get; set; }
        public string Contents { get; set; }
        public bool PODTypeID { get; set; }
        public bool IsCOD { get; set; }
        public int ConsigneeID { get; set; }
        public int Incoterm { get; set; } // 1:DDU 3:DDP

    }
}