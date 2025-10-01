using System;
using System.Drawing;

namespace InfoTrack.NaqelAPI.Report
{//check
    public partial class rpCustomerLabel4x6ByAir : DevExpress.XtraReports.UI.XtraReport
    {
        public rpCustomerLabel4x6ByAir()
        {
            InitializeComponent();
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

        private void xrPictureBox3_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            var incotermID = Convert.ToInt32(GetCurrentColumnValue("Incoterm"));
            if (incotermID == 1) // DDU
                xrPictureBox3.Visible = true;
            else
                xrPictureBox3.Visible = false;
        }
    }
}
