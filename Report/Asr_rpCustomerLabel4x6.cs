using System;
using System.Drawing;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class Asr_rpCustomerLabel4x6 : DevExpress.XtraReports.UI.XtraReport
    {
        public Asr_rpCustomerLabel4x6()
        {
            InitializeComponent();
        }

        //private void imgCustomSymbol_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        //{
        //    var dv = Convert.ToDouble(GetCurrentColumnValue("DeclaredValue"));
        //    var er = Convert.ToDouble(GetCurrentColumnValue("ExchangeRate"));
        //    double DVusd = Math.Round(dv / er, 2);

        //    if (DVusd > 266.67)
        //        imgCustomSymbol.FillColor = Color.Black;
        //    else
        //        imgCustomSymbol.FillColor = Color.Transparent;
        //}
    }
}