using System;
using System.Linq;

using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using Dapper;
using System.Data.SqlClient;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class rpCustomerLabel_4_8Inches : DevExpress.XtraReports.UI.XtraReport
    {
        public rpCustomerLabel_4_8Inches()
        {
            InitializeComponent();
            CheckDeclareValue();
            //this.StyleSheet.LoadFromFile(GlobalVar.ReportStylePath);
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

            //int ExchangeRate = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).DeclaredValue

            if (dsView.rpCustomerWaybillwtihPieceBarCode.Rows.Count > 0)
            {
                int WaybillNo = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as InfoTrack.NaqelAPI.App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).WayBillNo;

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

                int ClientID = (dsView.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).ClientID;
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