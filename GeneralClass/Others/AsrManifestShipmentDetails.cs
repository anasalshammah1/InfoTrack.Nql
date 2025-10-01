using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class AsrManifestShipmentDetails
    {//private App_Data.DataDataContext dc;
        public AsrManifestShipmentDetails()
        {
        }

        public ClientInformation ClientInfo = new ClientInformation();
        public ConsigneeInformation ConsigneeInfo = new ConsigneeInformation();
        public CommercialInvoice _CommercialInvoice = new CommercialInvoice();

        internal double CODCharge = 0;
        internal bool IsCOD = false;
        internal int OriginStationID;
        internal int DestinationStationID;
        internal string OriginCityCode;
        internal string DestinationCityCode;
        internal int? ODAOriginStationID = null;
        internal int? ODADestinationStationID = null;
        internal int ServiceTypeID = 0;
        internal bool CreateBooking = false;
        internal bool isRTO = false;
        internal bool GeneratePiecesBarCodes = false;
        internal DateTime? PromisedDeliveryDateFrom = null;
        internal DateTime? PromisedDeliveryDateTo = null;
        [System.ComponentModel.DefaultValue(1)]
        internal double Width = 1;
        [System.ComponentModel.DefaultValue(1)]
        internal double Length = 1;
        [System.ComponentModel.DefaultValue(1)]
        internal double Height = 1;
        [System.ComponentModel.DefaultValue(0.1)]
        internal double VolumetricWeight = 0.1;

        public int PicesCount = 1;
        public int WaybillNo = 0;
        public int OriginWaybillNo = 0;
        public string RefNo = "";
        [System.ComponentModel.DefaultValue(1)]
        public int BillingType = 1;
        public int LoadTypeID = 0;
        public double DeclareValue = 0;
        public int CurrencyID = 4;
        public DateTime PickUpDate = DateTime.Now.AddDays(DateTime.Now.DayOfWeek == DayOfWeek.Thursday ? 2 : 1);
        public WaybillSurcharge WaybillSurcharge = null;
        public string GoodDesc = "";
        public string DeliveryInstruction = "";
        public string Latitude = "";
        public string Longitude = "";
        [System.ComponentModel.DefaultValue(0)]
        public double Weight = 0;
        public double InsuredValue = 0;
        public string Reference1 = "";
        public string Reference2 = "";
        public double GoodsVATAmount = 0;
        [System.ComponentModel.DefaultValue(false)]
        public bool IsCustomDutyPayByConsignee = false;


        internal Result IsWaybillDetailsValid(AsrManifestShipmentDetails _AsrManifestShipmentDetails, bool IsCourierLoadType = false)
        {
            Result result = new Result();

            //result = _AsrManifestShipmentDetails.ConsigneeInfo.CheckConsigneeInfo(_AsrManifestShipmentDetails.ConsigneeInfo, _AsrManifestShipmentDetails.ClientInfo);
            //if (result.HasError)
            //    return result;

            #region Check value not small than 0
            if (_AsrManifestShipmentDetails.RefNo.Trim().Length == 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Invalid RefNo");
                return result;
            }

            if (_AsrManifestShipmentDetails.PicesCount <= 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErPicesCount");
                return result;
            }

            if (_AsrManifestShipmentDetails.Weight <= 0 || _AsrManifestShipmentDetails.Length <= 0
                 || _AsrManifestShipmentDetails.Height <= 0 || _AsrManifestShipmentDetails.Width <= 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWeightOrSize");
                return result;
            }

            if (_AsrManifestShipmentDetails.DeclareValue < 0 || _AsrManifestShipmentDetails.CurrencyID <= 0)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErDeclareValueOrCurrency");
                return result;
            }

            if (_AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Count() <= 0 || _AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Count() > 2)
            {
                result.HasError = true;
                result.Message = "ErrSurcharge";
                return result;
            }
            #endregion

            #region Check bill type
            int[] acceptBillingTypeArray = { 1, 3, 5 }; // PrePaid, ExternalBilling, COD
            if (!acceptBillingTypeArray.Contains(_AsrManifestShipmentDetails.BillingType))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return result;
            }

            if (!GlobalVar.GV.IsBillingCorrect(BillingType, _AsrManifestShipmentDetails.ClientInfo.ClientID))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongBillingType");
                return result;
            }

            //if (_AsrManifestShipmentDetails.CODCharge < 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCODCharge");
            //    return result;
            //}

            //if (_AsrManifestShipmentDetails.BillingType == 1 && _AsrManifestShipmentDetails.CODCharge > 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErBillingTypeID"); // must add to common Error Messages
            //    return result;
            //}

            //if (_AsrManifestShipmentDetails.BillingType == 5 && _AsrManifestShipmentDetails.CODCharge <= 0)
            //{
            //    result.HasError = true;
            //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCODCharge");
            //    return result;
            //}
            #endregion

            #region Check load type
           // int[] acceptLoadTypeArray = { 66, 136, 204 ,206 , 284 }; // Domestic ASR, International ASR, Drop Off- ASR
            string acceptLoadTypeArray = System.Configuration.ConfigurationManager.AppSettings["ASRLoadTypes"].ToString();

            if (!acceptLoadTypeArray.Contains(_AsrManifestShipmentDetails.LoadTypeID.ToString()))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
                return result;
            }

            if (_AsrManifestShipmentDetails.ClientInfo.ClientID != 1024600 && !GlobalVar.GV.IsLoadTypeCorrect(_AsrManifestShipmentDetails.ClientInfo, _AsrManifestShipmentDetails.LoadTypeID))
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongLoadType");
                return result;
            }

            #endregion

            #region Check city code
            var OriginCityCode = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode;
            var OriginCountryCode = _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode;

            var DestinationCityCode = _AsrManifestShipmentDetails.ConsigneeInfo.CityCode;
            var DestinationCountryCode = _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode;


            if (string.IsNullOrEmpty(OriginCityCode) || string.IsNullOrEmpty(OriginCountryCode))
            {
                result.HasError = true;
                result.Message = "Please provide a valid Origin CityCode";
                return result;
            }

            if (GlobalVar.GV.ISCityCodeValid(OriginCityCode, OriginCountryCode) == "")
            {
                result.HasError = true;
                result.Message = "Please provide a valid Origin CityCode and CountryCode";
                return result;
            }

            if (string.IsNullOrEmpty(DestinationCityCode) || string.IsNullOrEmpty(DestinationCountryCode))
            {
                result.HasError = true;
                result.Message = "Please provide a valid Destination CityCode";
                return result;
            }

            if (GlobalVar.GV.ISCityCodeValid(DestinationCityCode, DestinationCountryCode) == "")
            {
                result.HasError = true;
                result.Message = "Please provide a valid Destination CityCode and CountryCode";
                return result;
            }

            if (OriginCountryCode != DestinationCountryCode) // International Delivery
            {
                //int[] IntacceptLoadTypeArray = { 136, 206 }; //International ASR, Drop Off- ASR
                string IntacceptLoadTypeArray = System.Configuration.ConfigurationManager.AppSettings["INTASRLoadTypes"].ToString();


                if (_AsrManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() == 0)
                {
                    result.HasError = true;
                    result.Message = "Please provide valid CommercialInvoiceDetail data!";
                    return result;
                }

                if (_AsrManifestShipmentDetails.DeclareValue <= 0)
                {
                    result.HasError = true;
                    result.Message = "Declared value should be greater than 0 for packages to different country!";
                    return result;
                }

                if (_AsrManifestShipmentDetails.CurrencyID <= 0)
                {
                    result.HasError = true;
                    result.Message = "CurrencyID is needed for packages to different country!";
                    return result;
                }

                if ( !IntacceptLoadTypeArray.Contains(_AsrManifestShipmentDetails.LoadTypeID.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Please check LoadTypeID!";
                    return result;
                }
            }

                string list1 = System.Configuration.ConfigurationManager.AppSettings["ClientIDWithDomAsrDV"].ToString();
                List<int> ClientValidation = list1.Split(',').Select(Int32.Parse).ToList();
            bool IsAllowDomAsrDV = ClientValidation.Contains(_AsrManifestShipmentDetails.ClientInfo.ClientID);


            if (OriginCountryCode == DestinationCountryCode) // Domestic Delivery
            {
                if (_AsrManifestShipmentDetails.DeclareValue != 0 && !IsAllowDomAsrDV)
                {
                    result.HasError = true;
                    result.Message = "Declared value should be 0 for domestic ASR delivery!";
                    return result;
                }
                string DomacceptLoadTypeArray = System.Configuration.ConfigurationManager.AppSettings["DomASRLoadTypes"].ToString();

                if (!DomacceptLoadTypeArray.Contains(_AsrManifestShipmentDetails.LoadTypeID.ToString()))
                {
                    result.HasError = true;
                    result.Message = "Please check LoadTypeID!";
                    return result;
                }

                // Domestic ASR delivery commercial invoice detail is optional
                //if (_AsrManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Any())
                //{
                //    result.HasError = true;
                //    result.Message = "Please do not send commercial invoice data for domestic ASR delivery.";
                //    return result;
                //}
            }
            #endregion

            #region Check Surcharge
            if (_AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(7) && _AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(8))
            {
                result.HasError = true;
                result.Message = "ErrSurcharge";
                return result;
            }

            if (!_AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(9) && !_AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(10))
            {
                result.HasError = true;
                result.Message = "ErrSurcharge";
                return result;
            }

            if (_AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(9) && _AsrManifestShipmentDetails.WaybillSurcharge.SurchargeIDList.Contains(10))
            {
                result.HasError = true;
                result.Message = "ErrSurcharge";
                return result;
            }
            #endregion

            if (_AsrManifestShipmentDetails.PickUpDate < DateTime.Now.Date)
            {
                result.HasError = true;
                result.Message = "PickUpDate cannot be early than current time";
                return result;
            }

            if (_AsrManifestShipmentDetails.DeclareValue > 0 && !IsAllowDomAsrDV && _AsrManifestShipmentDetails._CommercialInvoice.InvoiceNo == "") // must remove this validation
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErCommercialInvoice");
                return result;
            }

            if (_AsrManifestShipmentDetails._CommercialInvoice.CommercialInvoiceDetailList.Count() > 0
                && _AsrManifestShipmentDetails._CommercialInvoice.RefNo != _AsrManifestShipmentDetails.RefNo)
            {
                result.HasError = true;
                result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Please check your RefNo.");
                return result;
            }

            ServiceTypeID = GlobalVar.GV.GetServiceTypeID(_AsrManifestShipmentDetails.ClientInfo, LoadTypeID);
            IsCOD = _AsrManifestShipmentDetails.BillingType == 5 || _AsrManifestShipmentDetails.CODCharge > 0;

            OriginStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(
                _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode,
                _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode), IsCourierLoadType);

            DestinationStationID = GlobalVar.GV.GetStationByCity(GlobalVar.GV.GetCityIDByCityCode(
                _AsrManifestShipmentDetails.ConsigneeInfo.CityCode,
                _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode), IsCourierLoadType);

            this.OriginCityCode = GlobalVar.GV.ISCityCodeValid(
               _AsrManifestShipmentDetails.ConsigneeInfo.CityCode,
               _AsrManifestShipmentDetails.ConsigneeInfo.CountryCode);
            this.DestinationCityCode = GlobalVar.GV.ISCityCodeValid(
                 _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CityCode,
                 _AsrManifestShipmentDetails.ClientInfo.ClientAddress.CountryCode);

            var tempODAStationID = GlobalVar.GV.GetODAStationByCityandCountryCode(_AsrManifestShipmentDetails.ConsigneeInfo.CityCode);
            if (tempODAStationID.HasValue)
                ODADestinationStationID = tempODAStationID.Value;

            return result;
        }
    }
}