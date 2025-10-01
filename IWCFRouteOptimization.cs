using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using static InfoTrack.NaqelAPI.Class.RouteOptimizationDataType;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IWCFRouteOptimization" in both code and config file together.
    [ServiceContract]
    public interface IWCFRouteOptimization
    {
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetPassword")]
        DefaultResult GetPassword(GetPasswordRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetDeliveryStatus")]
        List<DeliveryStatusResult> GetDeliveryStatus(GetDeliveryStatusRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetCheckPointTypeFromServer")]
        List<CheckPointTypeResult> GetCheckPointTypeFromServer(GetCheckPointTypeRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetStation")]
        List<StationResult> GetStation(GetStationRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetWaybillDetails")]
        WaybillDetailsResult GetWaybillDetails(GetWaybillDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetBookingDetails")]
        BookingDetailsResult GetBookingDetails(GetBookingDetailsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendUserMeLoginsDataToServer")]
        SendUserMeLoginResult SendUserMeLoginsDataToServer(UserMELoginRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendOnDeliveryDataToServer")]
        SendOnDeliveryResult SendOnDeliveryDataToServer(OnDeliveryRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendNotDeliveredDataToServer")]
        SendNotDeliveredResult SendNotDeliveredDataToServer(NotDeliveredRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendPickUpDataToServer")]
        SendPickUpResult SendPickUpDataToServer(PickUpRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendOnCLoadingForDeliverySheet")]
        OnCLoadingForDeliverySheetResult SendOnCLoadingForDeliverySheet(OnCLoadingForDeliverySheetRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "BringPickUpData")]
        BringPickUpDataResult BringPickUpData(BringPickUpDataRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "BringMyRouteShipments")]
        List<BringMyRouteShipmentsResult> BringMyRouteShipments(BringMyRouteShipmentsRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetUserMEData")]
        GetUserMEDataResult GetUserMEData(GetUserMEDataRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckNewVersion")]
        CheckNewVersionResult CheckNewVersion(CheckNewVersionRequest instance);

        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckTruckID")]
        //DefaultResult CheckTruckID(CheckTruckIDRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetShipmentForPicking")]
        List<ShipmentsForPickingResult> GetShipmentForPicking(GetShipmentForPickingRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetShipmentsForDeliverySheet")]
        List<ShipmentsForDeliveryResult> GetShipmentsForDeliverySheet(ShipmentsForDeliverySheetRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckWaybillAlreadyPickedUp")]
        CheckWaybillAlreadyPickedUpResult CheckWaybillAlreadyPickedUp(CheckWaybillAlreadyPickedUpRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckBeforeSubmitCOD")]
        CheckBeforeSubmitCODResult CheckBeforeSubmitCOD(CheckBeforeSubmitCODRequest instance);

        //[OperationContract]
        //[WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "DeliverySheetChecking")]
        //List<DeliverySheetCheckingResult> DeliverySheetChecking(DeliverySheetCheckingRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "CheckPendingCOD")]
        List<CheckPendingCODResult> CheckPendingCOD(CheckPendingCODRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "OptimizedOutOfDeliveryShipment")]
        List<OptimizedOutOfDeliveryShipmentResult> OptimizedOutOfDeliveryShipment(OptimizedOutOfDeliveryShipmentRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "AddCallHistory")]
        DefaultResult AddCallHistory(NewCallRequest instance);
        //DefaultResult AddCallHistory(string EmployID, string MobileNo);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "GetLoadType")]
        GetLoadTypeResult GetLoadType(GetLoadTypeRequest instance);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SendCheckPoint")]
        SendCheckPointResult SendCheckPoint(SendCheckPointRequest instance);
    }
}