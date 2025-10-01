using InfoTrack.NaqelAPI.App_Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace InfoTrack.NaqelAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "WCFShipping" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select WCFShipping.svc or WCFShipping.svc.cs at the Solution Explorer and start debugging.
    public class WCFShipping : IWCFShipping
    {
        public List<Tracking> TraceByWaybillNo(string WaybillNo)
        {
            List<Tracking> TrackingList = new List<Tracking>();

            if (WaybillNo.Length > 8)
                WaybillNo = WaybillNo.Remove(8, WaybillNo.Length - 8);

            App_Data.DataDataContext dc = new App_Data.DataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<ViwTracking> ViwTrackingInstance = new List<ViwTracking>();
            ViwTrackingInstance = dc.ViwTrackings.Where(P => P.WaybillNo == Convert.ToInt32(WaybillNo)).OrderBy(P => P.Date).ToList();

            for (int i = 0; i < ViwTrackingInstance.Count; i++)
            {
                Tracking newActivity = new Tracking();
                newActivity.ClientID = 1024600;
                newActivity.Date = ViwTrackingInstance[i].Date;

                newActivity.Activity = ViwTrackingInstance[i].Activity;
                newActivity.StationCode = ViwTrackingInstance[i].StationCode;
                newActivity.WaybillNo = ViwTrackingInstance[i].WaybillNo;
                newActivity.HasError = ViwTrackingInstance[i].HasError;
                newActivity.ErrorMessage = ViwTrackingInstance[i].ErrorMessage;
                newActivity.Comments = ViwTrackingInstance[i].Comments;
                newActivity.RefNo = ViwTrackingInstance[i].RefNo;

                if (ViwTrackingInstance[i].EventCode.HasValue)
                    newActivity.EventCode = ViwTrackingInstance[i].EventCode.Value;
                else
                    newActivity.EventCode = 3;

                TrackingList.Add(newActivity);
            }

            return TrackingList;
        }

        /*App_Data.DataDataContext dc;
        int WaybillNo = 0;
        public Result CreateWaybill(ManifestShipmentDetails _ManifestShipmentDetails)
        {
            Result result = new Result();

            //_ManifestShipmentDetails.
            result = _ManifestShipmentDetails.ClientInfo.CheckClientInfo(_ManifestShipmentDetails.ClientInfo, true);

            //if (result.HasError)
            //{
            //    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
            //    return result;
            //}

            result = _ManifestShipmentDetails.IsWaybillDetailsValid(_ManifestShipmentDetails);
            //if (result.HasError)
            //{
            //    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
            //    return result;
            //}

            _ManifestShipmentDetails.DeliveryInstruction = GlobalVar.GV.GetString(_ManifestShipmentDetails.DeliveryInstruction, 200);
            App_Data.CustomerWayBill NewWaybill = new App_Data.CustomerWayBill();
            NewWaybill.ClientID = _ManifestShipmentDetails.ClientInfo.ClientID;
            NewWaybill.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
            NewWaybill.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;
            NewWaybill.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
            NewWaybill.ServiceTypeID = _ManifestShipmentDetails.ServiceTypeID;
            NewWaybill.BillingTypeID = _ManifestShipmentDetails.BillingType;
            if (NewWaybill.BillingTypeID == 5)
                NewWaybill.IsCOD = true;
            else
                NewWaybill.IsCOD = false;

            _ManifestShipmentDetails.ConsigneeInfo.CheckConsigneeData(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.ConsigneeInfo);
            NewWaybill.ConsigneeID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeID;
            NewWaybill.ConsigneeAddressID = _ManifestShipmentDetails.ConsigneeInfo.ConsigneeDetailID;

            NewWaybill.OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode, _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode));
            //NewWaybill.OriginStationID = Convert.ToInt32(_ManifestShipmentDetails.OriginStationID);

            NewWaybill.DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(_ManifestShipmentDetails.ConsigneeInfo.CityCode, _ManifestShipmentDetails.ConsigneeInfo.CountryCode));
            //NewWaybill.DestinationStationID = Convert.ToInt32(_ManifestShipmentDetails.DestinationStationID);

            NewWaybill.PickUpDate = DateTime.Now;
            NewWaybill.PicesCount = _ManifestShipmentDetails.PicesCount;
            NewWaybill.Weight = _ManifestShipmentDetails.Weight;
            NewWaybill.Width = 1;
            NewWaybill.Length = 1;
            NewWaybill.Height = 1;
            NewWaybill.VolumeWeight = 0.0002;
            NewWaybill.BookingRefNo = "";
            NewWaybill.ManifestedTime = DateTime.Now;

            if (_ManifestShipmentDetails.RefNo != "")
                NewWaybill.RefNo = GlobalVar.GV.GetString(_ManifestShipmentDetails.RefNo, 100); ;

            NewWaybill.IsPrintBarcode = false;
            NewWaybill.StatusID = 1;
            NewWaybill.IsInsurance = false;
            NewWaybill.DeclaredValue = 0;
            NewWaybill.InsuredValue = 0;
            NewWaybill.PODDetail = "";
            NewWaybill.DeliveryInstruction = _ManifestShipmentDetails.DeliveryInstruction;
            NewWaybill.CODCharge = _ManifestShipmentDetails.CODCharge;
            NewWaybill.Discount = 0;

            //if (_ManifestShipmentDetails.CODCharge > 0)
            //    NewWaybill.IsCOD = true;
            //else
            //    NewWaybill.IsCOD = false;
            NewWaybill.NetCharge = 0;
            NewWaybill.OnAccount = 0;
            NewWaybill.ServiceCharge = 0;

            NewWaybill.ODAStationCharge = 0;
            NewWaybill.OtherCharge = 0;
            NewWaybill.PaidAmount = 0;
            NewWaybill.SpecialCharge = 0;
            NewWaybill.StandardShipment = 0;
            NewWaybill.StorageCharge = 0;
            NewWaybill.ProductTypeID = Convert.ToInt32(EnumList.ProductType.Home_Delivery);
            NewWaybill.IsShippingAPI = true;

            if (_ManifestShipmentDetails.ClientInfo.ClientID == 1024600)
            {
                NewWaybill.IsRTO = _ManifestShipmentDetails.isRTO;
                NewWaybill.PODTypeID = null;
                NewWaybill.PODDetail = "";
            }
            else
                NewWaybill.IsRTO = false;
            NewWaybill.IsManifested = false;

            //if (_ManifestShipmentDetails.ODAOriginID.HasValue)
            //    NewWaybill.ODAOriginID = _ManifestShipmentDetails.ODAOriginID;

            //if (_ManifestShipmentDetails.ODADestinationID.HasValue)
            //    NewWaybill.ODADestinationID = _ManifestShipmentDetails.ODADestinationID;

            dc = new App_Data.DataDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (WaybillNo > 0)
                NewWaybill.WayBillNo = WaybillNo;
            else
                NewWaybill.WayBillNo = GlobalVar.GV.GetWaybillNo(EnumList.ProductType.Home_Delivery);

            if (NewWaybill.WayBillNo < 1000)
            {
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details < 1000";
                return result;
            }

            try
            {
                dc.CustomerWayBills.InsertOnSubmit(NewWaybill);
                dc.SubmitChanges();
            }
            catch (Exception e)
            {
                //WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                result.HasError = true;
                result.Message = "an error happen when saving the waybill details code : 120";
                GlobalVar.GV.AddErrorMessage(e, _ManifestShipmentDetails.ClientInfo);
                GlobalVar.GV.AddErrorMessage1(dc.Connection.ConnectionString, _ManifestShipmentDetails.ClientInfo);
            }

            if (!result.HasError)
            {
                result.WaybillNo = NewWaybill.WayBillNo;
                result.Key = NewWaybill.ID;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("NewWaybillSuccess");

                if (_ManifestShipmentDetails.ClientInfo.ClientID != 9016808 && _ManifestShipmentDetails.CreateBooking == true)
                {
                    BookingShipmentDetails _bookingDetails = new BookingShipmentDetails();
                    _bookingDetails.ClientInfo = _ManifestShipmentDetails.ClientInfo;
                    _bookingDetails.ClientInfo.ClientAddressID = _ManifestShipmentDetails.ClientInfo.ClientAddressID;
                    _bookingDetails.ClientInfo.ClientContactID = _ManifestShipmentDetails.ClientInfo.ClientContactID;

                    _bookingDetails.BillingType = _ManifestShipmentDetails.BillingType;
                    _bookingDetails.PicesCount = _ManifestShipmentDetails.PicesCount;
                    _bookingDetails.Weight = _ManifestShipmentDetails.Weight;
                    _bookingDetails.PickUpPoint = "";
                    _bookingDetails.SpecialInstruction = "";
                    _bookingDetails.OriginStationID = NewWaybill.OriginStationID;
                    _bookingDetails.DestinationStationID = NewWaybill.DestinationStationID;
                    _bookingDetails.OfficeUpTo = DateTime.Now;
                    _bookingDetails.PickUpReqDateTime = DateTime.Now;
                    _bookingDetails.ContactPerson = _ManifestShipmentDetails.ClientInfo.ClientContact.Name;
                    _bookingDetails.ContactNumber = _ManifestShipmentDetails.ClientInfo.ClientContact.PhoneNumber;
                    _bookingDetails.LoadTypeID = _ManifestShipmentDetails.LoadTypeID;
                    _bookingDetails.WaybillNo = result.WaybillNo;

                    Result BookingResult = new Result();
                    //try
                    //{
                    //    BookingResult = CreateBooking(_bookingDetails);
                    //}
                    //catch
                    //{
                    //    WritetoXML(_ManifestShipmentDetails, _ManifestShipmentDetails.ClientInfo, "Create New Waybill", EnumList.MethodType.CreateWaybill, result);
                    //    BookingResult.HasError = true;
                    //}

                    if (!BookingResult.HasError)
                        result.BookingRefNo = GlobalVar.GV.GetString(BookingResult.BookingRefNo, 100);
                }

                if (_ManifestShipmentDetails.GeneratePiecesBarCodes)
                {
                    App_Data.CustomerBarCode instanceBarcode;

                    for (int i = 1; i <= NewWaybill.PicesCount; i++)
                    {
                        instanceBarcode = new App_Data.CustomerBarCode();
                        instanceBarcode.BarCode = Convert.ToInt64(Convert.ToString(NewWaybill.WayBillNo) + i.ToString("D5"));
                        instanceBarcode.CustomerWayBillsID = NewWaybill.ID;
                        instanceBarcode.StatusID = 1;
                        dc.CustomerBarCodes.InsertOnSubmit(instanceBarcode);
                        dc.SubmitChanges();
                    }
                }
            }

            if (WaybillNo <= 0)
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.Create_New_Shipment, result.WaybillNo.ToString(), result.Key);
            else
                GlobalVar.GV.CreateShippingAPIRequest(_ManifestShipmentDetails.ClientInfo, EnumList.APIRequestType.UpdateWaybill, WaybillNo.ToString(), result.Key);
            return result;
        }*/
    }
}