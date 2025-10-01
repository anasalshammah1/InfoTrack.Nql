using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.GeneralClass.Others
{//check
    public class CommercialInvoice
    {
        public string RefNo = "";

        [System.ComponentModel.DefaultValue("0")]
        public string InvoiceNo = "";

        public DateTime InvoiceDate = System.DateTime.Now;
        public string Consignee = "";
        public string ConsigneeAddress = "";
        public string ConsigneeEmail = "";
        public string MobileNo = "";
        public string Phone = "";
        public float TotalCost = 0;
        public string CurrencyCode = "";
        
        public List<CommercialInvoiceDetail> CommercialInvoiceDetailList = new List<CommercialInvoiceDetail>();
    }

    public class CommercialInvoiceDetail
    {
        public int Quantity = 0;
        public string UnitType = "";
        public string CountryofManufacture = "";
        public string Description = "";
        public string ChineseDescription = "";
        public float UnitCost = 0;
        public string CustomsCommodityCode = "";
        public string Currency  = "";
        private float Amount = 0;

        public string SKU = "";
        public string CPC = "";
        public double ItemWeightUnit = 0;


    }
}