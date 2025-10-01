using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using static InfoTrack.NaqelAPI.IPhoneDataTypes;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWCFIPhone" in both code and config file together.
    [ServiceContract]
    public interface IWCFIPhone
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetCountries")]
        List<CountryResult> GetCountries();

        //[OperationContract]
        //[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetCities")]
        //List<InfoTrack.NaqelAPI.IPhoneDataTypes.City> GetCities();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetStations")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.StationResult> GetStations();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetComplaintType")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintTypeResult> GetComplaintType();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetLoadType")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.LoadTypeResult> GetLoadType();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetLocationsList")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.OurLocationsResult> GetLocationsList();

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "TraceByWaybillNo")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.IPhoneTrackingResult> TraceByWaybillNo(IPhoneDataTypes.TrackingDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetWaybillListByMobileNo")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> GetWaybillListByMobileNo(IPhoneDataTypes.TrackingDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetWaybillListByReference")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.WaybillListResult> GetWaybillListByReference(IPhoneDataTypes.TrackingDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetRate")]
        IPhoneDataTypes.RateResult GetRate(IPhoneDataTypes.GetRateDetailsRequest instance);        

        //[OperationContract]
        //[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetLocationDetails/{LocationID}")]
        // OurLocation GetLocationDetails(string LocationID);

        //[OperationContract]
        //[WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetFleetDetails/{FleetNo}")]
        //IPhoneDataTypes.FleetDetails GetFleetDetails(string FleetNo);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SignUp")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.AppMobileVerificationResult SignUp(IPhoneDataTypes.SignUpDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "ResendPassword")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.DefaultDetailsResult ResendPassword(IPhoneDataTypes.MobileVerificationRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckPassword")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.AppClientDetailsResult CheckPassword(IPhoneDataTypes.MobileVerificationRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "VerifiedMobileNo")]
        void VerifiedMobileNo(InfoTrack.NaqelAPI.IPhoneDataTypes.VerifiedMobleNoRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SyncAccount")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult SyncAccount(IPhoneDataTypes.AccountDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetAdvs")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.AdvResult> GetAdvs();

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetNews")]
        List<InfoTrack.NaqelAPI.IPhoneDataTypes.NewsResult> GetNews();

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "AddComplaint")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult AddComplaint(InfoTrack.NaqelAPI.IPhoneDataTypes.ComplaintDataRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CreateClientAddress")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.CreatingAccountResult CreateClientAddress(InfoTrack.NaqelAPI.IPhoneDataTypes.ClientDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CreateBooking")]
        InfoTrack.NaqelAPI.IPhoneDataTypes.SyncResult CreateBooking(InfoTrack.NaqelAPI.IPhoneDataTypes.BookingDetailsRequest instance);

        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetCurrentLOcationByWaybill")]
        //ServiceReference1.CurrentLocation GetCurrentLOcationByWaybill(GetCurrentLOcationByWaybillRequest instance);

        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetCurrentLOcationByWaybill")]
        //InfoTrack.NaqelAPI.IPhoneDataTypes.GetCurrentLOcationByWaybillResult GetCurrentLocation(InfoTrack.NaqelAPI.IPhoneDataTypes.GetCurrentLOcationByWaybillRequest instance);

    }
}