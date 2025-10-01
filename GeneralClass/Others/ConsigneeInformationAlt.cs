using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{
    public class ConsigneeInformationAlt
    {
        //private InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster;
        public ConsigneeInformationAlt()
        {
        }

        //internal int ConsigneeID;
        //internal int ConsigneeDetailID;

        [System.ComponentModel.DefaultValue(0)]
        public long? ConsigneeNationalID = 0;

        public string ConsigneePassportNo = "";
        public string ConsigneePassportExp = "";
        public string ConsigneeNationality = "";

        public string ConsigneeName = "";
        public string Email = "";
        public string Mobile = "";
        public string PhoneNumber = "";
        public string Fax = "";
        public string District = "";


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

        private string countryName = "";
        public string CountryName
        {
            get { return countryName.Trim(); }
            set
            {
                countryName = value.Trim();
            }
        }

        private string provinceName = "";
        public string ProvinceName
        {
            get { return provinceName.Trim(); }
            set
            {
                provinceName = value.Trim();
            }
        }

        private string cityName = "";
        public string CityName
        {
            get { return cityName.Trim(); }
            set
            {
                cityName = value.Trim();
            }
        }

    }
}