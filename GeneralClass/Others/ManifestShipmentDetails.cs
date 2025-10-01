using InfoTrack.NaqelAPI.GeneralClass.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Text.RegularExpressions;
using InfoTrack.BusinessLayer.DContext;

namespace InfoTrack.NaqelAPI
{
    public class ManifestShipmentDetails
    {
        //private App_Data.DataDataContext dc;
        public ManifestShipmentDetails()
        {
            CreateBooking = false;
            GeneratePiecesBarCodes = false;
        }

        public ClientInformation ClientInfo = new ClientInformation();
        public ConsigneeInformation ConsigneeInfo = new ConsigneeInformation();
        public CommercialInvoice _CommercialInvoice = new CommercialInvoice();

        [System.ComponentModel.DefaultValue(0)]
        public int CurrenyID = 0;


        public int BillingType;
        public int PicesCount = 0;

        public double Weight = 0;
        internal int OriginStationID;
        internal int DestinationStationID;
        internal int? ODAOriginStationID = null;
        internal int? ODADestinationStationID = null;
        //public bool PickUpFromSeller = false;

        public string DeliveryInstruction = "";
        public double CODCharge = 0;

        [System.ComponentModel.DefaultValue(false)]
        public bool CreateBooking;
        public string PickUpPoint { get; set; } = "";

        [System.ComponentModel.DefaultValue(false)]
        public bool isRTO;

        [System.ComponentModel.DefaultValue(true)]
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
        public double VolumetricWeight = 0.1;

        [System.ComponentModel.DefaultValue(0)]
        public double InsuredValue = 0;

        //[System.ComponentModel.DefaultValue(false)]
        //public bool IsInsurance = false;

        public string Reference1 = "";
        public string Reference2 = "";
        public string Reference3 = "";

        //[System.ComponentModel.DefaultValue(0)]
        //public double CustomDutyAmount = 0;

        [System.ComponentModel.DefaultValue(0)]
        public double GoodsVATAmount = 0;

        [System.ComponentModel.DefaultValue(false)]
        public bool IsCustomDutyPayByConsignee;

        public List<ItemPieces> Itempieceslist;

        public class ItemPieces
        {
            public string PieceBarcode = "";
            public string PieceDescription = "";

        }

        internal Result IsWaybillDetailsValid(ManifestShipmentDetails _ManifestShipmentDetails, int? PharmaClientID = 0, bool? IsCourierLoadType = false)
        {
            string loadtypeB2B = System.Configuration.ConfigurationManager.AppSettings["B2BLoadtypeIDs"].ToString();
            List<int> b2BLoadtypes = loadtypeB2B.Split(',').Select(Int32.Parse).ToList();

            string SplClient = System.Configuration.ConfigurationManager.AppSettings["SplClientID"].ToString();
            List<int> splList = SplClient.Split(',').Select(Int32.Parse).ToList();

            string loadtypeB2BINT = System.Configuration.ConfigurationManager.AppSettings["INTB2BLoadtypeIDs"].ToString();
            List<int> INTloadtypeB2B = loadtypeB2BINT.Split(',').Select(Int32.Parse).ToList();
            Result result = new Result();
            result.HasError = true;
            ServiceTypeID = GlobalVar.GV.GetServiceTypeID(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.LoadTypeID);


            #region BillingType & COD Validation
            int[] acceptBillingTypeArray = { 1, 3, 5 }; // PrePaid, ExternalBilling, COD
            if (!acceptBillingTypeArray.Contains(_ManifestShipmentDetails.BillingType))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return result;
            }

            if (_ManifestShipmentDetails.CODCharge < 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCODCharge");
                return result;
            }

            if (_ManifestShipmentDetails.BillingType == 1 && _ManifestShipmentDetails.CODCharge > 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErBillingTypeID"); // must add to common Error Messages
                return result;
            }

            if (_ManifestShipmentDetails.BillingType == 5 && _ManifestShipmentDetails.CODCharge <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCODCharge");
                return result;
            }

