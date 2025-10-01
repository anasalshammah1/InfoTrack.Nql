using System;
using System.Drawing;
using InfoTrack.NaqelAPI.BusinessObjects;
using System.Collections.Generic;
using System.Linq;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class rpCustomerLabelA4 : DevExpress.XtraReports.UI.XtraReport
    {
        public rpCustomerLabelA4()
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

        private void rpCustomerLabelA4_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var productTypeCode = Convert.ToString(GetCurrentColumnValue("ProductCode"));
            if (productTypeCode == "EXP")
                exp.Text = "EXP";
            else
                exp.Text = "";

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

                pbIsCOD.Visible = true;
                chCOD.Checked = true;
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

            string clientRedington = System.Configuration.ConfigurationManager.AppSettings["RedingtonClientIDs"].ToString();
            var clientID = Convert.ToString(GetCurrentColumnValue("ClientID"));
            var deliveryInstruction = Convert.ToString(GetCurrentColumnValue("DeliveryInstruction"));
            if (clientRedington.Split(',').Contains(clientID))
            {
                lb_totalValue.Text = "Total Value : " + deliveryInstruction;
            }
        }
    }
}
