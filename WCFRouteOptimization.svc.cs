using System;
using System.Collections.Generic;
using System.Linq;
using static InfoTrack.NaqelAPI.Class.RouteOptimizationDataType;
using InfoTrack.BusinessLayer.DContext;
using InfoTrack.NaqelAPI.Class;

namespace InfoTrack.NaqelAPI
{
    public class WCFRouteOptimization : IWCFRouteOptimization
    {
        InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcData = new BusinessLayer.DContext.DocumentDataDataContext();
        InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext();
        InfoTrack.NaqelAPI.App_Data.HHDDataContext dcHHDERPNaqelSE = new App_Data.HHDDataContext();
        InfoTrack.BusinessLayer.DContext.HHDDataContext dcHHDERPNaqel = new BusinessLayer.DContext.HHDDataContext();

        public WCFRouteOptimization()
        {
            GlobalVar.GV.GetCurrendDate();
        }

        public DefaultResult GetPassword(GetPasswordRequest instance)
        {
            DefaultResult result = new DefaultResult();
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.Employ employInstance = new BusinessLayer.DContext.Employ();

            if (dcMaster.Employs.Where(P => P.ID == instance.EmployID &&
                                            P.StatusID != 3).Count() > 0)
            {
                employInstance = dcMaster.Employs.First(P => P.ID == instance.EmployID &&
                                            P.StatusID != 3);
                if (employInstance.PhoneNo != null && employInstance.PhoneNo != "")
                {
                    InfoTrack.BusinessLayer.DContext.UserAccountDataContext dcUserAccount = new BusinessLayer.DContext.UserAccountDataContext(GlobalVar.GV.GetInfoTrackConnection());
                    InfoTrack.BusinessLayer.DContext.HHDDataContext hhdContext = new BusinessLayer.DContext.HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());
                    if (hhdContext.UserMEs.Where(P => P.EmployID == instance.EmployID && P.StatusID != 3).Count() > 0)
                    {
                        //Check the password in the User ME Table
                        InfoTrack.BusinessLayer.DContext.UserME userMEInstance = new BusinessLayer.DContext.UserME();
                        userMEInstance = hhdContext.UserMEs.First(P => P.EmployID == instance.EmployID && P.StatusID != 3);
                        XMLGeneral xmlGeneral = new XMLGeneral();
                        InfoTrack.Common.Security sec = new Common.Security();
                        string pass = sec.Decrypt(userMEInstance.Password.ToArray());
                        string message = "Your password is :" + sec.Decrypt(userMEInstance.Password.ToArray());
                        xmlGeneral.SendOTP(sec.Encrypt("-1"), employInstance.PhoneNo, message, EnumList.PurposeList.ForgotPassword, instance.EmployID.ToString());

                        result.HasError = false;
                        if (instance.LanguageID == 1)
                            result.ErrorMessage = "Your password has been sent to this mobile no :" + employInstance.PhoneNo.ToString();
                        else
                            result.ErrorMessage = "Your password has been sent to this mobile no :" + employInstance.PhoneNo.ToString();
                    }
                    else
                        if (dcUserAccount.Users.Where(P => P.EmployID == instance.EmployID && P.StatusID != 3).Count() > 0)
                    {
                        //Check the password in the User Table
                        InfoTrack.BusinessLayer.DContext.User userInstance = new BusinessLayer.DContext.User();
                        userInstance = dcUserAccount.Users.First(P => P.EmployID == instance.EmployID && P.StatusID != 3);
                        XMLGeneral xmlGeneral = new XMLGeneral();
                        InfoTrack.Common.Security sec = new Common.Security();
                        string message = "Your password is :" + sec.Decrypt(userInstance.Password.ToArray());
                        xmlGeneral.SendOTP(sec.Encrypt("-1"), employInstance.PhoneNo, message, EnumList.PurposeList.ForgotPassword, instance.EmployID.ToString());

                        result.HasError = false;
                        if (instance.LanguageID == 1)
                            result.ErrorMessage = "Your password has been sent to this mobile no :" + employInstance.PhoneNo.ToString();
                        else
                            result.ErrorMessage = "Your password has been sent to this mobile no :" + employInstance.PhoneNo.ToString();
                    }
                    else
                    {
                        result.HasError = true;
                        if (instance.LanguageID == 1)
                            result.ErrorMessage = "There is no user has same ID.";
                        else
                            result.ErrorMessage = "There is no user has same ID.";
                    }
                }
                else
                {
                    result.HasError = true;
                    if (instance.LanguageID == 1)
                        result.ErrorMessage = "Mobile no is not updated for this employee " + instance.EmployID.ToString() + " in the employ list";
                    else
                        result.ErrorMessage = "Mobile no is not updated for this employee " + instance.EmployID.ToString() + " in the employ list";
                }
            }
            else
            {
                result.HasError = true;
                if (instance.LanguageID == 1)
                    result.ErrorMessage = "Please check your Employ ID : " + instance.EmployID.ToString();
                else
                    result.ErrorMessage = "Please check your Employ ID : " + instance.EmployID.ToString();
            }

            return result;
        }

