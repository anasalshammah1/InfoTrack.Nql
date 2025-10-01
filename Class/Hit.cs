using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.Class
{
    public class Hit
    {
        public Source _source { get; set; }
        public List<Hit> hits { get; set; }
    }

    public class Root
    {
        public Hit hits { get; set; }
    }

    public class Source
    {
        public int Id { get; set; }
        public string StationCode { get; set; }
        public DateTime Date { get; set; }
        public int ClientID { get; set; }
        public int WaybillNo { get; set; }
        public int PicesCount { get; set; }
        public int EmployID { get; set; }
        public int EventCode { get; set; }
        public string EventName { get; set; }
        public string Activity { get; set; }
        public string ActivityAr { get; set; }
        public int HasError { get; set; }
        public string ErrorMessage { get; set; }
        public string Comments { get; set; }
        public string RefNo { get; set; }
        public int IsInternalType { get; set; }
        public int IsSent { get; set; }
        public int NewWaybillNo { get; set; }
        public int TrackingTypeID { get; set; }
        public int WaybillID { get; set; }
        public int DeliveryStatusID { get; set; }
        public string DeliveryStatusMessage { get; set; }
        public string ImageURL { get; set; }
        public int ServiceTypeID { get; set; }
        public int LoadTypeID { get; set; }
        public int ProductTypeID { get; set; }
        public int DivisionID { get; set; }
        public int BillingTypeID { get; set; }
        public string BillingTypeCode { get; set; }
        public string BillingTypeName { get; set; }
        public double CollectedAmount { get; set; }
        public double TotalCharges { get; set; }
        public string FacilityCode { get; set; }
        public string DeliveryStatusReason { get; set; }
        public string Reference { get; set; }
        public string Remarks { get; set; }
    }

    public class ClientResponse
    {
        public string StationCode { get; set; }
        public DateTime Date { get; set; }
        [JsonProperty(PropertyName = "ActivityCode")]
        public int TrackingTypeID { get; set; }
        public string Activity { get; set; }
        [JsonProperty(PropertyName = "ArabicActivity")]
        public string ActivityAr { get; set; }
        public int WaybillNo { get; set; }
        public int ClientID { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public string Comments { get; set; }
        public string RefNo { get; set; }
        public int DeliveryStatusID { get; set; }
        public string DeliveryStatusMessage { get; set; }
        public int EventCode { get; set; }
    }
}