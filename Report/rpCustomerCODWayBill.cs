using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Linq;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class rpCustomerCODWayBill : DevExpress.XtraReports.UI.XtraReport
    {
        //int ServiceTypeID = 0;
        public rpCustomerCODWayBill()
        {
            InitializeComponent();
            //this.StyleSheet.LoadFromFile(GlobalVar.GV.ReportStylePath);
        }

        private void pbIsCOD_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (dsView1.rpCustomerWaybill.Rows.Count > 0)
            {
                InfoTrack.NaqelAPI.App_Data.InfoTrackData.rpCustomerWaybillRow rowCustomerWaybill = null;
                rowCustomerWaybill = dsView1.rpCustomerWaybill.First(P => P.WayBillID == Convert.ToInt32(lbWaybillID.Text));
                if (!rowCustomerWaybill.IsCOD)
                {
                    pbIsCOD.Visible = false;
                    chCOD.Checked = false;
                    chShipper.Checked = true;
                    chRecipient.Checked = false;
                }
                else
                {
                    if (Convert.ToInt32(rowCustomerWaybill.CODCharge) <= 0)
                    {
                        chShipper.Checked = true;
                        chRecipient.Checked = false;
                    }
                    else
                    {
                        chRecipient.Checked = true;
                        chShipper.Checked = false;
                    }

                    pbIsCOD.Visible = true;
                    chCOD.Checked = true;
                }
                chInternational.Checked = false;
                chDomestic.Checked = false;
                chPallet.Checked = false;
                chExpress.Checked = false;
                chCourier.Checked = false;
                chOthers.Checked = false;

                if (Convert.ToInt32(rowCustomerWaybill.ServiceTypeID) == Convert.ToInt32(GlobalVar.ServiceTypes.International) || Convert.ToInt32(rowCustomerWaybill.ServiceTypeID) == Convert.ToInt32(GlobalVar.ServiceTypes.InternationalCourier))
                    chInternational.Checked = true;
                else
                    chDomestic.Checked = true;

                if (Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.Pallet))
                    chPallet.Checked = true;
                else if (Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.Express) || Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.ExpressIntl) || Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.ExpressDomestic))
                    chExpress.Checked = true;
                else if (Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.CourierIntl) || Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.Document) || Convert.ToInt32(rowCustomerWaybill.LoadTypeID) == Convert.ToInt32(GlobalVar.LoadTypes.NonDocument))
                    chCourier.Checked = true;
                else
                    chOthers.Checked = true;

                if (Convert.ToInt32(rowCustomerWaybill.PODTypeID) >= 0)
                    lbPODType.Text = rowCustomerWaybill.PODType;

                //CheckDeclareValue();
                AddCheckingNo(lbWaybillNo.Text);

                //double Res = 0;
                //Res = (Convert.ToDouble(lbWaybillNo.Text) % 7);
                //lbWaybillNo.Text = rowCustomerWaybill.WayBillNo + " - " + Convert.ToInt32(Res).ToString();
                //picBarCode.Text = lbWaybillNo.Text;
            }
        }

        private void AddCheckingNo(string WaybillNo)
        {
          //  CheckDeclareValue();
            if (WaybillNo.Length > 6)
            {
                //double validationNo = 0;
                //validationNo = Convert.ToDouble(WaybillNo) % 7;

                lbWaybillNo.Text = WaybillNo.ToString(); // + " - " + Convert.ToInt32(validationNo).ToString();
                //picBarCode.Text = WaybillNo.ToString() + Convert.ToInt32(validationNo).ToString();
            }
        }

        //private void CheckDeclareValue()
        //{
        //    //if (dsView1.rpCustomerWaybillwtihPieceBarCode.Rows.Count > 0)
        //    if (dsView1.rpCustomerWaybill.Rows.Count > 0)
        //    {
        //        InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext();
        //        ////int ClientID = (dsView1.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).ClientID;
        //        //InfoTrack.BusinessLayer.DContext.APIClientAccess instance = dcMaster.APIClientAccesses.First(P => P.ClientID == ClientID);
        //        //double declareValue = (dsView1.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).DeclaredValue;
        //        ////int waybillno = (dsView1.rpCustomerWaybillwtihPieceBarCode.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillwtihPieceBarCodeRow).WayBillNo;

        //        int ClientID = (dsView1.rpCustomerWaybill.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillRow).ClientID;
        //        int waybillno = (dsView1.rpCustomerWaybill.Rows[0] as App_Data.InfoTrackData.rpCustomerWaybillRow).WayBillNo;

        //        double NewDeclareValue = XMLShippingService.ExchangeRate(waybillno);
        //        if (NewDeclareValue > 266)
        //            imgCustomSymbol.FillColor = Color.Black;
        //        else
        //            imgCustomSymbol.FillColor = Color.Transparent;
        //    }
        //}

        private void imgCustomSymbol_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            InfoTrack.BusinessLayer.DContext.MastersDataContext dcMaster = new BusinessLayer.DContext.MastersDataContext();

            var data= dsView1.rpCustomerWaybill.Rows[CurrentRowIndex] as App_Data.InfoTrackData.rpCustomerWaybillRow;
            //var data = GetCurrentRow() as App_Data.InfoTrackData.rpCustomerWaybillRow;
           
            double NewDeclareValue = XMLShippingService.ExchangeRate(data.WayBillNo);
            if (NewDeclareValue > 266)
                imgCustomSymbol.FillColor = Color.Black;
            else
                imgCustomSymbol.FillColor = Color.Transparent;
        }
    }
}