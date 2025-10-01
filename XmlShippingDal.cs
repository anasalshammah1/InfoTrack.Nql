using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;
using DevExpress.XtraReports.UI;
using InfoTrack.BusinessLayer.DContext;
using InfoTrack.NaqelAPI.Class;
using InfoTrack.NaqelAPI.GeneralClass.Others;
using System.Data.SqlClient;

namespace InfoTrack.NaqelAPI
{
    public class XmlShippingDal
    {
        public List<ViwTracking> GetViewTracking(int waybillNo)
        {
            var list = new List<ViwTracking>();
            //try
            //{
            //    using (SqlConnection con = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
            //    {
            //        con.Open();
            //        var sql = " select * from ViwTracking where WaybillNo = " + waybillNo;
            //        SqlDataAdapter adap = new SqlDataAdapter(sql, con);
            //        DataSet ds = new DataSet();
            //        adap.Fill(ds);
            //        list = ds.Tables[0].ToList<ViwTracking>();
            //        adap.Dispose();
            //        con.Close();
            //    }
            //}
            //catch (Exception ex)
            //{

            //}

            return list;
        }
        public Byte[] GetAPIClientAccess(int clientId, int statusId)
        {
            var list = new List<APIClientAccess>();
            Byte[] passWord = null;
            DataTable dt;
            try
            {
                using (SqlConnection con = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
                {
                    con.Open();
                    var sql = " select top 1 ClientPassword from APIClientAccess where ClientID = " + clientId + " and StatusID = " + statusId;
                    SqlDataAdapter adap = new SqlDataAdapter(sql, con);
                    DataSet ds = new DataSet();
                    adap.Fill(ds);
                    if (ds.Tables[0].Rows.Count > 0)
                        passWord = ds.Tables[0].Rows[0][0] as Byte[];
                    // dt=ds.Tables[0] as DataTable;
                    //list = ds.Tables[0].ToList<APIClientAccess>();
                    //list = Extensions.ConvertDataTableToList<APIClientAccess>(ds.Tables[0]);
                    adap.Dispose();
                    con.Close();
                }
            }
            catch (Exception ex)
            {

            }
            //return list;
            return passWord;
        }

        public void InsertShippingAPIRequest(int ClientID, int apiType, string RefNo, int? key)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection()))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 60;
                    cmd.CommandText = "spInsertAPIRequest";
                    cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientID;
                    cmd.Parameters.Add("@APIRequestTypeID", SqlDbType.Int).Value = apiType;
                    cmd.Parameters.Add("@RefNo", SqlDbType.NVarChar).Value = RefNo;
                    if (key.HasValue)
                        cmd.Parameters.Add("@KeyID", SqlDbType.Int).Value = key.Value;
                    var result=cmd.ExecuteNonQuery();
                    con.Close();
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {

            }

            

        }
    }

}