        public List<DeliveryStatusResult> GetDeliveryStatus(GetDeliveryStatusRequest instance)
        {
            List<DeliveryStatusResult> result = new List<DeliveryStatusResult>();

            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<DeliveryStatus> deliveryList = new List<DeliveryStatus>();
            deliveryList = dcMaster.DeliveryStatus.Where(P => P.StatusID == 1 && P.ID > 5).ToList();
            for (int i = 0; i < deliveryList.Count; i++)
                result.Add(new DeliveryStatusResult(deliveryList[i].ID, deliveryList[i].Code, deliveryList[i].Name, deliveryList[i].FName));

            return result;
        }

        public List<StationResult> GetStation(GetStationRequest instance)
        {
            List<StationResult> result = new List<StationResult>();

            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<ViwStationByCountry> stationList = new List<ViwStationByCountry>();
            stationList = dcMaster.ViwStationByCountries.Where(P => P.StatusID == 1).ToList();
            for (int i = 0; i < stationList.Count; i++)
                result.Add(new StationResult(stationList[i].ID, stationList[i].Code, stationList[i].Name, stationList[i].FName, stationList[i].CountryID.Value));

            return result;
        }

        public WaybillDetailsResult GetWaybillDetails(GetWaybillDetailsRequest instance)
        {
            WaybillDetailsResult result = new WaybillDetailsResult();

            dcData = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcHHDERPNaqel = new BusinessLayer.DContext.HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());

            InfoTrack.BusinessLayer.DContext.ViwWaybill viwWaybillInstance = new BusinessLayer.DContext.ViwWaybill();

            if (dcData.ViwWaybills.Where(P => P.WayBillNo == instance.WaybillNo).Count() > 0)
            {
                viwWaybillInstance = dcData.ViwWaybills.First(P => P.WayBillNo == instance.WaybillNo);
                InfoTrack.BusinessLayer.DContext.Consignee consigneeInstance = dcMaster.Consignees.First(P => P.ID == viwWaybillInstance.ConsigneeID);
                InfoTrack.BusinessLayer.DContext.ConsigneeDetail consigneeDetailInstance = dcMaster.ConsigneeDetails.First(P => P.ID == viwWaybillInstance.ConsigneeAddressID);


                result.HasError = false;

                result.ID = viwWaybillInstance.ID;
                result.WaybillNo = viwWaybillInstance.WayBillNo;
                result.PiecesCount = Convert.ToInt32(viwWaybillInstance.PicesCount);
                result.Weight = viwWaybillInstance.Weight;
                result.BillingType = dcMaster.BillTypes.First(P => P.ID == viwWaybillInstance.BillingTypeID).Code;

                if (viwWaybillInstance.BillingTypeID == 5)
                    result.CODAmount = viwWaybillInstance.CollectedAmount.Value;

                result.ConsigneeName = viwWaybillInstance.ConsigneeName;
                result.ConsigneeFName = consigneeInstance.FName;
                result.PhoneNo = viwWaybillInstance.ConsigneePhoneNo;
                result.MobileNo = viwWaybillInstance.ConsigneeMobileNo;

                result.Address = consigneeDetailInstance.FirstAddress;
                if (consigneeDetailInstance.SecondAddress != null)
                    result.SecondLine = consigneeDetailInstance.SecondAddress;
                if (consigneeDetailInstance.Near != null)
                    result.Near = consigneeDetailInstance.Near;

                result.locationCoordinate = GetLocationByWaybillNo(instance.WaybillNo);

                if (dcHHDERPNaqel.PickUps.Where(P => P.WaybillNo == instance.WaybillNo).Count() > 0)
                {
                    List<InfoTrack.BusinessLayer.DContext.PickUp> pickupList = dcHHDERPNaqel.PickUps.Where(P => P.WaybillNo == instance.WaybillNo).ToList();
                    List<InfoTrack.BusinessLayer.DContext.PickUpDetail> pickupDetailList = dcHHDERPNaqel.PickUpDetails.Where(P => P.PickUpID == pickupList[pickupList.Count - 1].ID).ToList();
                    for (int i = 0; i < pickupDetailList.Count; i++)
                        result.BarCodeList.Add(pickupDetailList[i].BarCode);
                }

            }
            else
            {
                result.HasError = true;
                result.ErrorMessage = "There is no waybill has the same No.";
            }

