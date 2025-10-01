using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.BusinessObjects
{
    public class rpCustomerBarCodeAsr
    {
        public int WayBillNo { get; set; }
        public double PicesCount { get; set; }
        public double Weight { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }
        public double VolumeWeight { get; set; }
        public string OrgCode { get; set; }
        public string DestCode { get; set; }
        public string DestName { get; set; }
        public string OrgName { get; set; }
        public string ClientLoc { get; set; }
        public string ClientFAdd { get; set; }
        public string ClientSAdd { get; set; }
        public string ClientPO { get; set; }
        public string ClientZip { get; set; }
        public string ClientFax { get; set; }
        public string ConName { get; set; }
        public string ConEmail { get; set; }
        public string ConPO { get; set; }
        public string ConMobile { get; set; }
        public string ConFAdd { get; set; }
        public string ConSAdd { get; set; }
        public string ConPh { get; set; }
        public string ConFax { get; set; }
        public int WayBillID { get; set; }
        public DateTime PickUpDate { get; set; }
        public int ClientID { get; set; }
        public bool Isinsurance { get; set; }
        public double DeclaredValue { get; set; }
        public int CurrencyID { get; set; }
        public double ExchangeRate { get; set; }
        public double InsuredValue { get; set; }
        public int PODTypeID { get; set; }
        public string PODDetail { get; set; }
        public string DeliveryInstruction { get; set; }
        public int BillingTypeID { get; set; }
        public string ClientName { get; set; }
        public string ClientPh { get; set; }
        public string ClientEmail { get; set; }
        public string ClientMobile { get; set; }
        public int BusinessTypeID { get; set; }
        public string ClientCity { get; set; }
        public string ConCity { get; set; }
        public string ConCountry { get; set; }
        public string ClientCountry { get; set; }
        public int ConsigneeID { get; set; }
        public string Name { get; set; }
        public string ClientComName { get; set; }
        public int ServiceTypeID { get; set; }
        public bool IsCOD { get; set; }
        public double CODCharge { get; set; }
        public int LoadTypeID { get; set; }
        public string Contents { get; set; }
        public string RefNo { get; set; }
        public string BookingRefNo { get; set; }
        public string BatchNo { get; set; }
        public string PODType { get; set; }
        public int ClientContactID { get; set; }
        public string BarCode { get; set; }
        public string CustomerPieceBarCode { get; set; }
        public string GoodDesc { get; set; }
        public int SurChargeCodeID { get; set; }
        public string SurChargeCode { get; set; }
        public string OriginCityCode { get; set; }
        public string DestinationCityCode { get; set; }
        public string OriginCityName { get; set; }
        public string DestinationCityName { get; set; }
        public string Surcharge1 { get; set; }
        public string Surcharge2 { get; set; }
        public string Surcharge3 { get; set; }
        public string Surcharge4 { get; set; }
    }
}