            if (!GlobalVar.GV.IsBillingCorrect(_ManifestShipmentDetails.BillingType, _ManifestShipmentDetails.ClientInfo.ClientID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return result;
            }
            #endregion

            #region LoadType Validation
            int[] notAcceptLoadTypeArray = { 66, 136, 204, 206 }; // Domestic ASR, International ASR, Drop Off- ASR , Drop off -ASR int'
            int[] CBUServiceID = { 7, 8 }; // Domestic ASR, International ASR, Drop Off- ASR , Drop off -ASR int'

            if (_ManifestShipmentDetails.LoadTypeID <= 0 || notAcceptLoadTypeArray.Contains(_ManifestShipmentDetails.LoadTypeID))
            {
                result.Message = "Please provide a correct LoadTypeID.";
                return result;
            }

            #region Parcel Locker validation
            if (_ManifestShipmentDetails.LoadTypeID == 259) // 259  Parcel Locker
            {
                if (string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.ParcelLockerMachineID))
                {
                    result.Message = "Please provide a correct Parcel Locker Machine ID.";
                    return result;
                }

                if (!Regex.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.ParcelLockerMachineID.Trim(), @"^[0-9]{1,5}$"))
                {
                    result.Message = "Please provide a correct Parcel Locker Machine ID.";
                    return result;
                }

                // validate parcel locker machine id
                if(!GlobalVar.GV.CheckPLMachineID(_ManifestShipmentDetails.ConsigneeInfo.ParcelLockerMachineID))
                {
                    result.Message = "Invalid Parcel Locker Machine ID.";
                    return result;
                }
            }
            #endregion

            #region SPL Office validation
            if (_ManifestShipmentDetails.LoadTypeID == 285) // 285  SPL Office
            {
                if (string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.SPLOfficeID))
                {
                    result.Message = "Please provide a correct SPL Office ID.";
                    return result;
                }

                if (!Regex.IsMatch(_ManifestShipmentDetails.ConsigneeInfo.SPLOfficeID.Trim(), @"^[0-9]{1,7}$"))
                {
                    result.Message = "Please provide a correct SPL Office ID.";
                    return result;
                }