            return result;
        }

        private LocationCoordinate GetLocationByWaybillNo(int WaybillNo)
        {
            LocationCoordinate locationCoordinate = new LocationCoordinate();

            // 1 - Customer Shared The Location.
            // 2 - Shipper send it with the waybill details.
            // 3 - Previous Transactions.

            dcData = new BusinessLayer.DContext.DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dcMaster.ConsigneeLocations.Where(P => P.WaybillNo == WaybillNo && P.StatusID != 3).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.ConsigneeLocation consigneeLocationInstance = dcMaster.ConsigneeLocations.First(P => P.WaybillNo == WaybillNo && P.StatusID != 3);
                locationCoordinate.Latitude = consigneeLocationInstance.Latitude;
                locationCoordinate.Longitude = consigneeLocationInstance.Longitude;
            }
            //else
            //    if (dcData.CustomerWayBills.Where(P=>P.WayBillNo == ))


            return locationCoordinate;
        }

        public BookingDetailsResult GetBookingDetails(GetBookingDetailsRequest instance)
        {
            BookingDetailsResult result = new BookingDetailsResult();

            return result;
        }

        public SendUserMeLoginResult SendUserMeLoginsDataToServer(UserMELoginRequest instance)
        {
            SendUserMeLoginResult result = new SendUserMeLoginResult();

            dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
            InfoTrack.NaqelAPI.App_Data.UserMeLogin userMeInstance = new App_Data.UserMeLogin();

            userMeInstance.EmployID = instance.EmployID;
            userMeInstance.StateID = instance.StateID;
            userMeInstance.Date = instance.Date;
            userMeInstance.HHDName = instance.HHDName;
            userMeInstance.Version = instance.AppVersion;
            userMeInstance.IsSync = instance.IsSync;
            userMeInstance.TruckID = instance.TruckID;

            dcHHDERPNaqelSE.UserMeLogins.InsertOnSubmit(userMeInstance);
            dcHHDERPNaqelSE.SubmitChanges();
            result.HasError = false;
            result.ID = instance.ID;
            result.IsSync = true;
            return result;
        }

        public SendOnDeliveryResult SendOnDeliveryDataToServer(OnDeliveryRequest instance)
        {
            SendOnDeliveryResult result = new SendOnDeliveryResult();
            try
            {
                dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
                InfoTrack.NaqelAPI.App_Data.OnDelivery onDeliveryInstance = new App_Data.OnDelivery();

                onDeliveryInstance.WaybillNo = instance.WaybillNo;
                onDeliveryInstance.ReceiverName = instance.ReceiverName;
                onDeliveryInstance.PiecesCount = instance.PiecesCount;
                onDeliveryInstance.TimeIn = instance.TimeIn;
                onDeliveryInstance.TimeOut = instance.TimeOut;
                onDeliveryInstance.UserID = instance.EmployID;
                onDeliveryInstance.IsSync = false;
                onDeliveryInstance.StationID = instance.StationID;
                onDeliveryInstance.IsPartial = instance.IsPartial;
                onDeliveryInstance.Latitude = instance.Latitude;
                onDeliveryInstance.Longitude = instance.Longitude;
                onDeliveryInstance.ReceivedAmt = instance.ReceivedAmt;
                onDeliveryInstance.CashAmount = instance.CashAmount;
                onDeliveryInstance.POSAmount = instance.POSAmount;
                onDeliveryInstance.ReceiptNo = instance.ReceiptNo;
                onDeliveryInstance.StopPointsID = instance.StopPointsID;
                dcHHDERPNaqelSE.OnDeliveries.InsertOnSubmit(onDeliveryInstance);
                dcHHDERPNaqelSE.SubmitChanges();

                for (int i = 0; i < instance.OnDeliveryDetailRequestList.Count; i++)
                {
                    InfoTrack.NaqelAPI.App_Data.OnDeliveryDetail onDeliveryDetailInstance = new App_Data.OnDeliveryDetail();
                    onDeliveryDetailInstance.BarCode = instance.OnDeliveryDetailRequestList[i].BarCode;
                    onDeliveryDetailInstance.IsSync = false;
                    onDeliveryDetailInstance.DeliveryID = onDeliveryInstance.ID;
                    dcHHDERPNaqelSE.OnDeliveryDetails.InsertOnSubmit(onDeliveryDetailInstance);
                }
                dcHHDERPNaqelSE.SubmitChanges();

                result.HasError = false;
                result.ErrorMessage = "";
                result.ID = instance.ID;
                result.IsSync = true;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.IsSync = false;
            }
            return result;
        }

        public SendNotDeliveredResult SendNotDeliveredDataToServer(NotDeliveredRequest instance)
        {
            SendNotDeliveredResult result = new SendNotDeliveredResult();
            try
            {
                dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
                InfoTrack.NaqelAPI.App_Data.NotDelivered notDeliveredInstance = new App_Data.NotDelivered();

                notDeliveredInstance.WaybillNo = instance.WaybillNo;
                notDeliveredInstance.TimeIn = instance.TimeIn;
                notDeliveredInstance.TimeOut = instance.TimeOut;
                notDeliveredInstance.UserID = instance.UserID;
                notDeliveredInstance.IsSync = false;
                notDeliveredInstance.StationID = instance.StationID;
                notDeliveredInstance.PiecesCount = instance.PiecesCount;
                notDeliveredInstance.DeliveryStatusID = instance.DeliveryStatusID;
                notDeliveredInstance.Notes = instance.Notes;
                notDeliveredInstance.Latitude = instance.Latitude;
                notDeliveredInstance.Longitude = instance.Longitude;
                dcHHDERPNaqelSE.NotDelivereds.InsertOnSubmit(notDeliveredInstance);
                dcHHDERPNaqelSE.SubmitChanges();

                if (instance.NotDeliveredDetailRequestList != null && instance.NotDeliveredDetailRequestList.Count > 0)
                {
                    for (int i = 0; i < instance.NotDeliveredDetailRequestList.Count; i++)
                    {
                        InfoTrack.NaqelAPI.App_Data.NotDeliveredDetail notDeliveredDetailInstance = new App_Data.NotDeliveredDetail();
                        notDeliveredDetailInstance.BarCode = instance.NotDeliveredDetailRequestList[i].BarCode;
                        notDeliveredDetailInstance.IsSync = false;
                        notDeliveredDetailInstance.NotDeliveredID = notDeliveredInstance.ID;
                        dcHHDERPNaqelSE.NotDeliveredDetails.InsertOnSubmit(notDeliveredDetailInstance);
                    }
                }
                dcHHDERPNaqelSE.SubmitChanges();

                result.HasError = false;
                result.ErrorMessage = "";
                result.ID = instance.ID;
                result.IsSync = true;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.IsSync = false;
            }
            return result;
        }

        public SendPickUpResult SendPickUpDataToServer(PickUpRequest instance)
        {
            SendPickUpResult result = new SendPickUpResult();
            try
            {
                dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
                InfoTrack.NaqelAPI.App_Data.PickUp pickUpInstance = new App_Data.PickUp();

                pickUpInstance.WaybillNo = instance.WaybillNo.ToString();
                pickUpInstance.ClientID = instance.ClientID;
                pickUpInstance.FromStationID = instance.FromStationID;
                pickUpInstance.ToStationID = instance.ToStationID;
                pickUpInstance.PieceCount = instance.PiecesCount;
                pickUpInstance.Weight = instance.Weight;
                pickUpInstance.TimeIn = instance.TimeIn;
                pickUpInstance.TimeOut = instance.TimeOut;
                pickUpInstance.UserID = instance.UserMEID;
                pickUpInstance.IsSync = false;
                pickUpInstance.StationID = instance.StationID;
                pickUpInstance.RefNo = instance.RefNo;
                pickUpInstance.Latitude = instance.Latitude;
                pickUpInstance.Longitude = instance.Longitude;
                pickUpInstance.ReceivedAmt = instance.ReceivedAmount;
                pickUpInstance.CurrentVersion = instance.AppVersion;

                dcHHDERPNaqelSE.PickUps.InsertOnSubmit(pickUpInstance);
                dcHHDERPNaqelSE.SubmitChanges();

                for (int i = 0; i < instance.PickUpDetailRequestList.Count; i++)
                {
                    InfoTrack.NaqelAPI.App_Data.PickUpDetail pickUpDetailInstance = new App_Data.PickUpDetail();
                    pickUpDetailInstance.BarCode = instance.PickUpDetailRequestList[i].BarCode;
                    pickUpDetailInstance.IsSync = false;
                    pickUpDetailInstance.PickUpID = pickUpInstance.ID;
                    dcHHDERPNaqelSE.PickUpDetails.InsertOnSubmit(pickUpDetailInstance);
                }
                dcHHDERPNaqelSE.SubmitChanges();

                result.HasError = false;
                result.ErrorMessage = "";
                result.ID = instance.ID;
                result.IsSync = true;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.IsSync = false;
            }
            return result;
        }

        public OnCLoadingForDeliverySheetResult SendOnCLoadingForDeliverySheet(OnCLoadingForDeliverySheetRequest instance)
        {
            OnCLoadingForDeliverySheetResult result = new OnCLoadingForDeliverySheetResult();
            try
            {
                dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
                InfoTrack.NaqelAPI.App_Data.OnCloadingForD OnCloadingForDInstance = new App_Data.OnCloadingForD();

                OnCloadingForDInstance.CourierID = instance.CourierID;
                OnCloadingForDInstance.UserID = instance.UserID;
                OnCloadingForDInstance.IsSync = false;
                OnCloadingForDInstance.CTime = instance.CTime;
                OnCloadingForDInstance.PieceCount = instance.PieceCount;
                OnCloadingForDInstance.TruckID = instance.TruckID;
                OnCloadingForDInstance.WaybillCount = instance.WaybillCount;
                OnCloadingForDInstance.StationID = instance.StationID;

                dcHHDERPNaqelSE.OnCloadingForDs.InsertOnSubmit(OnCloadingForDInstance);
                dcHHDERPNaqelSE.SubmitChanges();

                if (instance.OnCLoadingForDeliverySheetWaybillList != null && instance.OnCLoadingForDeliverySheetWaybillList.Count > 0)
                {
                    for (int i = 0; i < instance.OnCLoadingForDeliverySheetWaybillList.Count; i++)
                    {
                        InfoTrack.NaqelAPI.App_Data.OnCLoadingForDWaybill OnCLoadingForDWaybillInstance = new App_Data.OnCLoadingForDWaybill();
                        OnCLoadingForDWaybillInstance.WaybillNo = instance.OnCLoadingForDeliverySheetWaybillList[i].WaybillNo;
                        OnCLoadingForDWaybillInstance.IsSync = false;
                        OnCLoadingForDWaybillInstance.OnCLoadingID = OnCloadingForDInstance.ID;
                        dcHHDERPNaqelSE.OnCLoadingForDWaybills.InsertOnSubmit(OnCLoadingForDWaybillInstance);
                    }
                }

                if (instance.OnCLoadingForDeliverySheetPieceList != null && instance.OnCLoadingForDeliverySheetPieceList.Count > 0)
                {
                    for (int i = 0; i < instance.OnCLoadingForDeliverySheetPieceList.Count; i++)
                    {
                        InfoTrack.NaqelAPI.App_Data.OnCLoadingForDDetail OnCLoadingForDDetailInstance = new App_Data.OnCLoadingForDDetail();
                        OnCLoadingForDDetailInstance.BarCode = instance.OnCLoadingForDeliverySheetPieceList[i].BarCode;
                        OnCLoadingForDDetailInstance.IsSync = false;
                        OnCLoadingForDDetailInstance.OnCLoadingForDID = OnCloadingForDInstance.ID;
                        dcHHDERPNaqelSE.OnCLoadingForDDetails.InsertOnSubmit(OnCLoadingForDDetailInstance);
                    }
                }

                dcHHDERPNaqelSE.SubmitChanges();

                result.HasError = false;
                result.ErrorMessage = "";
                result.ID = instance.ID;
                result.IsSync = true;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.IsSync = false;
            }
            return result;
        }

        public GetUserMEDataResult GetUserMEData(GetUserMEDataRequest instance)
        {
            GetUserMEDataResult result = new GetUserMEDataResult();
            InfoTrack.BusinessLayer.DContext.HHDDataContext dcHHD = new BusinessLayer.DContext.HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.ViwUserME instanceUserME = new BusinessLayer.DContext.ViwUserME();

            if (dcHHD.UserMEs.Where(P => P.EmployID == instance.EmployID && P.StatusID != 3).Count() > 0)
            {
                instanceUserME = dcHHD.ViwUserMEs.First(P => P.EmployID == instance.EmployID && P.StatusID != 3);
                string pass = GlobalVar.GV.security.Decrypt(instanceUserME.Password.ToArray());
                if (pass == instance.Passowrd)
                {
                    result.HasError = false;

                    result.ID = instanceUserME.ID;
                    result.EmployID = instance.EmployID;
                    result.Password = pass;
                    result.StationID = instanceUserME.StationID.Value;
                    result.RoleMEID = instanceUserME.RoleMEID;
                    result.StatusID = instanceUserME.StatusID;
                    result.EmployName = instanceUserME.Name;
                    result.EmployName = instanceUserME.FName;
                    result.MobileNo = instanceUserME.PhoneNo;
                    result.StationCode = instanceUserME.StationCode;
                    result.StationName = instanceUserME.StationName;
                    result.StationFName = instanceUserME.StationFName;
                }
                else
                {
                    result.HasError = true;
                    result.ErrorMessage = "Password is not correct";
                }
            }
            else
            {
                result.HasError = true;
                result.ErrorMessage = "There is No Employee with this ID.";
            }

            return result;
        }

        public CheckNewVersionResult CheckNewVersion(CheckNewVersionRequest instance)
        {
            CheckNewVersionResult result = new CheckNewVersionResult();
            dcMaster = new BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<InfoTrack.BusinessLayer.DContext.AppVersion> appVersionList = dcMaster.AppVersions.Where(P => P.VersionTypeID == instance.AppSystemSettingID).ToList();
            if (appVersionList.Last().Name != instance.CurrentVersion)
            {
                result.NewVersion = appVersionList.Last().Name;
                result.HasError = false;
                result.HasNewVersion = true;
                result.WhatIsNew = appVersionList.Last().WhatIsNew;
                result.IsMandatory = appVersionList.Last().IsMandatory;
            }
            else
            {
                result.HasError = false;
                result.HasNewVersion = false;
            }

            return result;
        }

        public List<ShipmentsForPickingResult> GetShipmentForPicking(GetShipmentForPickingRequest instance)
        {
            List<ShipmentsForPickingResult> result = new List<ShipmentsForPickingResult>();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcData = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.Employ employInstance = dcMaster.Employs.First(P => P.ID == instance.EmployID);
            List<InfoTrack.BusinessLayer.DContext.ViwShipmentForPicking> list = dcData.ViwShipmentForPickings.Where(P => P.DestinationStationID == employInstance.StationID).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                ShipmentsForPickingResult shipmentInstance = new ShipmentsForPickingResult();
                shipmentInstance.ID = list[i].ID;
                shipmentInstance.WaybillNo = list[i].WayBillNo;
                shipmentInstance.PiecesCount = list[i].PicesCount;

                shipmentInstance.Weight = list[i].Weight;
                shipmentInstance.BillingType = list[i].BillType;
                shipmentInstance.CODAmount = list[i].CODAmount.Value;
                shipmentInstance.ConsigneeName = list[i].ConsigneeName;
                shipmentInstance.ConsigneeFName = list[i].ConsigneeFName;
                shipmentInstance.PhoneNo = list[i].ConsigneePhoneNo;
                shipmentInstance.MobileNo = list[i].ConsigneeMobileNo;
                shipmentInstance.Address = list[i].FirstAddress;
                shipmentInstance.SecondLine = list[i].SecondAddress;
                shipmentInstance.Near = list[i].Near;
                shipmentInstance.LocationCoordinate.Latitude = list[i].Latitude;
                shipmentInstance.LocationCoordinate.Longitude = list[i].Longitude;

                result.Add(shipmentInstance);
            }

            return result;
        }

        public List<ShipmentsForDeliveryResult> GetShipmentsForDeliverySheet(ShipmentsForDeliverySheetRequest instance)
        {
            List<ShipmentsForDeliveryResult> result = new List<ShipmentsForDeliveryResult>();

            ShipmentsForDeliveryResult first = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first1 = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first2 = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first3 = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first4 = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first5 = new ShipmentsForDeliveryResult();
            ShipmentsForDeliveryResult first6 = new ShipmentsForDeliveryResult();

            first.WaybillNo = 60038400;
            first1.WaybillNo = 60038401;
            first2.WaybillNo = 60038402;
            first3.WaybillNo = 60038403;
            first4.WaybillNo = 60038404;

            result.Add(first);
            result.Add(first1);
            result.Add(first2);
            result.Add(first3);
            result.Add(first4);

            return result;
        }

        public CheckWaybillAlreadyPickedUpResult CheckWaybillAlreadyPickedUp(CheckWaybillAlreadyPickedUpRequest instance)
        {
            CheckWaybillAlreadyPickedUpResult result = new CheckWaybillAlreadyPickedUpResult();
            dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());

            if (dcHHDERPNaqelSE.PickUps.Where(P => P.WaybillNo == instance.WaybillNo.ToString()).Count() > 0)
                result.hasPickedUp = true;

            return result;
        }

        public BringPickUpDataResult BringPickUpData(BringPickUpDataRequest instance)
        {
            BringPickUpDataResult result = new BringPickUpDataResult();

            dcData = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.Waybills.Where(P => P.WayBillNo == instance.WaybillNo && P.IsCancelled == false).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.Waybill waybillInstance = dcData.Waybills.First(P => P.WayBillNo == instance.WaybillNo && P.IsCancelled == false);
                result.ClientID = waybillInstance.ClientID;
                result.OriginStationID = waybillInstance.OriginStationID;
                result.DestinationStationID = waybillInstance.DestinationStationID;
                result.PiecesCount = waybillInstance.PicesCount;
                result.Weight = waybillInstance.Weight;
            }
            else
                if (dcData.CustomerWayBills.Where(P => P.WayBillNo == instance.WaybillNo && P.StatusID != 3).Count() > 0)
            {
                InfoTrack.BusinessLayer.DContext.CustomerWayBill customerWaybillInstance = dcData.CustomerWayBills.First(P => P.WayBillNo == instance.WaybillNo && P.StatusID != 3);
                result.ClientID = customerWaybillInstance.ClientID;
                result.OriginStationID = customerWaybillInstance.OriginStationID;
                result.DestinationStationID = customerWaybillInstance.DestinationStationID;
                result.PiecesCount = customerWaybillInstance.PicesCount;
                result.Weight = customerWaybillInstance.Weight;
            }

            return result;
        }

        public List<BringMyRouteShipmentsResult> BringMyRouteShipments(BringMyRouteShipmentsRequest instance)
        {
            List<BringMyRouteShipmentsResult> result = new List<BringMyRouteShipmentsResult>();
            dcData = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            if (dcData.ViwRouteOptimizationDeliverySheets.Where(P => P.EmployID == instance.EmployID &&
             P.DeliverySheetDate.Day == DateTime.Now.Day &&
             P.DeliverySheetDate.Month == DateTime.Now.Month &&
             P.DeliverySheetDate.Year == DateTime.Now.Year).Count() > 0)//|| P.ID == 826498
            {
                List<InfoTrack.BusinessLayer.DContext.ViwRouteOptimizationDeliverySheet> list = dcData.ViwRouteOptimizationDeliverySheets.Where(P => (P.EmployID == instance.EmployID &&
               P.DeliverySheetDate.Day == DateTime.Now.Day &&
               P.DeliverySheetDate.Month == DateTime.Now.Month &&
               P.DeliverySheetDate.Year == DateTime.Now.Year)).ToList(); //|| P.ID == 826498

                for (int i = 0; i < list.Count(); i++)
                {
                    BringMyRouteShipmentsResult currentInstance = new BringMyRouteShipmentsResult();
                    currentInstance.OrderNo = 0;
                    currentInstance.ItemNo = list[i].WayBillNo.ToString();
                    currentInstance.TypeID = 1;
                    currentInstance.BillingType = list[i].BillingType;
                    currentInstance.CODAmount = list[i].CODAmount.HasValue ? list[i].CODAmount.Value : 0;
                    currentInstance.DeliverySheetID = list[i].ID;
                    currentInstance.Date = list[i].DeliverySheetDate;
                    //TODO
                    //currentInstance.ExpectedTime = DateTime.Now;
                    if (list[i].LatLong != null && list[i].LatLong.Length > 5)
                    {
                        string[] data = list[i].LatLong.Split(',');
                        if (data.Length > 1)
                        {
                            currentInstance.Latitude = data[0];
                            currentInstance.Longitude = data[1];
                        }
                    }
                    currentInstance.ClientID = list[i].ClientID.HasValue ? list[i].ClientID.Value : 0;
                    currentInstance.ClientName = list[i].ClientName;
                    currentInstance.ClientFName = list[i].ClientFName;
                    currentInstance.ClientAddressID = list[i].ClientAddressID.HasValue ? list[i].ClientAddressID.Value : 0;
                    currentInstance.ClientAddressPhoneNumber = list[i].ClientAddressPhoneNumber;
                    currentInstance.ClientAddressFirstAddress = list[i].ClientAddressFirstAddress;
                    currentInstance.ClientAddressSecondAddress = list[i].ClientAddressSecondAddress;
                    currentInstance.ClientAddressLocation = list[i].ClientAddressLocation;
                    currentInstance.ClientContactID = list[i].ClientContactID.HasValue ? list[i].ClientContactID.Value : 0;
                    currentInstance.ClientContactName = list[i].ClientContactName;
                    currentInstance.ClientContactFName = list[i].ClientContactFName;
                    currentInstance.ClientContactPhoneNumber = list[i].ClientContactPhoneNumber;
                    //currentInstance.ClientContactMobileNo = list[i].ClientContactMobileNo;
                    //currentInstance.ConsigneeID = list[i].consignee;
                    currentInstance.ConsigneeName = list[i].ConsigneeName;
                    currentInstance.ConsigneeFName = list[i].ConsigneeFName;
                    //currentInstance.ConsigneeDetailID = list[i].ConsigneeDetailID;
                    currentInstance.ConsigneePhoneNumber = list[i].ConsigneeDetailPhoneNumber;
                    currentInstance.ConsigneeFirstAddress = list[i].ConsigneeDetailFirstAddress;
                    currentInstance.ConsigneeSecondAddress = list[i].ConsigneeDetailSecondAddress;
                    currentInstance.ConsigneeNear = list[i].ConsigneeDetailNear;
                    currentInstance.ConsigneeMobile = list[i].ConsigneeDetailMobile;
                    //currentInstance.ConsigneeLatitude = list[i].ConsigneeLatitude;
                    //currentInstance.ConsigneeLongitude = list[i].ConsigneeLongitude;
                    currentInstance.Origin = list[i].Origin;
                    currentInstance.Destination = list[i].Destination;
                    currentInstance.PODNeeded = list[i].PODNeeded.HasValue ? list[i].PODNeeded.Value : false;
                    currentInstance.PODDetail = list[i].PODDetail;
                    currentInstance.PODTypeCode = list[i].PODTypeCode;
                    currentInstance.PODTypeName = list[i].PODTypeName;

                    result.Add(currentInstance);
                }
            }

            return result;
        }

        public CheckBeforeSubmitCODResult CheckBeforeSubmitCOD(CheckBeforeSubmitCODRequest instance)
        {
            CheckBeforeSubmitCODResult result = new CheckBeforeSubmitCODResult();

            //InfoTrack.BusinessLayer.DContext.DocumentDataDataContext dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            //List<InfoTrack.BusinessLayer.DContext.ViwWaybillDeliveredForDeliverySheet> waybillList =
            //        dcDocument.ViwWaybillDeliveredForDeliverySheets.Where(P => P.DeliverySheetID == instance.DeliverySheetID).ToList();
            //double TotalAmount = 0;
            //for (int i = 0; i < waybillList.Count; i++)
            //    TotalAmount += waybillList[i].CollectedAmount.Value;

            //double CourierAmount = instance.TotalCash + instance.TotalPOS;
            //if (CourierAmount > TotalAmount)
            //{
            //    result.Notes = "You have a problem";
            //    result.Notes += "\n\rIf you have more than one Delivery Sheet , so you have to submit each Delivery Sheet seprate.";
            //    result.Notes += "\n\r If you have Manual Delivery Sheet, so you have to submit it seprate.";
            //    result.Notes += "\n\rAmount different is : " + (CourierAmount - TotalAmount).ToString();

            //    //Check for Missing Scan.
            //    for (int i = 0; i < waybillList.Count; i++)
            //    {
            //        if (dcDocument.ViwDeliveries.Where(P => P.WaybillID == waybillList[i].WaybillID && P.DeliveryStatusID == 5).Count() <= 0)
            //            result.Notes += "\n\rThere is no Delivery Scan for this waybill " + waybillList[i].WayBillNo.ToString();
            //    }
            //}
            //else
            //    if (CourierAmount < TotalAmount)
            //{
            //    result.Notes = "You have a problem";
            //    result.Notes += "\n\rAmount different is : " + (CourierAmount - TotalAmount).ToString();
            //    //Check for Missing Scan.
            //    for (int i = 0; i < waybillList.Count; i++)
            //    {
            //        if (dcDocument.ViwDeliveries.Where(P => P.WaybillID == waybillList[i].WaybillID && P.DeliveryStatusID == 5).Count() <= 0)
            //            result.Notes += "\n\rThere is no Delivery Scan for this waybill " + waybillList[i].WayBillNo.ToString();
            //    }
            //}
            //else
            //    result.Notes = "O.K";

            return result;
        }

        public List<CheckPendingCODResult> CheckPendingCOD(CheckPendingCODRequest instance)
        {
            List<CheckPendingCODResult> result = new List<CheckPendingCODResult>();
            dcData = new DocumentDataDataContext();
            List<InfoTrack.BusinessLayer.DContext.rpPendingCODCash> pendingList = dcData.rpPendingCODCashes.Where(P => P.CourierID == instance.EmployID &&
                                                                                        P.DeliveryDate >= new DateTime(2017, 1, 1) &&
                                                                                        P.DeliveryDate != null).OrderBy(P => P.DeliveryDate).ToList();
            for (int i = 0; i < pendingList.Count; i++)
            {
                if ((pendingList[i].AmountForClient + pendingList[i].NetCharge) - pendingList[i].TotalPaid > 1)
                {
                    CheckPendingCODResult checkCODPending = new CheckPendingCODResult();
                    checkCODPending.WaybillNo = pendingList[i].WayBillNo;

                    if (pendingList[i].BillingTypeID == 2 || pendingList[i].BillingTypeID == 6)
                        checkCODPending.Amount = pendingList[i].NetCharge - (pendingList[i].TotalPaid.HasValue ? pendingList[i].TotalPaid.Value : 0);
                    else
                        checkCODPending.Amount = pendingList[i].CollectedAmount.Value - (pendingList[i].TotalPaid.HasValue ? pendingList[i].TotalPaid.Value : 0);
                    checkCODPending.DeliveryDate = pendingList[i].DeliveryDate.Value;
                    result.Add(checkCODPending);
                }
            }

            return result;
        }

        public List<OptimizedOutOfDeliveryShipmentResult> OptimizedOutOfDeliveryShipment(OptimizedOutOfDeliveryShipmentRequest instance)
        {
            List<OptimizedOutOfDeliveryShipmentResult> result = new List<OptimizedOutOfDeliveryShipmentResult>();
            RouteOptimizationDataContext routeOptimizationDataContext = new RouteOptimizationDataContext();
            List<InfoTrack.BusinessLayer.DContext.ViewOutforDelivery> list = routeOptimizationDataContext.ViewOutforDeliveries.Where(P => P.EmployeeID == instance.EmployID.ToString() &&
            P.OFDDate.Value.Year == DateTime.Now.Year &&
            P.OFDDate.Value.Month == DateTime.Now.Month &&
            P.OFDDate.Value.Day == DateTime.Now.Day).ToList();
            for (int i = 0; i < list.Count(); i++)
            {
                OptimizedOutOfDeliveryShipmentResult optimizedOutOfDeliveryShipmentResult = new OptimizedOutOfDeliveryShipmentResult();
                optimizedOutOfDeliveryShipmentResult.WaybillNo = list[i].WaybillNo;
                result.Add(optimizedOutOfDeliveryShipmentResult);
            }

            return result;
        }

        public RouteOptimizationDataType.DefaultResult AddCallHistory(NewCallRequest instance)
        {
            DefaultResult result = new DefaultResult();
            if(instance.EmployID <= 0)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Employ No";
                return result;
            }

            if (instance.MobileNo.Length <= 5)
            {
                result.HasError = true;
                result.ErrorMessage = "Wrong Mobile No";
                return result;
            }

            DocumentDataDataContext dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            InfoTrack.BusinessLayer.DContext.CallingHistory callingHistory = new CallingHistory();
            callingHistory.EmployID = instance.EmployID;
            callingHistory.MobileNo = instance.MobileNo;
            callingHistory.Date = DateTime.Now;

            dc.CallingHistories.InsertOnSubmit(callingHistory);
            dc.SubmitChanges();

            result.HasError = false;
            return result;
        }

        //public DefaultResult AddCallHistory(string EmployID, string MobileNo)
        //{
        //    DefaultResult result = new DefaultResult();

        //    DocumentDataDataContext dc = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        //    InfoTrack.BusinessLayer.DContext.CallingHistory callingHistory = new CallingHistory();
        //    callingHistory.EmployID = Convert.ToInt32(EmployID);
        //    callingHistory.MobileNo = MobileNo;
        //    callingHistory.Date = DateTime.Now;

        //    dc.CallingHistories.InsertOnSubmit(callingHistory);
        //    dc.SubmitChanges();

        //    result.HasError = false;

        //    return result;
        //}

        public GetLoadTypeResult GetLoadType(GetLoadTypeRequest instance)
        {
            GetLoadTypeResult result = new GetLoadTypeResult();
            if (instance.ClientID > 0 )
            {

            }
            else
            {

            }
            return result;
        }

        public SendCheckPointResult SendCheckPoint(SendCheckPointRequest instance)
        {
            SendCheckPointResult result = new SendCheckPointResult();

            try
            {
                dcHHDERPNaqelSE = new App_Data.HHDDataContext(GlobalVar.GV.GetInfoTrackSEConnection());
                InfoTrack.NaqelAPI.App_Data.CheckPoint checkPointInstance = new App_Data.CheckPoint();

                checkPointInstance.Date = instance.Date;
                checkPointInstance.CheckPointTypeID = instance.CheckPointTypeID;
                checkPointInstance.EmployID = instance.EmployID;
                checkPointInstance.AppVersion = instance.AppVersion;
                checkPointInstance.Latitude = instance.Latitude;
                checkPointInstance.Longitude= instance.Longitude;
                checkPointInstance.IsSync = false;
                checkPointInstance.StatusID = 1;

                dcHHDERPNaqelSE.CheckPoints.InsertOnSubmit(checkPointInstance);
                dcHHDERPNaqelSE.SubmitChanges();

                for (int i = 0; i < instance.CheckPointWaybillDetailsRequestList.Count; i++)
                {
                    InfoTrack.NaqelAPI.App_Data.CheckPointWaybillDetail checkPointDetail = new App_Data.CheckPointWaybillDetail();
                    checkPointDetail.WaybillNo = instance.CheckPointWaybillDetailsRequestList[i].WaybillNo;
                    checkPointDetail.IsSync = false;
                    checkPointDetail.CheckPointID = checkPointInstance.ID;
                    checkPointDetail.StatusID = 1;
                    dcHHDERPNaqelSE.CheckPointWaybillDetails.InsertOnSubmit(checkPointDetail);
                }
                dcHHDERPNaqelSE.SubmitChanges();

                result.HasError = false;
                result.ErrorMessage = "";
                result.ID = instance.ID;
                result.IsSync = true;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.ErrorMessage = ex.Message;
                result.IsSync = false;
            }

            return result;
        }

        public List<CheckPointTypeResult> GetCheckPointTypeFromServer(GetCheckPointTypeRequest instance)
        {
            List<CheckPointTypeResult> result = new List<CheckPointTypeResult>();

            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List<CheckPointType> list = new List<CheckPointType>();
            list = dcMaster.CheckPointTypes.Where(P => P.StatusID == 1 ).ToList();
            for (int i = 0; i < list.Count; i++)
                result.Add(new CheckPointTypeResult(list[i].ID, list[i].Name, list[i].FName));

            return result;
        }
    }
}