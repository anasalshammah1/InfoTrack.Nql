using System;
using System.Drawing;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class B2B_rpCustomerLabel4x6 : DevExpress.XtraReports.UI.XtraReport
    {
        public B2B_rpCustomerLabel4x6()
        {
            InitializeComponent();
        }

        private void xrPictureBox1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var loadTypeID = Convert.ToInt32(GetCurrentColumnValue("LoadTypeID"));
            if (loadTypeID == 161) // 2 Hours delivery
                xrPictureBox1.Visible = true;
            else
                xrPictureBox1.Visible = false;
        }

        private void exp_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var productTypeCode = Convert.ToString(GetCurrentColumnValue("ProductCode"));
            if (productTypeCode == "EXP")
                exp.Text = "EXP";
            else
                exp.Text = "";
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
    }
}