                // validate parcel locker machine id
                if (!GlobalVar.GV.CheckSPLOfficeID(_ManifestShipmentDetails.ConsigneeInfo.SPLOfficeID))
                {
                    result.Message = "Invalid SPL Office ID.";
                    return result;
                }
            }
            #endregion

            if (_ManifestShipmentDetails.ClientInfo.ClientID != 1024600 && !GlobalVar.GV.IsLoadTypeCorrect(_ManifestShipmentDetails.ClientInfo, _ManifestShipmentDetails.LoadTypeID))
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
                return result;
            }

            // Incoterm changes are for international service only --Make a condition for access this part once it's international order.

            if (_ManifestShipmentDetails.ServiceTypeID == 7 && INTloadtypeB2B.Contains(_ManifestShipmentDetails.LoadTypeID))
            {
                if (string.IsNullOrWhiteSpace(_ManifestShipmentDetails.Incoterm))
                {
                    _ManifestShipmentDetails.Incoterm = "DDU";
                    _ManifestShipmentDetails.IsCustomDutyPayByConsignee = true;
                }

                if (_ManifestShipmentDetails.Incoterm == "DAP")
                {
                    _ManifestShipmentDetails.Incoterm = "DDU";
                    _ManifestShipmentDetails.IsCustomDutyPayByConsignee = true;
                }

                if (!GlobalVar.GV.IsCorrectIncoterm(_ManifestShipmentDetails.Incoterm))
                {
                    result.Message = "Please provide Valid Incoterm Value .";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.Email))
                {
                    result.Message = "Please provide Consignee's Email.";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(_ManifestShipmentDetails.ConsigneeInfo.BuildingNo))
                {
                    result.Message = "Please provide a Consignee's BuildingNo.";
                    return result;
                }
            }



            InfoTrack.BusinessLayer.DContext.MastersDataContext dc = new InfoTrack.BusinessLayer.DContext.MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if ((string.IsNullOrWhiteSpace(_ManifestShipmentDetails.Incoterm) || !GlobalVar.GV.IsCorrectIncoterm(_ManifestShipmentDetails.Incoterm))
             && !INTloadtypeB2B.Contains(_ManifestShipmentDetails.LoadTypeID)
              && (_ManifestShipmentDetails.ServiceTypeID == 7 || _ManifestShipmentDetails.LoadTypeID == 56)) // * FRD : If blank & B2C Load Type ID default =DDP
            {
                _ManifestShipmentDetails.Incoterm = "DDP";
                _ManifestShipmentDetails.IsCustomDutyPayByConsignee = false;
            }


            #endregion

            #region Check city code
            var OriginCityCode = _ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode;
            var OriginCountryCode = _ManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode;

            var DestinationCityCode = _ManifestShipmentDetails.ConsigneeInfo.CityCode;
            var DestinationCountryCode = _ManifestShipmentDetails.ConsigneeInfo.CountryCode;

            if (string.IsNullOrWhiteSpace(OriginCityCode) || string.IsNullOrWhiteSpace(OriginCountryCode))
            {
                result.Message = "Please provide a valid Origin CityCode";
                return result;
            }

            if (GlobalVar.GV.ISCityCodeValid(OriginCityCode, OriginCountryCode, IsCourierLoadType) == "")
            {
                result.Message = "Please provide a valid Origin CityCode and CountryCode";
                return result;
            }

            if (string.IsNullOrWhiteSpace(DestinationCityCode) || string.IsNullOrWhiteSpace(DestinationCountryCode))
            {
                result.Message = "Please provide a valid Destination CityCode";
                return result;
            }

            if (GlobalVar.GV.ISCityCodeValid(DestinationCityCode, DestinationCountryCode, IsCourierLoadType) == "")
            {
                result.Message = "Please provide a valid Destination CityCode and CountryCode";
                return result;
            }

            if (_ManifestShipmentDetails.DeclareValue < 0)
            {
                result.Message = "Declare Value should not be less than 0";
                return result;
            }

            if (OriginCountryCode != DestinationCountryCode && _ManifestShipmentDetails.DeclareValue <= 0)
            {
                result.Message = "Declare Value should be greater than 0";
                return result;
            }
            if (_ManifestShipmentDetails.ClientInfo.ClientID == 9017663 && DeclareValue <= 0)
            {
                result.Message = "Please pass DeclaredValue";
                return result;
            }
            //Solve FreshProduct wrong manifest issue by our end.

            if ((OriginCountryCode == DestinationCountryCode) && _ManifestShipmentDetails.ClientInfo.ClientID == 9021048)
            {
                _ManifestShipmentDetails.ClientInfo.ClientID = 9022745;
                _ManifestShipmentDetails.LoadTypeID = 36;
                //check this
                _ManifestShipmentDetails.ServiceTypeID = 8;
            }



            if (_ManifestShipmentDetails.LoadTypeID == 56 && (OriginCountryCode.Trim().ToLower() != DestinationCountryCode.Trim().ToLower()))
            {
                result.Message = "The LoadtypeID 56 is for Same origin and destination country Code";
                return result;
            }

            //check when using wrong Loadtype based on Origin & destination country
            List<int> _domesticServiceTypeID = new List<int>() { 8 };
            if ((OriginCountryCode.Trim().ToLower() != DestinationCountryCode.Trim().ToLower()) && _ManifestShipmentDetails.LoadTypeID != 56
                && _domesticServiceTypeID.Contains(_ManifestShipmentDetails.ServiceTypeID))
            {
                result.Message = "Wrong LoadTypeID or Origin/Destination CountryCode.";
                return result;
            }



            //SPL Validation -- add new logic 
            //if (_ManifestShipmentDetails.ClientInfo.ClientID == 9022477 && _ManifestShipmentDetails.LoadTypeID != 203)
            //{
            //    result.Message = "Please provide a valid origin CityCode and Destination CityCode. ";
            //    return result;
            //}

            //handle the error if account is disabled. 
            if (_ManifestShipmentDetails.ClientInfo.ClientID == 9022477 && _ManifestShipmentDetails.LoadTypeID == 203)
            {
                _ManifestShipmentDetails.ClientInfo.ClientID = 9026333;
            }

            //if (OriginCityCode != DestinationCityCode && ClientInfo.ClientContact)
            //{=
            //    result.HasError = true;
            //    result.Message = "Declare Value should be greater than 0";
            //    return result;
            //}
            ////if (_ManifestShipmentDetails.ClientInfo.ClientAddress.CityCode != _ManifestShipmentDetails.ConsigneeInfo.CityCode && _ManifestShipmentDetails.ClientInfo.ClientID = 9020077)
            ////{

            ////}
            #endregion

            if (_ManifestShipmentDetails.PicesCount <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErPicesCount");
                return result;
            }

            if (splList.Contains(_ManifestShipmentDetails.ClientInfo.ClientID))
            {

                if (!GlobalVar.GV.isValidPcs(_ManifestShipmentDetails.PicesCount, _ManifestShipmentDetails.Itempieceslist, _ManifestShipmentDetails.LoadTypeID))
                {

                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErPicesCount");
                    return result;

                }

                else
                {
                    var splMaster = _ManifestShipmentDetails.RefNo;
                    // save it in log table for SPL Barcode :
                    var dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                    //splCustomerBarCode instance = new splCustomerBarCode();
                    // Create a list to hold all the instances
                    var instances = new List<splCustomerBarCode>();
                    if (_ManifestShipmentDetails.LoadTypeID != 116)
                    {
                        var instance = new splCustomerBarCode
                        {
                            splMasterPieceBarCode = splMaster,
                            splPieceBarCode = splMaster,
                            PieceDescription = "",
                            StatusID = 1,
                        };

                        // Add the instance to the list
                        instances.Add(instance);
                    }
                   

                    foreach (var item in _ManifestShipmentDetails.Itempieceslist)
                    {
                         var instance = new splCustomerBarCode
                        {
                            splMasterPieceBarCode = splMaster,
                            splPieceBarCode = item.PieceBarcode,
                            PieceDescription = item.PieceDescription,
                            StatusID = 1,
                        };

                        // Add the instance to the list
                        instances.Add(instance);
                    }

                    dcMaster.splCustomerBarCodes.InsertAllOnSubmit(instances);
                    dcMaster.SubmitChanges();

                }



            }
            // check if valid piece 





            if (_ManifestShipmentDetails.Weight <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWeight");
                return result;
            }

            if (_ManifestShipmentDetails.DeclareValue > 0 && _ManifestShipmentDetails.CurrenyID <= 0)
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCurrency");
                return result;
            }
            //ignore DV validation for express loadtypes and for SPL LM
            if (_ManifestShipmentDetails.DeclareValue > 0
                && _ManifestShipmentDetails._CommercialInvoice.InvoiceNo == ""
                && _ManifestShipmentDetails.ServiceTypeID != 4
                && _ManifestShipmentDetails.ClientInfo.ClientID != 9022477) // must remove this validation
            {
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCommercialInvoice");
                return result;
            }

            List<int> _internationalServiceTypeID = new List<int>() { 6, 7 };
            if (OriginCountryCode == DestinationCountryCode &&
                _internationalServiceTypeID.Contains(_ManifestShipmentDetails.ServiceTypeID))
            {
                result.Message = "Wrong LoadTypeID or Origin/Destination CountryCode.";
                return result;
            }
            #region Commented before
            //if (!GlobalVar.GV.IsRefNoCorrect( _ManifestShipmentDetails.ClientInfo.ClientID , RefNo))
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongRefNo");
            //    return result;
            //}

            //if (GlobalVar.GV.CheckCurrency(CurrenyID))
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCurrency");
            //    return result;
            //}

            //if (_ManifestShipmentDetails.OriginStationID <= 0)
            //{
            //    _ManifestShipmentDetails.OriginStationID = GlobalVar.GV.GetStationByCity(_ManifestShipmentDetails.ClientInfo.ClientAddress.CityID);
            //    if (_ManifestShipmentDetails.OriginStationID <= 0)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCountryCode");
            //        return result;
            //    }
            //}

            //if (!GlobalVar.GV.IsStationCorrect(_ManifestShipmentDetails.OriginStationID))
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCountryCode");
            //    return result;
            //}

            //if (_ManifestShipmentDetails.DestinationStationID <= 0)
            //{
            //    _ManifestShipmentDetails.DestinationStationID = GlobalVar.GV.GetStationByCity(_ManifestShipmentDetails.ConsigneeInfo.CityID);
            //    if (_ManifestShipmentDetails.DestinationStationID <= 0)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCountryCode");
            //        return result;
            //    }
            //}

            //if (!GlobalVar.GV.IsStationCorrect(_ManifestShipmentDetails.DestinationStationID))
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCountryCode");
            //    return result;
            //}

            //if (_ManifestShipmentDetails.ContactPerson == "")
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErContactPerson");
            //    return result;
            //}

            //if (_ManifestShipmentDetails.ContactNumber == "")
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErContactNumber");
            //    return result;
            //}

            //if (_ManifestShipmentDetails.ODAOriginID.HasValue)
            //{
            //    if (CheckODAStation(_ManifestShipmentDetails.ODAOriginID.Value, _ManifestShipmentDetails.OriginStationID) == false)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongODAAssigning");
            //        return result;
            //    }
            //}

            //if (_ManifestShipmentDetails.ODADestinationID.HasValue)
            //{
            //    if (CheckODAStation(_ManifestShipmentDetails.ODADestinationID.Value, _ManifestShipmentDetails.DestinationStationID) == false)
            //    {
            //        result.HasError = true;
            //        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongODAAssigning");
            //        return result;
            //    }
            //}
            #endregion

            //if (_ManifestShipmentDetails.CreateBooking && (_ManifestShipmentDetails.PickUpPoint == null || _ManifestShipmentDetails.PickUpPoint.Trim() == ""))
            //{
            //    result.HasError = true;
            //    result.Message = "PickUpPoint is required when create booking selected, please pass your warehouse address.";
            //    return result;
            //}
            result.HasError = false;
            return result;

        }

        //internal void WriteToFile(ManifestShipmentDetails _manifest)
        //{
        //    string Data = "";
        //    Data += _manifest.BillingType.ToString();
        //    Data += _manifest.CODCharge.ToString();

        //    //System.IO.File.WriteAllText(

        //    //foreach (System.Reflection.FieldInfo info in _manifest)
        //    //{
        //    //    Console.WriteLine(info.Name + ": " +
        //    //       info.GetValue(_manifest).ToString());
        //    //}
        //}

        //private bool CheckODAStation(int ODAStationID, int StationID)
        //{
        //    bool result = false;

        //    App_Data.DataDataContext dc = new App_Data.DataDataContext(GlobalVar.GV.GetERPNaqelConnection());
        //    if (dc.ViwODAStationAPIs.Where(P => P.ID == ODAStationID && P.StationID == StationID).Count() > 0)
        //        result = true;

        //    return result;
        //}

    }

    public class BulletDlv_Req_Details
    {
        //private App_Data.DataDataContext dc;
        public BulletDlv_Req_Details()
        {
            CreateBooking = true;
            GeneratePiecesBarCodes = true;
        }

        public ClientInformation ClientInfo = new ClientInformation();
        public BulletConsigneeInformation ConsigneeInfo = new BulletConsigneeInformation();

        [System.ComponentModel.DefaultValue(0)]
        public int CurrenyID = 0;


        public int BillingType;
        public int PicesCount = 0;
        public double Weight = 0;
        internal int OriginStationID;
        internal int DestinationStationID;
        internal int? ODAOriginStationID = null;
        internal int? ODADestinationStationID = null;

        public string DeliveryInstruction = "";
        public double CODCharge = 0;
        [System.ComponentModel.DefaultValue(true)]
        public bool CreateBooking;
        // public string PickUpPoint { get; set; } = "";
        [System.ComponentModel.DefaultValue(true)]
        public bool GeneratePiecesBarCodes;
        public int LoadTypeID = 0;
        internal int ServiceTypeID = 0;
        internal int WaybillNo = 0;
        public string GoodDesc = "";
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
        public double VolumetricWeight = 0.1;

        [System.ComponentModel.DefaultValue(0)]
        public double InsuredValue = 0;

        public string Reference1 = "";

        public string Reference2 = "";



        //internal Result IsWaybillDetailsValid(BulletDlv_Req_Details _ManifestShipmentDetailsBD, int? PharmaClientID = 0, bool? IsCourierLoadType = false)
        //{

        //    //Result result = new Result();
        //    //result.HasError = true;
        //    //ServiceTypeID = GlobalVar.GV.GetServiceTypeID(_ManifestShipmentDetailsBD.ClientInfo, _ManifestShipmentDetailsBD.LoadTypeID);






        //    result.HasError = false;
        //    return result;

        //}


    }

}