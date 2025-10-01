using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NaqelAPI
{
    public class SellerInformation
    {
        public SellerInformation()
        {

        }

        private int sellerid ;
        internal int sellerID
        {
            get { return sellerid; }
            set { sellerid = value; }
        }

        private int sellerdetailid;
        internal int sellerDetailID
        {
            get { return sellerdetailid; }
            set { sellerdetailid  = value; }
        }

        private string sellername = "";
        public string SellerName
        {
            get { return sellername; }
            set { sellername = value; }
        }

        private string email = "";
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private string mobile = "";
        public string Mobile
        {
            get { return mobile; }
            set { mobile = value; }
        }

        private string phonenumber = "";
        public string PhoneNumber
        {
            get { return phonenumber; }
            set { phonenumber = value; }
        }

        private string fax = "";
        public string Fax
        {
            get { return fax; }
            set { fax = value; }
        }

        private string firstaddress = "";
        public string Address
        {
            get { return firstaddress; }
            set { firstaddress = value; }
        }

        private string near = "";
        public string Near
        {
            get { return near; }
            set { near = value; }
        }

        internal Result CheckSellerInfo( ClientInformation ClientInfo)
        {
            Result result = new Result();

            try
            {
                if (SellerName == null || SellerName == "")
                {
                    result.HasError = true;
                    result.Message = "ESellerName";
                    return result;
                }

                if (PhoneNumber == null || PhoneNumber == "")
                {
                    result.HasError = true;
                    result.Message = "ErPhoneNumber";
                    return result;
                }

                if (Address == null || Address == "")
                {
                    result.HasError = true;
                    result.Message = "ErFirstAddress";
                    return result;
                }
            }
            catch (Exception e) { GlobalVar.GV.AddErrorMessage(e, ClientInfo); }

            return result;
        }
    }
}