using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    [DataContract]
    public class AmazonTrackingResponse
    {
        public string Message { get; set; }
        //public bool HasError { get; set; }
        public string APIVersion { get; set; }
        public PackageTrackingInformation PackageTrackingInfo { get; set; } = new PackageTrackingInformation();
    }
    [DataContract]
    public class PackageTrackingInformation
    {
        public long TrackingNumber { get; set; }
        public PackageDestinationLocation packageDestinationLocation { get; set; }
        public PackageDeliveryDate packageDeliveryDate { get; set; }
        public TrackingEventHistory trackingEventHistory { get; set; }

    }
    [DataContract]
    public class PackageDestinationLocation
    {
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }
    }
    [DataContract]
    public class PackageDeliveryDate
    {
        public string ScheduledDeliveryDate { get; set; }
        public string ReScheduledDeliveryDate { get; set; }
    }
    [DataContract]
    public class TrackingEventHistory
    {
        public List<TrackingEventDetail> TrackingEventDetail { get; set; }

    }
    public class TrackingEventDetail
    {
        public string EventStatus { get; set; }
        public string EventReason { get; set; }
        public DateTime EventDateTime { get; set; }
        public EventLocation eventLocation { get; set; }
        public PickupStoreInfo PickupStoreInfo { get; set; }
        public string SignedForByName { get; set; }

    }
    [DataContract]
    public class EventLocation
    {
        public string Street1 { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }
    }
    [DataContract]
    public class PickupStoreInfo
    {
        public PickupDueDateDetails PickupDueDateDetails { get; set; }
        public int PickupID { get; set; }
        public string StoreName { get; set; }
        public int LocationID { get; set; }
        public EventLocation StoreLocation { get; set; }
    }
    [DataContract]
    public class PickupDueDateDetails
    {
        public DateTime Date { get; set; }
        public string UTCOffset { get; set; }
    }
    public class AmazonTrackingDbModel
    {
        public string PackageDestinationLocationCity { get; set; }
        public string PackageDestinationLocationCountry { get; set; }
        public string EventLocationCity { get; set; }
        public string EventLocationCountry { get; set; }
        public string PickupStoreLocationCity { get; set; }
        public string PickupStoreLocationCountry { get; set; }
        public DateTime ScheduledDeliveryDate { get; set; }
        public string EventStatus { get; set; }
        public string EventReason { get; set; }
        public DateTime EventDateTime { get; set; }
    }
}