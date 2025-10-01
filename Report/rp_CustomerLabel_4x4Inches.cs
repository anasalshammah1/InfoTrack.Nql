using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class rp_CustomerLabel_4x4Inches : DevExpress.XtraReports.UI.XtraReport
    {
        public rp_CustomerLabel_4x4Inches()
        {
            InitializeComponent();
        }
        private void lbWaybillNo_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            AddCheckingNo();
        }

        private void lbWaybillNo_AfterPrint(object sender, EventArgs e)
        {
            AddCheckingNo();
        }

        private void picBarCode_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            AddCheckingNo();
        }

        private void picBarCode_AfterPrint(object sender, EventArgs e)
        {
            AddCheckingNo();
        }

        private void AddCheckingNo()
        {
            CheckDeclareValue();

            if (dsView.rpCustomerWaybillwtihPieceBarCode.Rows.Count > 0)
            {
                int WaybillNo = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).WayBillNo;

                //double validationNo = 0;
                //validationNo = WaybillNo % 7;

                lbWaybillNo.Text = WaybillNo.ToString(); // + " - " + Convert.ToInt32(validationNo).ToString();
                //picBarCode.Text = WaybillNo.ToString() + Convert.ToInt32(validationNo).ToString();
            }
        }

        private void CheckDeclareValue()
        {
            if (dsView.rpCustomerWaybillwtihPieceBarCode.Rows.Count > 0)
            {
                InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext();
                int ClientID = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).ClientID;
                //InfoTrack.BusinessLayer.DContext.APIClientAccess instance = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientID);
                int waybillno = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).WayBillNo;
                double NewDeclareValue = XMLShippingService.ExchangeRate(waybillno);
                if (NewDeclareValue > 266.67)
                    imgCustomSymbol.FillColor = Color.Black;
                else
                    imgCustomSymbol.FillColor = Color.Transparent;
            }
        }
    }
}
