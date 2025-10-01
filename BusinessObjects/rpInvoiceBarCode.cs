using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI.BusinessObjects
{
    public class rpInvoiceBarCode
    {
        public int ID { get; set; }
        public int ClientID { get; set; }
        public string Consignee { get; set; }
        public string ConsigneeMobileNo { get; set; }
        public string ConsigneePhone { get; set; }
        public string ConsigneeAddress { get; set; }
        public string ConsigneeEmail { get; set; }
        public string CountryofManufacture { get; set; }
        public string CurrencyCode { get; set; }
        public string CustomsCommodityCode { get; set; }
        public string Description { get; set; }
        public string InvoiceDate { get; set; }
        public string InvoiceNo { get; set; }
        public string Quantity { get; set; }
        public string RefNo { get; set; }
        public string SubTotal { get; set; }
        public string TotalCost { get; set; }
        public string UnitCost { get; set; }
        public string UnitType { get; set; }

    }
}