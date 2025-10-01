using System;
using System.Drawing;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class Asr_rpCustomerLabelA4 : DevExpress.XtraReports.UI.XtraReport
    {
        //int ServiceTypeID = 0;
        public Asr_rpCustomerLabelA4()
        {
            InitializeComponent();
        }

        private void imgCustomSymbol_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var dv = Convert.ToDouble(GetCurrentColumnValue("DeclaredValue"));
            var er = Convert.ToDouble(GetCurrentColumnValue("ExchangeRate"));
            double DVusd = Math.Round(dv / er, 2);

            if (DVusd > 266.67)
                imgCustomSymbol.FillColor = Color.Black;
            else
                imgCustomSymbol.FillColor = Color.Transparent;
        }

        private void Asr_rpCustomerLabelA4_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var IsCOD = Convert.ToBoolean(GetCurrentColumnValue("IsCOD"));
            if (!IsCOD)
            {
                pbIsCOD.Visible = false;
                chCOD.Checked = false;
                chShipper.Checked = true;
                chRecipient.Checked = false;
            }
            else
            {
                pbIsCOD.Visible = true;
                chCOD.Checked = true;

                var CODCharge = Convert.ToDouble(GetCurrentColumnValue("CODCharge"));
                if (CODCharge <= 0)
                {
                    chShipper.Checked = true;
                    chRecipient.Checked = false;
                }
                else
                {
                    chRecipient.Checked = true;
                    chShipper.Checked = false;
                }
            }

            chInternational.Checked = false;
            chDomestic.Checked = false;
            chPallet.Checked = false;
            chExpress.Checked = false;
            chCourier.Checked = false;
            chOthers.Checked = false;

            var ServiceTypeID = Convert.ToInt32(GetCurrentColumnValue("ServiceTypeID"));
            if (ServiceTypeID == Convert.ToInt32(GlobalVar.ServiceTypes.International) 
                || ServiceTypeID == Convert.ToInt32(GlobalVar.ServiceTypes.InternationalCourier))
                chInternational.Checked = true;
            else
                chDomestic.Checked = true;

            var LoadTypeID = Convert.ToInt32(GetCurrentColumnValue("LoadTypeID"));
            if (LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.Pallet))
                chPallet.Checked = true;
            else if (LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.Express) 
                || LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.ExpressIntl) 
                || LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.ExpressDomestic))
                chExpress.Checked = true;
            else if (LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.CourierIntl)
                || LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.Document) 
                || LoadTypeID == Convert.ToInt32(GlobalVar.LoadTypes.NonDocument))
                chCourier.Checked = true;
            else
                chOthers.Checked = true;

            var PODTypeID = Convert.ToInt32(GetCurrentColumnValue("PODTypeID"));
            var PODType = Convert.ToString(GetCurrentColumnValue("PODType"));
            if (PODTypeID >= 0)
                lbPODType.Text = PODType;

            //CheckDeclareValue();
            var WayBillNo = Convert.ToInt32(GetCurrentColumnValue("WayBillNo"));
            AddCheckingNo(WayBillNo.ToString());

        }

        private void AddCheckingNo(string WaybillNo)
        {
            //  CheckDeclareValue();
            if (WaybillNo.Length > 6)
            {
                double validationNo = 0;
                validationNo = Convert.ToDouble(WaybillNo) % 7;

                lbWaybillNo.Text = WaybillNo.ToString() + " - " + Convert.ToInt32(validationNo).ToString();
                //picBarCode.Text = WaybillNo.ToString() + Convert.ToInt32(validationNo).ToString();
            }
        }
    }
}