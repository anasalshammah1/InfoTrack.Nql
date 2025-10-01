using Dapper;
using InfoTrack.BusinessLayer.DContext;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class ConsigneeInformation
    {
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        public ConsigneeInformation()
        {

        }

        internal int ConsigneeID;
        internal int ConsigneeDetailID;

        [System.ComponentModel.DefaultValue(0)]
        public long? ConsigneeNationalID = 0;

        public string ConsigneePassportNo = "";
        public string ConsigneePassportExp = "";
        public string ConsigneeNationality = "";
        public string ConsigneeNationalIdExpiry = "";
        public string ConsigneeBirthDate = "";
        public string ConsigneeName = "";
        public string Email = "";
        public string Mobile = "";
        public string PhoneNumber = "";
        public string Fax = "";
        public string District = "";
        public string What3Words = "";
        public string SPLOfficeID = "";
        public string consignee_serial = "";
        public string BuildingNo = "";
        public string CompanyName = "";
        private string address = "";
        public string Address
        {
            get
            {
                if (address.Length > 250)
                    return address.Remove(250 - 1);
                else
                    return address;
            }
            set
            {
                address = value;
            }
        }

        private string nationalAddress = "";
        public string NationalAddress
        {
            get
            {
                if (nationalAddress.Length > 15)
                    return nationalAddress.Remove(15 - 1);
                else
                    return nationalAddress;
            }
            set
            {
                nationalAddress = value;
            }
        }

        private string near = "";
        public string Near
        {
            get
            {
                if (near.Length > 200)
                    return near.Remove(200 - 1);
                else
                    return near;
            }
            set
            {
                near = value;
            }
        }

        private string countrycode = "";
        public string CountryCode
        {
            get { return countrycode.Trim(); }
            set
            {
                countrycode = value.Trim();
                //CityID = GlobalVar.GV.GetCityIDByCityCode(CityCode.Trim(), CountryCode.Trim());
            }
        }

        private string citycode = "";
        public string CityCode
        {
            get { return citycode.Trim(); }
            set
            {
                citycode = value.Trim();
                //CityID = GlobalVar.GV.GetCityIDByCityCode(value.Trim(), CountryCode.Trim());
            }
        }

        public string ParcelLockerMachineID = "";




        internal Result CheckConsigneeInfo(ConsigneeInformation _ConsigneeInformation, ClientInformation ClientInfo, bool? IsCourierLoadType = false)
        {
            Result result = new Result();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            try
            {
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.ConsigneeName) || _ConsigneeInformation.ConsigneeName.Length > 200)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeName");
                    return result;
                }
                _ConsigneeInformation.ConsigneeName = GlobalVar.GV.GetString(_ConsigneeInformation.ConsigneeName, 200);

                // Curse Words Filteration Part as requested by DM

                if (GlobalVar.GV.IsCurseName(_ConsigneeInformation.ConsigneeName, ClientInfo.ClientID))
                {
                    _ConsigneeInformation.ConsigneeName = "CustomerName";
                }

                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPhoneNumber");
                    return result;
                }

                //below countires have 8 digits phone No
                var Countries = new List<string>();
                Countries.Add("LB");
                Countries.Add("BH");
                Countries.Add("OM");
                Countries.Add("QA");
                Countries.Add("KW");

                if (!Countries.Contains(_ConsigneeInformation.countrycode))
                {
                    //if (string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber) || _ConsigneeInformation.PhoneNumber.Length > 200)
                    //{
                    //    result.HasError = true;
                    //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPhoneNumber");
                    //    return result;
                    //}
                    //_ConsigneeInformation.PhoneNumber = GlobalVar.GV.GetString(_ConsigneeInformation.PhoneNumber, 200);

                    if (_ConsigneeInformation.PhoneNumber.Length > 200
                        || _ConsigneeInformation.PhoneNumber.Trim().Length < 9)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPhoneNumber");
                        return result;
                    }
                    _ConsigneeInformation.PhoneNumber = GlobalVar.GV.GetString(_ConsigneeInformation.PhoneNumber.Trim(), 200);

                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.Mobile))
                    {
                        if (_ConsigneeInformation.Mobile.Length > 200)
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongMobileNumber");
                            return result;
                        }
                        _ConsigneeInformation.Mobile = GlobalVar.GV.GetString(_ConsigneeInformation.Mobile.Trim(), 200);
                    }
                }

                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.Address) || _ConsigneeInformation.Address.Length > 250)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeFirstAddress");
                    return result;
                }
                _ConsigneeInformation.Address = GlobalVar.GV.GetString(_ConsigneeInformation.Address.Replace('\n', ' ').Replace('\r', ' '), 250);

                //consignee serial null value :


                #region Consignee Passport information
                if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.ConsigneePassportNo) && _ConsigneeInformation.ConsigneePassportNo.Trim().Length > 20)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneePassportNo");
                    return result;
                }
                _ConsigneeInformation.ConsigneePassportNo = GlobalVar.GV.GetString(_ConsigneeInformation.ConsigneePassportNo.Trim(), 20);

                if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.ConsigneePassportExp) && _ConsigneeInformation.ConsigneePassportExp.Trim().Length > 20)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneePassportExp");
                    return result;
                }
                _ConsigneeInformation.ConsigneePassportExp = GlobalVar.GV.GetString(_ConsigneeInformation.ConsigneePassportExp.Trim(), 20);

                if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.ConsigneeNationality) && _ConsigneeInformation.ConsigneeNationality.Trim().Length > 100)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeNationality");
                    return result;
                }
                _ConsigneeInformation.ConsigneeNationality = GlobalVar.GV.GetString(_ConsigneeInformation.ConsigneeNationality.Trim(), 100);
                #endregion

                #region Consignee CountryCode & CityCode
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.CountryCode) || !GlobalVar.GV.IsCountryCodeExist(_ConsigneeInformation.CountryCode))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCountryCode");
                    return result;
                }
                //is active citycode
                if (!GlobalVar.GV.IseCorrectCityCode(_ConsigneeInformation.CityCode))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCode");
                    return result;
                }
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.CityCode) || !GlobalVar.GV.IsCityCodeExist(_ConsigneeInformation.CityCode, IsCourierLoadType))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCode");
                    return result;
                }

                int tempCityID = GlobalVar.GV.GetCityIDByCityCode(_ConsigneeInformation.CityCode, _ConsigneeInformation.CountryCode, IsCourierLoadType);
                if (tempCityID == 0)
                {
                    result.HasError = true;
                    result.Message = "Consignee Country Code and City Code Not Match.";
                    return result;
                }
                #endregion

                #region KSA consignee black list
                if (_ConsigneeInformation.CountryCode == "KSA")
                {
                    string tempPhone = "";
                    string tempMobile = "";

                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber) && _ConsigneeInformation.PhoneNumber.Trim().Length >= 9)
                    {
                        tempPhone = dcMaster.CorrectMobileNo(_ConsigneeInformation.PhoneNumber);
                        tempPhone = tempPhone.Substring(tempPhone.Length - 9, 9);
                    }
                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.Mobile) && _ConsigneeInformation.Mobile.Trim().Length >= 9)
                    {
                        tempMobile = dcMaster.CorrectMobileNo(_ConsigneeInformation.Mobile);
                        tempMobile = tempMobile.Substring(tempMobile.Length - 9, 9);
                    }
                    if (dcMaster.ViwBlackListPhoneSAs.Where(p => p.PhoneNo == tempPhone || p.PhoneNo == tempMobile).Any())
                    {
                        result.HasError = true;
                        result.Message = "Unfortunately we can not deliver to this Consignee";
                        return result;
                    }
                }
                #endregion
            }
            catch (Exception e) {
                result.HasError = true;
                result.Message = "an error happen when saving the consignee details code : 120";
                GlobalVar.GV.AddErrorMessage(e, ClientInfo); 
            
            }

            return result;
        }

        internal void CheckConsigneeData(ClientInformation ClientInfo, ConsigneeInformation _consignee, bool? IsCourierLoadType = false)
        {
            //dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            int _cityid = GlobalVar.GV.GetCityIDByCityCode(_consignee.CityCode, _consignee.CountryCode, IsCourierLoadType);

            //_consignee.ConsigneePassportNo = string.IsNullOrWhiteSpace(_consignee.ConsigneePassportNo) ? "" : _consignee.ConsigneePassportNo.Trim();
            //_consignee.ConsigneePassportExp = string.IsNullOrWhiteSpace(_consignee.ConsigneePassportExp) ? "" : _consignee.ConsigneePassportExp.Trim();
            //_consignee.ConsigneeNationality = string.IsNullOrWhiteSpace(_consignee.ConsigneeNationality) ? "" : _consignee.ConsigneeNationality.Trim();


            //var tempConsignee = dcMaster.ViwConsigneeListByClientAPIs
            //    .Where(P => P.ClientID == ClientInfo.ClientID
            //        && P.Name.ToLower() == _consignee.ConsigneeName.ToLower()
            //        && P.PhoneNumber == _consignee.PhoneNumber
            //        && P.FirstAddress == _consignee.Address
            //        && P.CityID == _cityid
            //        && P.StatusID == 1
            //        && P.ConsigneePassportNo == _consignee.ConsigneePassportNo
            //        && P.ConsigneePassportExp == _consignee.ConsigneePassportExp
            //        && P.ConsigneeNationality == _consignee.ConsigneeNationality)
            //    .Select(c => new ViwConsigneeListByClientAPI { ID = c.ID, ConsigneeDetailID = c.ConsigneeDetailID })
            //    .FirstOrDefault();


            //if (tempConsignee != null)
            //{
            //    _consignee.ConsigneeID = tempConsignee.ID;
            //    _consignee.ConsigneeDetailID = tempConsignee.ConsigneeDetailID.Value;
            //}
            //else
            //{
            InfoTrack.BusinessLayer.DContext.Consignee CInstance = new Consignee();
            CInstance.Name = _consignee.ConsigneeName;
            CInstance.Email = string.IsNullOrWhiteSpace(_consignee.Email) ? "" : _consignee.Email;
            CInstance.FName = string.IsNullOrWhiteSpace(_consignee.CompanyName) ? "" : _consignee.CompanyName;
            CInstance.StatusID = 1;
            CInstance.ClientID = ClientInfo.ClientID;
            CInstance.ConsigneePassportNo = _consignee.ConsigneePassportNo;
            CInstance.ConsigneePassportExp = _consignee.ConsigneePassportExp;
            CInstance.ConsigneeNationality = _consignee.ConsigneeNationality;
            // CInstance.ConsigneeBirthDate = _consignee.ConsigneeBirthDate;
            //CInstance.ConsigneeNationalIdExpiry = _consignee.ConsigneeNationalIdExpiry;
            dcMaster.Consignees.InsertOnSubmit(CInstance);
            dcMaster.SubmitChanges();

            InfoTrack.BusinessLayer.DContext.ConsigneeDetail CDInstance = new ConsigneeDetail();
            CDInstance.ConsigneeID = CInstance.ID;
            CDInstance.Near = string.IsNullOrWhiteSpace(_consignee.Near) ? "" : _consignee.Near;
            CDInstance.FirstAddress = _consignee.Address + "," + CDInstance.Near;
            CDInstance.What3Words = string.IsNullOrWhiteSpace(_consignee.What3Words) ? "" : _consignee.What3Words;

            CDInstance.SPLOfficeID = string.IsNullOrWhiteSpace(_consignee.SPLOfficeID) ? "" : _consignee.SPLOfficeID.Trim();

            CDInstance.NationalAddress = _consignee.NationalAddress;
            CDInstance.PhoneNumber = _consignee.PhoneNumber;
            CDInstance.CityID = _cityid;
            CDInstance.StatusID = 1;
            CDInstance.Fax = string.IsNullOrWhiteSpace(_consignee.Fax) ? "" : _consignee.Fax;
            CDInstance.Mobile = string.IsNullOrWhiteSpace(_consignee.Mobile) ? "" : _consignee.Mobile;
            CDInstance.District = string.IsNullOrWhiteSpace(_consignee.District) ? 0 : GlobalVar.GV.GetDistrictID(_consignee.District);
            CDInstance.BuildingNo = CDInstance.BuildingNo = string.IsNullOrWhiteSpace(_consignee.BuildingNo) ? "" : _consignee.BuildingNo;
            var ParcelLockerMachineID_int = int.TryParse(ParcelLockerMachineID.Trim(), out int outParcelLockerMachineID_int);
            CDInstance.MachineID = string.IsNullOrWhiteSpace(_consignee.ParcelLockerMachineID) || ParcelLockerMachineID_int == false ? 0 : outParcelLockerMachineID_int;
            var consigneeSerial = int.TryParse(consignee_serial, out int outconsigneeSerial);
            CDInstance.consignee_serial = string.IsNullOrWhiteSpace(_consignee.consignee_serial) || !consigneeSerial ? 0 : outconsigneeSerial;

            dcMaster.ConsigneeDetails.InsertOnSubmit(CDInstance);
            dcMaster.SubmitChanges();

            _consignee.ConsigneeID = CInstance.ID;
            _consignee.ConsigneeDetailID = CDInstance.ID;
        }
        internal Result CheckPLmachineID(ClientInformation ClientInfo, string ParcelLockerMachineID)
        {
            Result result = new Result();

            try
            {
                MastersDataContext dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (!int.TryParse(ParcelLockerMachineID, out int plid))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErInvalidPLMachineID"); ;
                    return result;
                }

                bool lockerExists = dcMaster.ViwAPIParcelLockers.Any(p => p.ParcelLockerID == plid);

                if (!lockerExists)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErInvalidPLMachineID");
                }
                else
                {
                    bool hasAvailableLockers = dcMaster.viwPLLockerAvailablities
                        .Any(p => p.MachineID == plid && p.availableNo >= 1);

                    if (!hasAvailableLockers)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErUnavailableParcelLocker");
                    }
                }
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "An error occurred while checking the parcel locker machine ID " + ex.Message;
            }

            return result;
        }

        internal Result CheckSPLDropOffLocation(ClientInformation ClientInfo, string SPLOfficeID)
        {
            Result result = new Result();

            try
            {
                MastersDataContext dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

                if (!int.TryParse(SPLOfficeID, out int splid))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErInvalidSPLOfficeID");
                    return result;
                }

                bool exists = dcMaster.viwDropOffLocations.Any(p => p.ID == splid);

                if (!exists)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErInvalidSPLOfficeID");
                }
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = "An error occurred while checking the SPL Drop Off Location ID: " + ex.Message;
            }

            return result;
        }

    }

    public class BulletConsigneeInformation
    {
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        public BulletConsigneeInformation()
        {

        }

        internal int ConsigneeID;
        internal int ConsigneeDetailID;



        public string ConsigneeName = "";
        public string Email = "";
        public string Mobile = "";
        public string PhoneNumber = "";
        public string Fax = "";

        private string address = "";

        public string Address
        {
            get
            {
                if (address.Length > 250)
                    return address.Remove(250 - 1);
                else
                    return address;
            }
            set
            {
                address = value;
            }
        }

        private string nationalAddress = "";
        public string NationalAddress
        {
            get
            {
                if (nationalAddress.Length > 15)
                    return nationalAddress.Remove(15 - 1);
                else
                    return nationalAddress;
            }
            set
            {
                nationalAddress = value;
            }
        }

        private string near = "";
        public string Near
        {
            get
            {
                if (near.Length > 200)
                    return near.Remove(200 - 1);
                else
                    return near;
            }
            set
            {
                near = value;
            }
        }

        private string countrycode = "";
        public string CountryCode
        {
            get { return countrycode.Trim(); }
            set
            {
                countrycode = value.Trim();
                //CityID = GlobalVar.GV.GetCityIDByCityCode(CityCode.Trim(), CountryCode.Trim());
            }
        }

        private string citycode = "";
        public string CityCode
        {
            get { return citycode.Trim(); }
            set
            {
                citycode = value.Trim();
                //CityID = GlobalVar.GV.GetCityIDByCityCode(value.Trim(), CountryCode.Trim());
            }
        }

        internal Result CheckConsigneeInfo(BulletConsigneeInformation _ConsigneeInformation, ClientInformation ClientInfo, bool? IsCourierLoadType = false)
        {
            Result result = new Result();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            try
            {
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.ConsigneeName) || _ConsigneeInformation.ConsigneeName.Length > 200)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeName");
                    return result;
                }
                _ConsigneeInformation.ConsigneeName = GlobalVar.GV.GetString(_ConsigneeInformation.ConsigneeName, 200);

                if (_ConsigneeInformation.countrycode != "BH" && _ConsigneeInformation.countrycode != "LB")
                {
                    if (string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber) || _ConsigneeInformation.PhoneNumber.Length > 200)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPhoneNumber");
                        return result;
                    }
                    _ConsigneeInformation.PhoneNumber = GlobalVar.GV.GetString(_ConsigneeInformation.PhoneNumber, 200);

                    if (string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber)
                        || _ConsigneeInformation.PhoneNumber.Length > 200
                        || _ConsigneeInformation.PhoneNumber.Trim().Length < 9)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongPhoneNumber");
                        return result;
                    }
                    _ConsigneeInformation.PhoneNumber = GlobalVar.GV.GetString(_ConsigneeInformation.PhoneNumber.Trim(), 200);

                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.Mobile))
                    {
                        if (_ConsigneeInformation.Mobile.Length > 200)
                        {
                            result.HasError = true;
                            result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongMobileNumber");
                            return result;
                        }
                        _ConsigneeInformation.Mobile = GlobalVar.GV.GetString(_ConsigneeInformation.Mobile.Trim(), 200);
                    }
                }

                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.Address) || _ConsigneeInformation.Address.Length > 250)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeFirstAddress");
                    return result;
                }
                _ConsigneeInformation.Address = GlobalVar.GV.GetString(_ConsigneeInformation.Address.Replace('\n', ' ').Replace('\r', ' '), 250);


                #region Consignee CountryCode & CityCode
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.CountryCode) || !GlobalVar.GV.IsCountryCodeExist(_ConsigneeInformation.CountryCode))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCountryCode");
                    return result;
                }
                //is active citycode
                if (!GlobalVar.GV.IseCorrectCityCode(_ConsigneeInformation.CityCode))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCode");
                    return result;
                }
                if (string.IsNullOrWhiteSpace(_ConsigneeInformation.CityCode) || !GlobalVar.GV.IsCityCodeExist(_ConsigneeInformation.CityCode, IsCourierLoadType))
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongConsigneeCityCode");
                    return result;
                }

                int tempCityID = GlobalVar.GV.GetCityIDByCityCode(_ConsigneeInformation.CityCode, _ConsigneeInformation.CountryCode, IsCourierLoadType);
                if (tempCityID == 0)
                {
                    result.HasError = true;
                    result.Message = "Consignee Country Code and City Code Not Match.";
                    return result;
                }
                #endregion

                #region KSA consignee black list
                if (_ConsigneeInformation.CountryCode == "KSA")
                {
                    string tempPhone = "";
                    string tempMobile = "";

                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.PhoneNumber) && _ConsigneeInformation.PhoneNumber.Trim().Length >= 9)
                    {
                        tempPhone = dcMaster.CorrectMobileNo(_ConsigneeInformation.PhoneNumber);
                        tempPhone = tempPhone.Substring(tempPhone.Length - 9, 9);
                    }
                    if (!string.IsNullOrWhiteSpace(_ConsigneeInformation.Mobile) && _ConsigneeInformation.Mobile.Trim().Length >= 9)
                    {
                        tempMobile = dcMaster.CorrectMobileNo(_ConsigneeInformation.Mobile);
                        tempMobile = tempMobile.Substring(tempMobile.Length - 9, 9);
                    }
                    if (dcMaster.ViwBlackListPhoneSAs.Where(p => p.PhoneNo == tempPhone || p.PhoneNo == tempMobile).Any())
                    {
                        result.HasError = true;
                        result.Message = "Unfortunately we can not deliver to this Consignee";
                        return result;
                    }
                }
                #endregion
            }
            catch (Exception e) { GlobalVar.GV.AddErrorMessage(e, ClientInfo); }

            return result;
        }

        internal void CheckConsigneeData(ClientInformation ClientInfo, BulletConsigneeInformation _consignee, bool? IsCourierLoadType = false)
        {
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            int _cityid = GlobalVar.GV.GetCityIDByCityCode(_consignee.CityCode, _consignee.CountryCode, IsCourierLoadType);

            //_consignee.ConsigneePassportNo = string.IsNullOrWhiteSpace(_consignee.ConsigneePassportNo) ? "" : _consignee.ConsigneePassportNo.Trim();
            //_consignee.ConsigneePassportExp = string.IsNullOrWhiteSpace(_consignee.ConsigneePassportExp) ? "" : _consignee.ConsigneePassportExp.Trim();
            //_consignee.ConsigneeNationality = string.IsNullOrWhiteSpace(_consignee.ConsigneeNationality) ? "" : _consignee.ConsigneeNationality.Trim();


            //var tempConsignee = dcMaster.ViwConsigneeListByClientAPIs
            //    .Where(P => P.ClientID == ClientInfo.ClientID
            //        && P.Name.ToLower() == _consignee.ConsigneeName.ToLower()
            //        && P.PhoneNumber == _consignee.PhoneNumber
            //        && P.FirstAddress == _consignee.Address
            //        && P.CityID == _cityid
            //        && P.StatusID == 1
            //        && P.ConsigneePassportNo == _consignee.ConsigneePassportNo
            //        && P.ConsigneePassportExp == _consignee.ConsigneePassportExp
            //        && P.ConsigneeNationality == _consignee.ConsigneeNationality)
            //    .Select(c => new ViwConsigneeListByClientAPI { ID = c.ID, ConsigneeDetailID = c.ConsigneeDetailID })
            //    .FirstOrDefault();


            //if (tempConsignee != null)
            //{
            //    _consignee.ConsigneeID = tempConsignee.ID;
            //    _consignee.ConsigneeDetailID = tempConsignee.ConsigneeDetailID.Value;
            //}
            //else
            //{
            InfoTrack.BusinessLayer.DContext.Consignee CInstance = new Consignee();
            CInstance.Name = _consignee.ConsigneeName;
            CInstance.Email = string.IsNullOrWhiteSpace(_consignee.Email) ? "" : _consignee.Email;
            CInstance.StatusID = 1;
            CInstance.ClientID = ClientInfo.ClientID;
            CInstance.ConsigneePassportNo = "";
            CInstance.ConsigneePassportExp = "";
            CInstance.ConsigneeNationality = "";
            CInstance.ConsigneeBirthDate = "";
            CInstance.ConsigneeNationalIdExpiry = "";
            dcMaster.Consignees.InsertOnSubmit(CInstance);
            dcMaster.SubmitChanges();

            InfoTrack.BusinessLayer.DContext.ConsigneeDetail CDInstance = new ConsigneeDetail();
            CDInstance.ConsigneeID = CInstance.ID;
            CDInstance.FirstAddress = _consignee.Address;
            CDInstance.What3Words = "";
            CDInstance.NationalAddress = _consignee.NationalAddress;
            CDInstance.PhoneNumber = _consignee.PhoneNumber;
            CDInstance.CityID = _cityid;
            CDInstance.StatusID = 1;
            CDInstance.Fax = string.IsNullOrWhiteSpace(_consignee.Fax) ? "" : _consignee.Fax;
            CDInstance.Mobile = string.IsNullOrWhiteSpace(_consignee.Mobile) ? "" : _consignee.Mobile;
            CDInstance.Near = string.IsNullOrWhiteSpace(_consignee.Near) ? "" : _consignee.Near;
            CDInstance.District = 0;
            CDInstance.BuildingNo = "";
            dcMaster.ConsigneeDetails.InsertOnSubmit(CDInstance);
            dcMaster.SubmitChanges();

            _consignee.ConsigneeID = CInstance.ID;
            _consignee.ConsigneeDetailID = CDInstance.ID;
        }
    }

}
