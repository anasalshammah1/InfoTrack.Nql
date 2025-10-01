using System;
using System.Drawing;

namespace InfoTrack.NaqelAPI.Report
{
    public partial class DunyanaLabel4x4 : DevExpress.XtraReports.UI.XtraReport
    {
        public DunyanaLabel4x4()
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
    }
}
