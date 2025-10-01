using InfoTrack.BusinessLayer.DContext;
using System;
using System.Linq;

namespace InfoTrack.NaqelAPI
{
    public class ClientInformation
    {
        private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        public ClientAddress ClientAddress = new ClientAddress();
        public ClientContact ClientContact = new ClientContact();

        internal int ClientAddressID;
        internal int ClientContactID;

        public int ClientID = 0;
        public string Password = "";
        public string Version = "";

        public ClientInformation()
        {

        }

        public ClientInformation(int _ClientID, string _Password)
        {
            ClientID = _ClientID;
            Password = _Password;
        }

        internal Result CheckClientInfo(ClientInformation _clientInformation, bool CheckAddressAndContact, int? PharmaClientID = 0, bool IsCourierLoadType = false)
        {
            Result result = new Result();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            try
            {
                #region Check API credential
                if (_clientInformation.ClientID <= 0)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientInfoData");
                    return result;
                }

                if (_clientInformation.Password == "")
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
                    return result;
                }

                if (_clientInformation.Version == "")//|| _clientInformation.Version != GlobalVar.GV.XMLShippingServiceVersion)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongVersion");
                    return result;
                }

                var tempClientID = _clientInformation.ClientID;
                if (PharmaClientID != 0)
                    _clientInformation.ClientID = (int)PharmaClientID;
                APIClientAccess ClientWebServiceInstance = new APIClientAccess();
                APIClientAccess instance = dcMaster.APIClientAccesses.FirstOrDefault(P => P.ClientID == _clientInformation.ClientID && P.StatusID == 1 && P.IsRestrictedToCreateWaybill != true);
                if (instance != null)
                {
                    InfoTrack.Common.Security security = new InfoTrack.Common.Security();
                    // APIClientAccess instance = //dcMaster.APIClientAccesses.First(P => P.ClientID == _clientInformation.ClientID && P.StatusID == 1);
                    string cc = security.Decrypt(instance.ClientPassword.ToArray());


                    if (instance.ClientPassword != security.Encrypt(_clientInformation.Password))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
                        return result;
                    }
                }
                else
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceClientWrongPassword");
                    return result;
                }
                #endregion

                int count = dcMaster.ClientBlackLists
                    .Where(P => P.ClientID == ClientID
                        && DateTime.Now >= P.FromDate 
                        && DateTime.Now <= P.ToDate 
                        && P.StatusID != 3 
                        && P.Amount == 0)
                    .Count();

                if (count > 0)
                {
                    result.HasError = true;
                    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("Your account credit has been stopped, please contact Naqel financial team.");
                    return result;
                }

                if (CheckAddressAndContact)
                {
                    if (PharmaClientID != 0)
                        _clientInformation.ClientID = tempClientID;
                    #region Check Client Address Data

                    if (!string.IsNullOrEmpty(_clientInformation.ClientAddress.PhoneNumber))
                        _clientInformation.ClientAddress.PhoneNumber = GlobalVar.GV.GetString(_clientInformation.ClientAddress.PhoneNumber, 50);

                    if (_clientInformation.ClientAddress.PhoneNumber == "" || _clientInformation.ClientAddress.PhoneNumber == "0" || _clientInformation.ClientAddress.PhoneNumber.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressPhoneNo");
                        return result;
                    }

                    if (!string.IsNullOrEmpty(_clientInformation.ClientAddress.FirstAddress))
                        _clientInformation.ClientAddress.FirstAddress = GlobalVar.GV.GetString(_clientInformation.ClientAddress.FirstAddress, 250);

                    if (_clientInformation.ClientAddress.FirstAddress == null || _clientInformation.ClientAddress.FirstAddress == "" || _clientInformation.ClientAddress.FirstAddress.Length > 250)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressFirstAddress");
                        return result;
                    }

                    //if (ClientAddress.CityID <= 0)
                    //{
                    //    result.HasError = true;
                    //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongCityID");
                    //    return result;
                    //}

                    //if (!GlobalVar.GV.IsCityCorrect(ClientAddress.CityID))
                    //{
                    //    result.HasError = true;
                    //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongCityID");
                    //    return result;
                    //}

                    if (string.IsNullOrEmpty(_clientInformation.ClientAddress.CountryCode))//== null || _clientInformation.ClientAddress.CountryCode == "")
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCountryCode");
                        return result;
                    }

                    if (!GlobalVar.GV.IsCountryCodeExist(_clientInformation.ClientAddress.CountryCode))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCountryCode");
                        return result;
                    }

                    if (string.IsNullOrEmpty(_clientInformation.ClientAddress.CityCode))//== null || _clientInformation.ClientAddress.CityCode == "")
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCode");
                        return result;
                    }
                    //is active citycode
                    if (!GlobalVar.GV.IseCorrectCityCode(_clientInformation.ClientAddress.CityCode))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCode");
                        return result;
                    }

                    if (!GlobalVar.GV.IsCityCodeExist(_clientInformation.ClientAddress.CityCode, IsCourierLoadType))
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCode");
                        return result;
                    }

                    if (_clientInformation.ClientAddress.CityCode == "DUBAI")
                    {

                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientAddressCityCode");
                        return result;
                    }

                    int tempCityID = GlobalVar.GV.GetCityIDByCityCode(_clientInformation.ClientAddress.CityCode, _clientInformation.ClientAddress.CountryCode, IsCourierLoadType);
                    if (tempCityID == 0)
                    {
                        result.HasError = true;
                        result.Message = "Client Country Code and City Code Not Match.";
                        return result;
                    }

                    //if (!GlobalVar.GV.IsCityCodeCorrect(ClientAddress.CityCode))
                    //{
                    //    result.HasError = true;
                    //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongCityID");
                    //    return result;
                    //}

                    //if (!GlobalVar.GV.IsCityCodeCorrect(ClientAddress.CountryCode))
                    //{
                    //    result.HasError = true;
                    //    result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongCityID");
                    //    return result;
                    //}

                    #endregion

                    #region Check Client Contact Data

                    if (_clientInformation.ClientContact.Name == null || _clientInformation.ClientContact.Name == "" || _clientInformation.ClientContact.Name.Length > 250)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientContactName");
                        return result;
                    }

                    if (_clientInformation.ClientContact.PhoneNumber == "0" || _clientInformation.ClientContact.PhoneNumber == "" || _clientInformation.ClientContact.PhoneNumber.Length > 50)
                    {
                        result.HasError = true;
                        result.Message = GlobalVar.GV.GVCommon.GetLocalizationMessage("ErWebServiceWrongClientContactPhoneNo");
                        return result;
                    }

                    #endregion

                    CheckClientAddressAndContact(_clientInformation, IsCourierLoadType);
                    if (_clientInformation.ClientAddressID == 0)
                    {
                        result.HasError = true;
                        result.Message = "Client Address is missing.";
                        return result;
                    }

                    if (_clientInformation.ClientContactID == 0)
                    {
                        result.HasError = true;
                        result.Message = "Client contact is missing.";
                        return result;
                    }
                }
            }
            catch (Exception e) {
                result.HasError = true;
                result.Message = "an error happen when saving the client details code : 120";
                GlobalVar.GV.AddErrorMessage(e, this); 
            
            }

            return result;
        }

        internal void CheckClientAddressAndContact(ClientInformation _clientInformation, bool IsCourierLoadType)
        {
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());

            if (dcMaster.ClientAddresses.Where(P => P.ClientID == _clientInformation.ClientID &&
                                           P.StatusID != 3 &&
                                           P.PhoneNumber.ToLower() == _clientInformation.ClientAddress.PhoneNumber.ToLower() && P.POBox == _clientInformation.ClientAddress.POBox &&
                                           // P.PhoneNumber.ToLower() == _clientInformation.ClientAddress.PhoneNumber.ToLower() &&
                                           P.FirstAddress.ToLower() == _clientInformation.ClientAddress.FirstAddress.ToLower() &&P.AddressAR.ToLower() == _clientInformation .ClientAddress.ShipperName &&
                                           P.CityID == GlobalVar.GV.GetCityIDByCityCode(_clientInformation.ClientAddress.CityCode, _clientInformation.ClientAddress.CountryCode, IsCourierLoadType)).Count() > 0)
                _clientInformation.ClientAddressID = dcMaster.ClientAddresses.First(P => P.ClientID == _clientInformation.ClientID &&
                                           P.StatusID != 3 &&
                                           P.PhoneNumber.ToLower() == _clientInformation.ClientAddress.PhoneNumber.ToLower() && P.POBox == _clientInformation.ClientAddress.POBox &&
                                           P.FirstAddress.ToLower() == _clientInformation.ClientAddress.FirstAddress.ToLower() &&  P.AddressAR.ToLower() == _clientInformation.ClientAddress.ShipperName &&
                                           P.CityID == GlobalVar.GV.GetCityIDByCityCode(_clientInformation.ClientAddress.CityCode, _clientInformation.ClientAddress.CountryCode, IsCourierLoadType)).ID;
            else
            {
                InfoTrack.BusinessLayer.DContext.ClientAddress instance = new BusinessLayer.DContext.ClientAddress();

                instance.PhoneNumber = _clientInformation.ClientAddress.PhoneNumber;
                instance.FirstAddress = _clientInformation.ClientAddress.FirstAddress;
                instance.CityID = GlobalVar.GV.GetCityIDByCityCode(_clientInformation.ClientAddress.CityCode, _clientInformation.ClientAddress.CountryCode, IsCourierLoadType);
                //instance.CityID = _clientInformation.ClientAddress.CityID;

                instance.POBox = _clientInformation.ClientAddress.POBox;
                instance.ZipCode = _clientInformation.ClientAddress.ZipCode;
                instance.Fax = _clientInformation.ClientAddress.Fax;
                instance.Location = _clientInformation.ClientAddress.Location;
                //check in
                instance.GPSLocation = (_clientInformation.ClientAddress.Latitude + ',' + _clientInformation.ClientAddress.Longitude);

                instance.ForPickUp = true;
                instance.ForInvoice = false;
                instance.ForSales = false;
                instance.ForDelivery = false;
                instance.ForMarketing = false;
                instance.ForOthers = false;
                instance.ClientID = ClientID;
                instance.RouteID = GlobalVar.GV.GetRouteID(instance.PhoneNumber, "0");
                instance.StatusID = 1;
                instance.IsNotificaton = false;
                instance.NationalAddress = _clientInformation.ClientAddress.NationalAddress;//***
                instance.AddressAR = _clientInformation.ClientAddress.ShipperName;
                dcMaster.ClientAddresses.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
                _clientInformation.ClientAddressID = instance.ID;
            }

            if (dcMaster.ClientContacts.Where(P => P.ClientAddressID == _clientInformation.ClientAddressID &&
                                           P.StatusID != 3 &&
                                           P.PhoneNumber.ToLower() == _clientInformation.ClientContact.PhoneNumber.ToLower() &&
                                           P.Name.ToLower() == _clientInformation.ClientContact.Name.ToLower() &&
                                           P.Mobile == _clientInformation.ClientContact.MobileNo).Count() > 0)

                _clientInformation.ClientContactID = dcMaster.ClientContacts.First(P => P.ClientAddressID == _clientInformation.ClientAddressID &&
                                           P.StatusID != 3 &&
                                           P.PhoneNumber.ToLower() == _clientInformation.ClientContact.PhoneNumber.ToLower() &&
                                           P.Name.ToLower() == _clientInformation.ClientContact.Name.ToLower() &&
                                           P.Mobile == _clientInformation.ClientContact.MobileNo).ID;
            else
            {
                InfoTrack.BusinessLayer.DContext.ClientContact instance = new BusinessLayer.DContext.ClientContact();

                instance.Name = _clientInformation.ClientContact.Name;
                instance.PhoneNumber = _clientInformation.ClientContact.PhoneNumber;
                instance.Email = _clientInformation.ClientContact.Email;
                instance.Mobile = _clientInformation.ClientContact.MobileNo;

                instance.ForPickUp = true;
                instance.ForInvoice = false;
                instance.ForSales = false;
                instance.ForDelivery = false;
                instance.ForMarketing = false;
                instance.ForOthers = false;
                instance.ClientAddressID = _clientInformation.ClientAddressID;
                instance.StatusID = 1;

                dcMaster.ClientContacts.InsertOnSubmit(instance);
                dcMaster.SubmitChanges();
                ClientContactID = instance.ID;
            }

        }
    }

    public class AmazonClientInformation
    {
        public string UserID { get; set; } = string.Empty;
        public string Password { get; set; }
    }

    public class ClientAddress
    {
        //internal int ClientAddressID = 0;
        internal int RouteID = 0;
        public string PhoneNumber = "0";

        [System.ComponentModel.DefaultValue("")]
        public string NationalAddress=""; //***
        private string address = "";
        public string FirstAddress
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

        private string location = "";
        public string Location
        {
            get
            {
                if (location.Length > 250)
                    return location.Remove(250 - 1);
                else
                    return location;
            }
            set
            {
                location = value;
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
                //CityID = GlobalVar.GV.GetCityIDByCityCode(citycode.Trim(), CountryCode.Trim());
            }
        }
        //Check in 
        public string POBox = "0";
        public string ZipCode = "0";
        public string Fax = "0";
        public string Latitude = "";
        public string Longitude = "";
        public string ShipperName = "";

    }


    public class ClientContact
    {
        //internal int ClientContactID = 0;
        public string Name = "";
        public string Email = "";
        public string PhoneNumber = "0";
        public string MobileNo = "0";
    }


    


}