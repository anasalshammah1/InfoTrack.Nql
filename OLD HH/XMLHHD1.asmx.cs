using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using InfoTrack.NaqelAPI.App_Data;
using InfoTrack.NaqelAPI.App_Data.InfoTrackSEDataTableAdapters;

namespace InfoTrack.NaqelAPI
{
    /// <summary>
    /// Summary description for XMLHHD
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/") ]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class XMLHHD : System.Web.Services.WebService
    {
        [WebMethod]
        public bool CheckConnectionToServerDB(byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                try
                {
                    conn.Open();
                    result = true;
                }
                catch (Exception x) { GlobalVar.GV.InsertError("1 - " + x.Message); }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("2 - " + x.Message); }
            return result;
        }

        [WebMethod]
        private bool CheckConnectionToServerDB()
        {
            bool result = false;
            try
            {
                System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                try
                {
                    conn.Open();
                    result = true;
                }
                catch (Exception x) { GlobalVar.GV.InsertError("3 - " + x.Message); }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("4 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public DateTime GetServerTime()
        {
            return DateTime.Now;
            //DateTime Result = new DateTime();

            //App_Data.InfoTrackDataTableAdapters.QueriesTableAdapter adapter =
            //    new App_Data.InfoTrackDataTableAdapters.QueriesTableAdapter();
            //Result = Convert.ToDateTime(adapter.CurrentTime());

            //return Result;
        }

        [WebMethod]
        public List<string> GetClientLoadType(byte[] EmpID, int ClientID)
        {
            List<string> Result = new List<string>();
            if (!GlobalVar.GV.IsSecure(EmpID)) return Result;

            App_Data.InfoTrackData.ViwLoadTypeByClientDataTable dt = new InfoTrackData.ViwLoadTypeByClientDataTable();

            App_Data.InfoTrackDataTableAdapters.ViwLoadTypeByClientTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.ViwLoadTypeByClientTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            
            adapter.FillByClientID(dt, ClientID);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                App_Data.InfoTrackData.ViwLoadTypeByClientRow row = dt.Rows[i] as App_Data.InfoTrackData.ViwLoadTypeByClientRow;
                Result.Add(row.Name);
            }

            return Result;
        }

        #region Send Data To Server

        [WebMethod]
        public bool SendPickUpDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    PickUpTableAdapter adapterH = new PickUpTableAdapter();
                    PickUpDetailTableAdapter adapterD = new PickUpDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    for (int i = 0; i < ds.PickUp.Rows.Count; i++)
                    {
                        InfoTrackSEData.PickUpRow CRow = ds.PickUp.Rows[i] as InfoTrackSEData.PickUpRow;
                        CRow.SetAdded();
                        if (CRow.IsCurrentVersionNull())
                            CRow.CurrentVersion = "";
                    }

                    for (int i = 0; i < ds.PickUpDetail.Rows.Count; i++)
                        (ds.PickUpDetail.Rows[i] as InfoTrackSEData.PickUpDetailRow).SetAdded();

                    try
                    {
                        adapterH.Update(ds.PickUp);

                        int ID = 0;
                        for (int i = 0; i < ds.PickUp.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.PickUpRow row = (ds.PickUp.Rows[i] as InfoTrackSEData.PickUpRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.WaybillNo, row.FromStationID, row.ToStationID, row.PieceCount, row.UserID, row.StationID, row.CurrentVersion != null ? row.CurrentVersion : ""));

                            for (int j = 0; j < ds.PickUpDetail.Rows.Count; j++)
                            {
                                if ((ds.PickUpDetail.Rows[j] as InfoTrackSEData.PickUpDetailRow).PickUpID == row.ID)
                                    (ds.PickUpDetail.Rows[j] as InfoTrackSEData.PickUpDetailRow).PickUpID = ID;
                            }
                        }
                        adapterD.Update(ds.PickUpDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception ww)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("5 - " + ww.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("6 - " + x.Message); }
            return result;
        }

        //[WebMethod]
        //public bool SendPickUpDataToServerUpdate(InfoTrackSEData ds, byte[] EmpID)
        //{
        //    bool result = false;
        //    try
        //    {
        //        if (!GlobalVar.GV.IsSecure(EmpID)) return result;
        //        if (CheckConnectionToServerDB())
        //        {
        //            PickUpTableAdapter adapterH = new PickUpTableAdapter();
        //            PickUpDetailTableAdapter adapterD = new PickUpDetailTableAdapter();
        //            System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetERPCourierSEConnection());
        //            c.Open();
        //            adapterH.Connection = c;
        //            adapterD.Connection = c;
        //                int ID = 0;
        //                for (int i = 0; i < ds.PickUp.Rows.Count; i++)
        //                {
        //                    var Transaction = c.BeginTransaction();
        //                    (ds.PickUp.Rows[i] as InfoTrackSEData.PickUpRow).SetAdded();  // Added one row 
        //                    adapterH.Update(ds.PickUp);

        //                    try
        //                    {

        //                        ID = 0;
        //                        InfoTrackSEData.PickUpRow row = (ds.PickUp.Rows[i] as InfoTrackSEData.PickUpRow);
        //                        ID = Convert.ToInt32(adapterH.GetID(row.WaybillNo, row.FromStationID, row.ToStationID, row.PieceCount,  row.StationID));
        //                        //mAdapter.Insert("New PickUp : ID = " + ID.ToString());
        //                        DataRow[] dRows= ds.PickUpDetail.Select(" PickUpID="+ row.ID);
        //                        foreach(DataRow dr in dRows)
        //                        {
        //                           dr["PickUpID"]=ID;
        //                           dr.SetAdded();
        //                        }
        //                        adapterD.Update(ds.PickUpDetail);
        //                        Transaction.Commit();
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                       Transaction.Rollback();
        //                       GlobalVar.GV.InsertError("5 - " + ex.Message);
        //                    }
        //                }
        //               c.Close();
        //              result = true;
        //            }

        //    }
        //    catch (Exception x) { GlobalVar.GV.InsertError("6 - " + x.Message); }
        //    return result;
        //}

        [WebMethod]
        public bool SendBookingWaybillDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    BookingWaybillTableAdapter adapterH = new BookingWaybillTableAdapter();
                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;

                    for (int i = 0; i < ds.BookingWaybill.Rows.Count; i++)
                        (ds.BookingWaybill.Rows[i] as InfoTrackSEData.BookingWaybillRow).SetAdded();
                    try
                    {
                        adapterH.Update(ds.BookingWaybill);
                        result = true;

                        T.Commit();
                        c.Close();
                    }
                    catch (Exception ww)
                    {
                        GlobalVar.GV.InsertError("An Error");
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("5 - " + ww.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("6 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendArrivalAtOrginDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    AtOriginTableAdapter adapterH = new AtOriginTableAdapter();
                    AtOriginDetailTableAdapter adapterD = new AtOriginDetailTableAdapter();
                    AtOriginWaybillDetailTableAdapter adapterDWaybill = new AtOriginWaybillDetailTableAdapter();
                    AtOriginConsDetailTableAdapter adapterDCons = new AtOriginConsDetailTableAdapter();
                    
                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDWaybill.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDCons.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;
                    adapterDWaybill.Connection = c;
                    adapterDCons.Connection = c;

                    for (int i = 0; i < ds.AtOriginDetail.Rows.Count; i++)
                        (ds.AtOriginDetail.Rows[i] as InfoTrackSEData.AtOriginDetailRow).SetAdded();
                    for (int i = 0; i < ds.AtOriginWaybillDetail.Rows.Count; i++)
                        (ds.AtOriginWaybillDetail.Rows[i] as InfoTrackSEData.AtOriginWaybillDetailRow).SetAdded();
                    for (int i = 0; i < ds.AtOriginConsDetail.Rows.Count; i++)
                        (ds.AtOriginConsDetail.Rows[i] as InfoTrackSEData.AtOriginConsDetailRow).SetAdded();

                    try
                    {
                        for (int i = 0; i < ds.AtOrigin.Rows.Count; i++)
                        {
                            InfoTrackSEData.AtOriginRow row = ds.AtOrigin.Rows[i] as InfoTrackSEData.AtOriginRow;
                            adapterH.Insert(row.CourierID, row.UserID, row.IsSync, row.CTime, row.PieceCount, row.ID, row.WaybillCount, row.StationID);
                        }
                        int ID = 0;
                        for (int i = 0; i < ds.AtOrigin.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.AtOriginRow row = (ds.AtOrigin.Rows[i] as InfoTrackSEData.AtOriginRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.CourierID, row.UserID, row.PieceCount, row.WaybillCount, row.StationID, row.CTime, row.IDs));
                            for (int j = 0; j < ds.AtOriginDetail.Rows.Count; j++)
                                if ((ds.AtOriginDetail.Rows[j] as InfoTrackSEData.AtOriginDetailRow).AtOriginID == row.ID)
                                    (ds.AtOriginDetail.Rows[j] as InfoTrackSEData.AtOriginDetailRow).AtOriginID = ID;
                            for (int j = 0; j < ds.AtOriginWaybillDetail.Rows.Count; j++)
                                if ((ds.AtOriginWaybillDetail.Rows[j] as InfoTrackSEData.AtOriginWaybillDetailRow).AtOriginID == row.ID)
                                    (ds.AtOriginWaybillDetail.Rows[j] as InfoTrackSEData.AtOriginWaybillDetailRow).AtOriginID = ID;
                            for (int j = 0; j < ds.AtOriginConsDetail.Rows.Count; j++)
                                if ((ds.AtOriginConsDetail.Rows[j] as InfoTrackSEData.AtOriginConsDetailRow).AtOriginID == row.ID)
                                    (ds.AtOriginConsDetail.Rows[j] as InfoTrackSEData.AtOriginConsDetailRow).AtOriginID = ID;
                        }
                        adapterD.Update(ds.AtOriginDetail);
                        adapterDWaybill.Update(ds.AtOriginWaybillDetail);
                        adapterDCons.Update(ds.AtOriginConsDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("50 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("7 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendConsDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    ConsTableAdapter adapterH = new ConsTableAdapter();
                    ConsDetailTableAdapter adapterD = new ConsDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    for (int i = 0; i < ds.ConsDetail.Rows.Count; i++)
                        (ds.ConsDetail.Rows[i] as InfoTrackSEData.ConsDetailRow).SetAdded();
                    try
                    {
                        for (int i = 0; i < ds.Cons.Rows.Count; i++)
                        {
                            InfoTrackSEData.ConsRow row = ds.Cons.Rows[i] as InfoTrackSEData.ConsRow;
                            adapterH.Insert(row.StationID, row.ConsNo, row.IsSync, row.EmployID, row.Date, row.CurrentVersion, row.IsServerTime, row.StatusID,row.WaybillCount);
                        }
                        int ID = 0;
                        for (int i = 0; i < ds.Cons.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.ConsRow row = (ds.Cons.Rows[i] as InfoTrackSEData.ConsRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.StationID, row.ConsNo, row.IsSync, row.EmployID, row.Date, row.CurrentVersion, row.IsServerTime,row.StatusID,row.WaybillCount));
                            for (int j = 0; j < ds.ConsDetail.Rows.Count; j++)
                                if ((ds.ConsDetail.Rows[j] as InfoTrackSEData.ConsDetailRow).ConsID == row.ID)
                                    (ds.ConsDetail.Rows[j] as InfoTrackSEData.ConsDetailRow).ConsID = ID;
                        }
                        adapterD.Update(ds.ConsDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("501 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("7 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendWaybillWeightDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    WaybillWeightTableAdapter adapter = new WaybillWeightTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.WaybillWeight.Rows.Count; i++)
                        (ds.WaybillWeight.Rows[i] as InfoTrackSEData.WaybillWeightRow).SetAdded();

                    try
                    {
                        adapter.Update(ds.WaybillWeight);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("51 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("8 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendConsWaybillDeletingDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    ConsWaybillDeletingTableAdapter adapter = new ConsWaybillDeletingTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.ConsWaybillDeleting.Rows.Count; i++)
                        (ds.ConsWaybillDeleting.Rows[i] as InfoTrackSEData.ConsWaybillDeletingRow).SetAdded();

                    try
                    {
                        adapter.Update(ds.ConsWaybillDeleting);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("51 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("8 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendMesurementDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    App_Data.InfoTrackSEDataTableAdapters.MeasurementsTableAdapter adapter = new MeasurementsTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.Measurements.Rows.Count; i++)
                        (ds.Measurements.Rows[i] as InfoTrackSEData.MeasurementsRow).SetAdded();

                    try
                    {
                        adapter.Update(ds.Measurements);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("52 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("9 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendUserMeLoginsDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    App_Data.InfoTrackSEDataTableAdapters.UserMeLoginTableAdapter adapter = new UserMeLoginTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.UserMeLogin.Rows.Count; i++)
                        try
                        {
                            (ds.UserMeLogin.Rows[i] as InfoTrackSEData.UserMeLoginRow).SetAdded();
                        }
                        catch (Exception mc) { GlobalVar.GV.InsertError(mc.Message); }
                    try
                    {
                        adapter.Update(ds.UserMeLogin);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("520 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("9 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendBorderCheckingDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    App_Data.InfoTrackSEDataTableAdapters.BorderCheckingTableAdapter adapter = new BorderCheckingTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.BorderChecking.Rows.Count; i++)
                        try
                        {
                            (ds.BorderChecking.Rows[i] as InfoTrackSEData.BorderCheckingRow).SetAdded();
                        }
                        catch (Exception mc) { GlobalVar.GV.InsertError(mc.Message); }
                    try
                    {
                        adapter.Update(ds.BorderChecking);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("SendBorderCheckingDataToServer1 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("SendBorderCheckingDataToServer 2 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendOnLoadingDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    OnLoadingTableAdapter adapterH = new OnLoadingTableAdapter();
                    OnLoadingDetailTableAdapter adapterD = new OnLoadingDetailTableAdapter();
                    OnLoadingWaybillDetailTableAdapter adapterDWaybill = new OnLoadingWaybillDetailTableAdapter();
                    OnLoadingConsDetailTableAdapter adapterDCons = new OnLoadingConsDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDWaybill.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDCons.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;
                    adapterDWaybill.Connection = c;
                    adapterDCons.Connection = c;

                    for (int i = 0; i < ds.OnLoadingDetail.Rows.Count; i++)
                        (ds.OnLoadingDetail.Rows[i] as InfoTrackSEData.OnLoadingDetailRow).SetAdded();
                    for (int i = 0; i < ds.OnLoadingWaybillDetail.Rows.Count; i++)
                        (ds.OnLoadingWaybillDetail.Rows[i] as InfoTrackSEData.OnLoadingWaybillDetailRow).SetAdded();
                    for (int i = 0; i < ds.OnLoadingConsDetail.Rows.Count; i++)
                        (ds.OnLoadingConsDetail.Rows[i] as InfoTrackSEData.OnLoadingConsDetailRow).SetAdded();

                    try
                    {
                        for (int i = 0; i < ds.OnLoading.Rows.Count; i++)
                        {
                            InfoTrackSEData.OnLoadingRow row = ds.OnLoading.Rows[i] as InfoTrackSEData.OnLoadingRow;
                            adapterH.Insert(row.UserID, row.IsSync, row.CTime, row.PieceCount, row.TrailerNo, row.WaybillCount, row.ID, false, row.StationID);
                        }
                        int ID = 0;
                        for (int i = 0; i < ds.OnLoading.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.OnLoadingRow row = (ds.OnLoading.Rows[i] as InfoTrackSEData.OnLoadingRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.IsSync, row.UserID, false, row.StationID, row.IDs, row.TrailerNo, row.WaybillCount, row.PieceCount, row.CTime));
                            for (int j = 0; j < ds.OnLoadingDetail.Rows.Count; j++)
                                if ((ds.OnLoadingDetail.Rows[j] as InfoTrackSEData.OnLoadingDetailRow).OnLoadingID == row.ID)
                                    (ds.OnLoadingDetail.Rows[j] as InfoTrackSEData.OnLoadingDetailRow).OnLoadingID = ID;
                            for (int j = 0; j < ds.OnLoadingWaybillDetail.Rows.Count; j++)
                                if ((ds.OnLoadingWaybillDetail.Rows[j] as InfoTrackSEData.OnLoadingWaybillDetailRow).OnLoadingID == row.ID)
                                    (ds.OnLoadingWaybillDetail.Rows[j] as InfoTrackSEData.OnLoadingWaybillDetailRow).OnLoadingID = ID;
                            for (int j = 0; j < ds.OnLoadingConsDetail.Rows.Count; j++)
                                if ((ds.OnLoadingConsDetail.Rows[j] as InfoTrackSEData.OnLoadingConsDetailRow).OnLoadingID == row.ID)
                                    (ds.OnLoadingConsDetail.Rows[j] as InfoTrackSEData.OnLoadingConsDetailRow).OnLoadingID = ID;
                        }
                        adapterD.Update(ds.OnLoadingDetail);
                        adapterDWaybill.Update(ds.OnLoadingWaybillDetail);
                        adapterDCons.Update(ds.OnLoadingConsDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("53 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("10 - " + x.Message); }
            return result;
        }

        //*********************************Visible Loading Factor ( Coded by Anil ****************

        [WebMethod]
        public bool SendLoadVisibleFactorDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    App_Data.InfoTrackSEDataTableAdapters.DeliverySheetVisibleFactorTableAdapter adapter =
                        new DeliverySheetVisibleFactorTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c =
                        new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;

                    for (int i = 0; i < ds.DeliverySheetVisibleFactor.Rows.Count; i++)
                        (ds.DeliverySheetVisibleFactor.Rows[i] as InfoTrackSEData.DeliverySheetVisibleFactorRow).SetAdded();
                    try
                    {
                        adapter.Update(ds.DeliverySheetVisibleFactor);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("520 -Visual Factor " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("9 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public DataTable GetTruckNo(int DeliverySheetID, int EmployeeID)
        {
            DataSet ds = new DataSet();

            DataTable tbl = new DataTable("Info");
            tbl.Columns.Add("TruckNo");
            tbl.Columns.Add("TotalWeight");
            tbl.Columns.Add("TotalWayBill");
            tbl.Columns.Add("TotalPcs");
            tbl.Columns.Add("CourierName");
            tbl.Columns.Add("SignedOff");


            DataTable tbl1 = new DataTable("Temp");
            tbl1.Columns.Add("Name");

            System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackConnection());
            c.Open();

            string sqlstr = "Select Truck.Name as TruckNo,TotalWeight,TotalWaybill,TotalPieces,Employ.name as CourierName " +
                " from DeliverySheet left join Courier on Courier.ID=DeliverySheet.CourierID " +
                " left join Employ on Employ.ID=Courier.EmployID " +
                " left join Truck on Truck.ID=DeliverySheet.TruckID " +
                " where DeliverySheet.ID=" + DeliverySheetID.ToString();
            SqlDataAdapter adp = new SqlDataAdapter(sqlstr, c);
            
            adp.Fill(ds);
            if (ds.Tables.Count > 0)
            {
                DataRow dr = tbl.NewRow();
                dr["TruckNo"] = ds.Tables[0].Rows[0]["TruckNo"].ToString();
                dr["TotalWeight"] = ds.Tables[0].Rows[0]["TotalWeight"].ToString();
                dr["TotalWayBill"] = ds.Tables[0].Rows[0]["TotalWaybill"].ToString();
                dr["CourierName"] = ds.Tables[0].Rows[0]["CourierName"].ToString();
                dr["TotalPcs"] = ds.Tables[0].Rows[0]["TotalPieces"].ToString();

                tbl.Rows.Add(dr);
            }

            adp = new SqlDataAdapter("Select Name from Employ where ID=" + EmployeeID.ToString(), c);
            adp.Fill(tbl1);
            if (tbl1.Rows.Count > 0)
                tbl.Rows[0]["SignedOff"] = tbl1.Rows[0][0].ToString();

            c.Close();
            return tbl;
        }

        //[WebMethod]
        //public bool UpdateInboundDataToPickup(InfoTrackSEData.tempInboundDataTable ManifestDetails,
        //                                      InfoTrackSEData.tempInboundDetailsDataTable ManifestPieces,
        //                                      string EmployeeID)
        //{
        //    bool result = false;
        //    try
        //    {


        //        if (CheckConnectionToServerDB())
        //        {
        //            PickUpTableAdapter adapterH = new PickUpTableAdapter();
        //            PickUpDetailTableAdapter adapterD = new PickUpDetailTableAdapter();
        //            System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetERPCourierSEConnection());
        //            c.Open();

        //            DataTable tblUser = GlobalVar.GV.GetDataTable("Select ID from UserMELogin where EmployID=" + EmployeeID, c);

        //            if (tblUser.Rows.Count == 0)
        //                return result;

        //            System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
        //            c.EnlistTransaction(T);
        //            adapterH.Connection = c;
        //            adapterD.Connection = c;

        //            for (int i = 0; i < ManifestDetails.Rows.Count; i++)
        //            {
        //                adapterH.Insert(ManifestDetails.Rows[i]["WayBillNo"].ToString(),
        //                                0, 528, 528,
        //                int.Parse(ManifestDetails.Rows[i]["PieceCount"].ToString()),
        //                double.Parse(ManifestDetails.Rows[i]["Weight"].ToString()),
        //                DateTime.Now, DateTime.Now, false,
        //                int.Parse(tblUser.Rows[0][0].ToString()),
        //                528, "Inboud");
        //            }



        //            try
        //            {

        //                for (int i = 0; i < ManifestPieces.Rows.Count; i++)
        //                {

        //                    DataTable tbl = GlobalVar.GV.GetDataTable("Select ID from PickUp where WayBillNo=" + ManifestPieces.Rows[i]["WayBillNo"].ToString() +
        //                                  " PieceCount=" + ManifestPieces.Rows[i]["PieceCount"].ToString(), c);
        //                    if (tbl.Rows.Count > 0)
        //                    {
        //                        adapterD.Insert(ManifestPieces.Rows[i]["PieceBarcode"].ToString(),
        //                                     int.Parse(tbl.Rows[0][0].ToString()),
        //                                     false);
        //                    }

        //                }


        //                T.Commit();
        //                c.Close();
        //                result = true;
        //            }
        //            catch (Exception ww)
        //            {
        //                T.Rollback();
        //                result = false;
        //                c.Close();
        //                GlobalVar.GV.InsertError("5 - " + ww.Message);
        //            }
        //        }
        //    }
        //    catch (Exception x) { GlobalVar.GV.InsertError("6 - " + x.Message); }
        //    return result;
        //}






        // *********************************Visible Loading Factor****************

        [WebMethod]
        public bool SendAtDestinationDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    AtDestinationTableAdapter adapterH = new AtDestinationTableAdapter();
                    AtDestinationDetailTableAdapter adapterD = new AtDestinationDetailTableAdapter();
                    AtDestinationWaybillDetailTableAdapter adapterDWaybill = new AtDestinationWaybillDetailTableAdapter();
                    AtDestinationConsDetailTableAdapter adapterDCons = new AtDestinationConsDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDWaybill.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDCons.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;
                    adapterDWaybill.Connection = c;
                    adapterDCons.Connection = c;

                    for (int i = 0; i < ds.AtDestinationDetail.Rows.Count; i++)
                        (ds.AtDestinationDetail.Rows[i] as InfoTrackSEData.AtDestinationDetailRow).SetAdded();
                    for (int i = 0; i < ds.AtDestinationWaybillDetail.Rows.Count; i++)
                        (ds.AtDestinationWaybillDetail.Rows[i] as InfoTrackSEData.AtDestinationWaybillDetailRow).SetAdded();
                    for (int i = 0; i < ds.AtDestinationConsDetail.Rows.Count; i++)
                        (ds.AtDestinationConsDetail.Rows[i] as InfoTrackSEData.AtDestinationConsDetailRow).SetAdded();
                    try
                    {
                        for (int i = 0; i < ds.AtDestination.Rows.Count; i++)
                        {
                            InfoTrackSEData.AtDestinationRow row = ds.AtDestination.Rows[i] as InfoTrackSEData.AtDestinationRow;
                            adapterH.Insert(row.UserID, row.IsSync, row.CTime, row.PieceCount, row.TrailerNo, row.WaybillCount, row.ID, row.StationID);
                        }
                        int ID = 0;
                        for (int i = 0; i < ds.AtDestination.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.AtDestinationRow row = (ds.AtDestination.Rows[i] as InfoTrackSEData.AtDestinationRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.UserID, row.IsSync, row.CTime, row.PieceCount, row.TrailerNo, row.WaybillCount, row.IDs, row.StationID));
                            for (int j = 0; j < ds.AtDestinationDetail.Rows.Count; j++)
                                if ((ds.AtDestinationDetail.Rows[j] as InfoTrackSEData.AtDestinationDetailRow).AtDestinationID == row.ID)
                                    (ds.AtDestinationDetail.Rows[j] as InfoTrackSEData.AtDestinationDetailRow).AtDestinationID = ID;
                            for (int j = 0; j < ds.AtDestinationWaybillDetail.Rows.Count; j++)
                                if ((ds.AtDestinationWaybillDetail.Rows[j] as InfoTrackSEData.AtDestinationWaybillDetailRow).AtDestinationID == row.ID)
                                    (ds.AtDestinationWaybillDetail.Rows[j] as InfoTrackSEData.AtDestinationWaybillDetailRow).AtDestinationID = ID;
                            for (int j = 0; j < ds.AtDestinationConsDetail.Rows.Count; j++)
                                if ((ds.AtDestinationConsDetail.Rows[j] as InfoTrackSEData.AtDestinationConsDetailRow).AtDestinationID == row.ID)
                                    (ds.AtDestinationConsDetail.Rows[j] as InfoTrackSEData.AtDestinationConsDetailRow).AtDestinationID = ID;
                        }
                        adapterD.Update(ds.AtDestinationDetail);
                        adapterDWaybill.Update(ds.AtDestinationWaybillDetail);
                        adapterDCons.Update(ds.AtDestinationConsDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("45 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("11 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendOnCLoadingForDDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;

            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    OnCloadingForDTableAdapter adapterH = new OnCloadingForDTableAdapter();
                    OnCLoadingForDDetailTableAdapter adapterD = new OnCLoadingForDDetailTableAdapter();
                    OnCLoadingForDWaybillTableAdapter adapterDWaybill = new OnCLoadingForDWaybillTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterDWaybill.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;
                    adapterDWaybill.Connection = c;

                    for (int i = 0; i < ds.OnCLoadingForDDetail.Rows.Count; i++)
                        (ds.OnCLoadingForDDetail.Rows[i] as InfoTrackSEData.OnCLoadingForDDetailRow).SetAdded();
                    for (int i = 0; i < ds.OnCLoadingForDWaybill.Rows.Count; i++)
                        (ds.OnCLoadingForDWaybill.Rows[i] as InfoTrackSEData.OnCLoadingForDWaybillRow).SetAdded();
                    try
                    {
                        for (int i = 0; i < ds.OnCloadingForD.Rows.Count; i++)
                        {
                            InfoTrackSEData.OnCloadingForDRow row = ds.OnCloadingForD.Rows[i] as InfoTrackSEData.OnCloadingForDRow;
                            row.IDs = row.ID;
                            if (row.IsTruckIDNull())
                                row.TruckID = "0";
                            adapterH.Insert(row.CourierID, row.UserID, row.IsSync, row.CTime, row.PieceCount, row.TruckID, row.IDs, row.WaybillCount, row.StationID);
                        }

                        int ID = 0;
                        for (int i = 0; i < ds.OnCloadingForD.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.OnCloadingForDRow row = (ds.OnCloadingForD.Rows[i] as InfoTrackSEData.OnCloadingForDRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.CourierID, row.UserID, row.IsSync, row.CTime, row.PieceCount, row.TruckID, row.IDs, row.WaybillCount, row.StationID));

                            for (int j = 0; j < ds.OnCLoadingForDDetail.Rows.Count; j++)
                                if ((ds.OnCLoadingForDDetail.Rows[j] as InfoTrackSEData.OnCLoadingForDDetailRow).OnCLoadingForDID == row.ID)
                                    (ds.OnCLoadingForDDetail.Rows[j] as InfoTrackSEData.OnCLoadingForDDetailRow).OnCLoadingForDID = ID;
                            for (int j = 0; j < ds.OnCLoadingForDWaybill.Rows.Count; j++)
                                if ((ds.OnCLoadingForDWaybill.Rows[j] as InfoTrackSEData.OnCLoadingForDWaybillRow).OnCLoadingID == row.ID)
                                    (ds.OnCLoadingForDWaybill.Rows[j] as InfoTrackSEData.OnCLoadingForDWaybillRow).OnCLoadingID = ID;
                        }
                        adapterD.Update(ds.OnCLoadingForDDetail);
                        adapterDWaybill.Update(ds.OnCLoadingForDWaybill);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception exx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("100 - " + exx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("12 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendOnDeliveryDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    OnDeliveryTableAdapter adapterH = new OnDeliveryTableAdapter();
                    OnDeliveryDetailTableAdapter adapterD = new OnDeliveryDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    for (int i = 0; i < ds.OnDelivery.Rows.Count; i++)
                        (ds.OnDelivery.Rows[i] as InfoTrackSEData.OnDeliveryRow).SetAdded();
                    for (int i = 0; i < ds.OnDeliveryDetail.Rows.Count; i++)
                        (ds.OnDeliveryDetail.Rows[i] as InfoTrackSEData.OnDeliveryDetailRow).SetAdded();
                    try
                    {
                        adapterH.Update(ds.OnDelivery);
                        int ID = 0;

                        for (int i = 0; i < ds.OnDelivery.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.OnDeliveryRow row = (ds.OnDelivery.Rows[i] as InfoTrackSEData.OnDeliveryRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.StationID, row.IsSync, row.UserID, row.TimeOut, row.TimeIn, row.PiecesCount, row.ReceiverName, row.WaybillNo));
                            for (int j = 0; j < ds.OnDeliveryDetail.Rows.Count; j++)
                                if ((ds.OnDeliveryDetail.Rows[j] as InfoTrackSEData.OnDeliveryDetailRow).DeliveryID == row.ID)
                                    (ds.OnDeliveryDetail.Rows[j] as InfoTrackSEData.OnDeliveryDetailRow).DeliveryID = ID;
                        }
                        adapterD.Update(ds.OnDeliveryDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("55 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("13 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendMultiDeliveryDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    MultiDeliveryTableAdapter adapterH = new MultiDeliveryTableAdapter();
                    MultiDeliveryDetailTableAdapter adapterD = new MultiDeliveryDetailTableAdapter();
                    MultiDeliveryWaybillDetailTableAdapter adapterW = new MultiDeliveryWaybillDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterW.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;
                    adapterW.Connection = c;

                    for (int i = 0; i < ds.MultiDelivery.Rows.Count; i++)
                        (ds.MultiDelivery.Rows[i] as InfoTrackSEData.MultiDeliveryRow).SetAdded();
                    for (int i = 0; i < ds.MultiDeliveryDetail.Rows.Count; i++)
                        (ds.MultiDeliveryDetail.Rows[i] as InfoTrackSEData.MultiDeliveryDetailRow).SetAdded();
                    for (int i = 0; i < ds.MultiDeliveryWaybillDetail.Rows.Count; i++)
                        (ds.MultiDeliveryWaybillDetail.Rows[i] as InfoTrackSEData.MultiDeliveryWaybillDetailRow).SetAdded();
                    try
                    {
                        adapterH.Update(ds.MultiDelivery);
                        int ID = 0;

                        for (int i = 0; i < ds.MultiDelivery.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.MultiDeliveryRow row = (ds.MultiDelivery.Rows[i] as InfoTrackSEData.MultiDeliveryRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.IsSync, row.ReceiverName, row.PiecesCount, row.TimeIn, row.TimeOut, row.UserID, row.StationID, row.WaybillsCount));

                            for (int j = 0; j < ds.MultiDeliveryDetail.Rows.Count; j++)
                                if ((ds.MultiDeliveryDetail.Rows[j] as InfoTrackSEData.MultiDeliveryDetailRow).MultiDeliveryID == row.ID)
                                    (ds.MultiDeliveryDetail.Rows[j] as InfoTrackSEData.MultiDeliveryDetailRow).MultiDeliveryID = ID;

                            for (int j = 0; j < ds.MultiDeliveryWaybillDetail.Rows.Count; j++)
                                if ((ds.MultiDeliveryWaybillDetail.Rows[j] as InfoTrackSEData.MultiDeliveryWaybillDetailRow).MultiDeliveryID == row.ID)
                                    (ds.MultiDeliveryWaybillDetail.Rows[j] as InfoTrackSEData.MultiDeliveryWaybillDetailRow).MultiDeliveryID = ID;
                        }
                        adapterD.Update(ds.MultiDeliveryDetail);
                        adapterW.Update(ds.MultiDeliveryWaybillDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("255 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("130 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendNotDeliveredDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    NotDeliveredTableAdapter adapterH = new NotDeliveredTableAdapter();
                    NotDeliveredDetailTableAdapter adapterD = new NotDeliveredDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    for (int i = 0; i < ds.NotDelivered.Rows.Count; i++)
                        (ds.NotDelivered.Rows[i] as InfoTrackSEData.NotDeliveredRow).SetAdded();
                    for (int i = 0; i < ds.NotDeliveredDetail.Rows.Count; i++)
                        (ds.NotDeliveredDetail.Rows[i] as InfoTrackSEData.NotDeliveredDetailRow).SetAdded();
                    try
                    {
                        adapterH.Update(ds.NotDelivered);
                        int ID = 0;
                        for (int i = 0; i < ds.NotDelivered.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.NotDeliveredRow row = (ds.NotDelivered.Rows[i] as InfoTrackSEData.NotDeliveredRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.WaybillNo, row.TimeIn, row.TimeOut, row.UserID, row.IsSync, row.StationID, row.PiecesCount, row.DeliveryStatusID));
                            for (int j = 0; j < ds.NotDeliveredDetail.Rows.Count; j++)
                                if ((ds.NotDeliveredDetail.Rows[j] as InfoTrackSEData.NotDeliveredDetailRow).NotDeliveredID == row.ID)
                                    (ds.NotDeliveredDetail.Rows[j] as InfoTrackSEData.NotDeliveredDetailRow).NotDeliveredID = ID;
                        }
                        adapterD.Update(ds.NotDeliveredDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("56 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("14 -" + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendNightStockDataToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    NightStockTableAdapter adapterH = new NightStockTableAdapter();
                    NightStockDetailTableAdapter adapterD = new NightStockDetailTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    for (int i = 0; i < ds.NightStockDetail.Rows.Count; i++)
                        (ds.NightStockDetail.Rows[i] as InfoTrackSEData.NightStockDetailRow).SetAdded();
                    try
                    {
                        for (int i = 0; i < ds.NightStock.Rows.Count; i++)
                        {
                            InfoTrackSEData.NightStockRow row = ds.NightStock.Rows[i] as InfoTrackSEData.NightStockRow;
                            adapterH.Insert(row.UserID, row.IsSync, row.CTime, row.PieceCount, row.StationID, row.ID);
                        }
                        int ID = 0;
                        for (int i = 0; i < ds.NightStock.Rows.Count; i++)
                        {
                            ID = 0;
                            InfoTrackSEData.NightStockRow row = (ds.NightStock.Rows[i] as InfoTrackSEData.NightStockRow);
                            ID = Convert.ToInt32(adapterH.GetID(row.UserID, row.IsSync, row.CTime, row.PieceCount, row.StationID, row.IDs));
                            for (int j = 0; j < ds.NightStockDetail.Rows.Count; j++)
                                if ((ds.NightStockDetail.Rows[j] as InfoTrackSEData.NightStockDetailRow).NightStockID == row.ID)
                                    (ds.NightStockDetail.Rows[j] as InfoTrackSEData.NightStockDetailRow).NightStockID = ID;
                        }
                        adapterD.Update(ds.NightStockDetail);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("57 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("15 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public bool SendUserLogsToServer(InfoTrackSEData ds, byte[] EmpID)
        {
            bool result = false;
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    UserLogsTableAdapter adapter = new UserLogsTableAdapter();
                    adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackSEConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapter.Connection = c;
                    for (int i = 0; i < ds.UserLogs.Rows.Count; i++)
                        (ds.UserLogs.Rows[i] as InfoTrackSEData.UserLogsRow).SetAdded();
                    try
                    {
                        adapter.Update(ds.UserLogs);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        result = false;
                        c.Close();
                        GlobalVar.GV.InsertError("58 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("16 - " + x.Message); }
            return result;
        }

        [WebMethod]
        public void RegisterLogin(byte[] EmpID, int EmployID, int StateID, string HHDName, string SystemVersion)
        {
            if (!GlobalVar.GV.IsSecure(EmpID)) return;
            App_Data.InfoTrackDataTableAdapters.UserMeLoginTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.UserMeLoginTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
            adapter.Insert(EmployID, StateID, null, HHDName, SystemVersion);
        }

        #endregion

        #region Bring Data To Machine

        [WebMethod]
        public InfoTrackData.StationDataTable BringStationFromServer(byte[] EmpID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.StationTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.StationTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.StationDataTable table = new InfoTrackData.StationDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillByStatusID(table);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("17" + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.RouteDataTable BringRouteFromServer(byte[] EmpID, int StationID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.RouteTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.RouteTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.RouteDataTable table = new InfoTrackData.RouteDataTable();

            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillByStationID(table, StationID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("18" + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.BorderClearenceDataTable BringBordersFromServer(byte[] EmpID, int StationID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.BorderClearenceTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.BorderClearenceTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.BorderClearenceDataTable table = new InfoTrackData.BorderClearenceDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillNotDeleted(table);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("BringBordersFromServer - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.ViwTmpPiecesListFDSDataTable BringViwTmpPiecesListFDSByStation(byte[] EmpID, int StationID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFDSTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFDSTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.ViwTmpPiecesListFDSDataTable table = new InfoTrackData.ViwTmpPiecesListFDSDataTable();
            if (!GlobalVar.GV.IsSecure(EmpID)) return table;

            try
            {
                Adapter.FillByStationID(table, StationID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("19 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.HHViwDeliverySheetDataTable BringHHViwDeliverySheet(byte[] EmpID, int DeliverySheetID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.HHViwDeliverySheetTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.HHViwDeliverySheetTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.HHViwDeliverySheetDataTable table = new InfoTrackData.HHViwDeliverySheetDataTable();
            if (!GlobalVar.GV.IsSecure(EmpID)) return table;
            try
            {
                Adapter.FillByDeliverySheetID(table, DeliverySheetID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("20 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.HHViwPUpFArriveTOrgDataTable BringHHViwPUpFArriveTOrg(byte[] EmpID, int EmployID, DateTime FromDate, DateTime ToDate)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.HHViwPUpFArriveTOrgTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.HHViwPUpFArriveTOrgTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.HHViwPUpFArriveTOrgDataTable table = new InfoTrackData.HHViwPUpFArriveTOrgDataTable();
            if (!GlobalVar.GV.IsSecure(EmpID)) return table;
            try
            {
                //GlobalVar.GV.InsertError(FromDate.ToString());
                //GlobalVar.GV.InsertError(ToDate.ToString());
                //GlobalVar.GV.InsertError(EmployID.ToString());
                Adapter.FillByEmployID(table, EmployID, FromDate, ToDate);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("20 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.ViwTmpPiecesListFDSDataTable BringViwTmpPiecesListFDSByRoute(byte[] EmpID, int StationID, int RouteID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFDSTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFDSTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.ViwTmpPiecesListFDSDataTable table = new InfoTrackData.ViwTmpPiecesListFDSDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillByRouteID(table, StationID, RouteID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("21 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.ViwTmpPiecesListFTPDataTable BringViwTmpPiecesListFTBByDays(byte[] EmpID, int StationID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFTPTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.ViwTmpPiecesListFTPTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.ViwTmpPiecesListFTPDataTable table = new InfoTrackData.ViwTmpPiecesListFTPDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillByStationID(table, StationID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("22 - " + x.Message); }
            return table;
        }

        //[WebMethod]
        //public erpcou.ViwTmpPiecesListFDSDataTable BringVTPiecesFromServer(byte[] EmpID, int StationID)
        //{
        //    NaqelHHDWebServices.Data.InfoTrackSEDataTableAdapters.messTableAdapter mAdapter = new messTableAdapter();
        //    mAdapter.Connection.ConnectionString = "Data Source=192.168.1.20;Initial Catalog=ERPCourierSE;User ID=sa;Password=123";

        //    tmpData ds = new tmpData();
        //    //ds.EnforceConstraints = false;
        //    Data.tmpDataTableAdapters.ViwTmpPiecesListFDSTableAdapter Adapter = new NaqelHHDWebServices.Data.tmpDataTableAdapters.ViwTmpPiecesListFDSTableAdapter();
        //    Adapter.Connection.ConnectionString = GlobalVar.GV.GetERPNaqelConnection();
        //    //Data.tmpData.ViwtmpPiecesListFDSDataTable table = new tmpData.ViwtmpPiecesListFDSDataTable();
        //    if (!GlobalVar.GV.IsSecure(EmpID)) return ds.ViwTmpPiecesListFDS;
        //    //Adapter.FillByStationID(ds.ViwTmpPiecesListFDS,StationID);
        //    try
        //    {
        //        Adapter.Fill(ds.ViwTmpPiecesListFDS);
        //    }
        //    catch (Exception xx) { mAdapter.Insert(xx.Message); }
        //    mAdapter.Insert(ds.ViwTmpPiecesListFDS.Rows.Count.ToString());
        //    return ds.ViwTmpPiecesListFDS;
        //}

        //[WebMethod]
        //public tmpData.ViwTmpPiecesListFDSDataTable BringViwTmpPiecesListFDSFromServer1(byte[] EmpID, int StationID, int RouteID)
        //{
        //    Data.tmpDataTableAdapters.ViwTmpPiecesListFDSTableAdapter Adapter = new NaqelHHDWebServices.Data.tmpDataTableAdapters.ViwTmpPiecesListFDSTableAdapter();
        //    Adapter.Connection.ConnectionString = GlobalVar.GV.GetERPNaqelConnection();
        //    Data.tmpData.ViwTmpPiecesListFDSDataTable table = new tmpData.ViwTmpPiecesListFDSDataTable();
        //    if (!GlobalVar.GV.IsSecure(EmpID)) return table;
        //    Adapter.FillByStationAndRoute(table, StationID, RouteID);
        //    return table;
        //}

        [WebMethod]
        public InfoTrackData.DeliveryStatusDataTable BringDeliveryStatusFromServer(byte[] EmpID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.DeliveryStatusTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.DeliveryStatusTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.DeliveryStatusDataTable table = new InfoTrackData.DeliveryStatusDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.FillByStatusID(table);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("23 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData.ViwMEWaybillDataTable BringWaybillFromServer(System.Collections.Generic.List<int> waybillList, byte[] EmpID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwMEWaybillTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.ViwMEWaybillTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.ViwMEWaybillDataTable table = new InfoTrackData.ViwMEWaybillDataTable();
            InfoTrackData.ViwMEWaybillDataTable table1 = new InfoTrackData.ViwMEWaybillDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;

                for (int i = 0; i < waybillList.Count; i++)
                    Adapter.FillByWayBillNo(table1, waybillList[i]);

                table.Merge(table1);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("24 - " + x.Message); }
            return table;
        }

        [WebMethod]
        public InfoTrackData BringEmpRoleFromServer(List<int> EmployID, byte[] EmpID)
        {
            InfoTrackData ds = new InfoTrackData();
            InfoTrackData dstmp = new InfoTrackData();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID))
                    return dstmp;
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwUserMETableAdapter userMEAdapter = new App_Data.InfoTrackDataTableAdapters.ViwUserMETableAdapter();
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.RoleMETableAdapter rolemeAdapter = new App_Data.InfoTrackDataTableAdapters.RoleMETableAdapter();
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.RoleMEDetailTableAdapter rolemedetailAdapter = new App_Data.InfoTrackDataTableAdapters.RoleMEDetailTableAdapter();

                userMEAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                rolemeAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                rolemedetailAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                for (int i = 0; i < EmployID.Count; i++)
                {
                    userMEAdapter.FillByEmployID(ds.ViwUserME, EmployID[i]);
                    if (ds.ViwUserME.Rows.Count > 0)
                    {
                        InfoTrackData.ViwUserMERow row = ds.ViwUserME.Rows[0] as InfoTrackData.ViwUserMERow;
                        dstmp.UserME.Rows.Add(new object[] { row.ID, row.EmployID, row.Password, row.RoleMEID, row.MachineUniqID, row.StatusID, row.StationID });
                    }
                }
                if (dstmp.UserME.Rows.Count > 0)
                {
                    for (int i = 0; i < dstmp.UserME.Rows.Count; i++)
                    {
                        rolemeAdapter.FillByID(ds.RoleME, Convert.ToInt32((dstmp.UserME.Rows[i] as InfoTrackData.UserMERow).RoleMEID));
                        if (ds.RoleME.Rows.Count > 0)
                        {
                            InfoTrackData.RoleMERow rowRoleME = ds.RoleME.Rows[0] as InfoTrackData.RoleMERow;
                            if (dstmp.RoleME.Rows.Count > 0)
                            {
                                if (dstmp.RoleME.Where(P => P.ID == rowRoleME.ID).Count() <= 0)
                                {
                                    dstmp.RoleME.Rows.Add(new object[] { rowRoleME.ID, rowRoleME.Name, rowRoleME.FName, rowRoleME.StatusID });
                                    rolemedetailAdapter.FillByRoleMEID(ds.RoleMEDetail, rowRoleME.ID);
                                    for (int j = 0; j < ds.RoleMEDetail.Rows.Count; j++)
                                        dstmp.RoleMEDetail.Rows.Add(new object[] { rowRoleME.ID, (ds.RoleMEDetail.Rows[j] as InfoTrackData.RoleMEDetailRow).RoleKey });
                                }
                            }
                            else
                            {
                                dstmp.RoleME.Rows.Add(new object[] { rowRoleME.ID, rowRoleME.Name, rowRoleME.FName, rowRoleME.StatusID });
                                rolemedetailAdapter.FillByRoleMEID(ds.RoleMEDetail, rowRoleME.ID);
                                for (int j = 0; j < ds.RoleMEDetail.Rows.Count; j++)
                                    dstmp.RoleMEDetail.Rows.Add(new object[] { rowRoleME.ID, (ds.RoleMEDetail.Rows[j] as InfoTrackData.RoleMEDetailRow).RoleKey });
                            }
                        }
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("25 - " + x.Message); }
            return dstmp;
        }

        [WebMethod]
        public InfoTrackData BringTripCodeFormServer(byte[] EmpID)
        {
            InfoTrackData ds = new InfoTrackData();
            if (!GlobalVar.GV.IsSecure(EmpID)) return ds;
            try
            {
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripCodeTableAdapter hAdapter = new App_Data.InfoTrackDataTableAdapters.TripCodeTableAdapter();
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripCodeDetailTableAdapter dAdapter = new App_Data.InfoTrackDataTableAdapters.TripCodeDetailTableAdapter();
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViwTripCodeDestinationTableAdapter viwAdapter = new App_Data.InfoTrackDataTableAdapters.ViwTripCodeDestinationTableAdapter();

                hAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                dAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                viwAdapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();


                ds.EnforceConstraints = false;
                try { hAdapter.FillNotDeleted(ds.TripCode); }
                catch { }

                try { dAdapter.FillNotDeleted(ds.TripCodeDetail); }
                catch { }

                try { viwAdapter.Fill(ds.ViwTripCodeDestination); }
                catch { }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("26" + x.Message); }
            return ds;
        }

        [WebMethod]
        public byte[] GetUserPassword(int EmployID, byte[] EmpID)
        {
            byte[] userpassword = new byte[40];
            try
            {
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.UserMETableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.UserMETableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                InfoTrackData ds = new InfoTrackData();
                adapter.FillByEmployID(ds.UserME, EmployID);
                if (ds.UserME.Rows.Count > 0)
                    userpassword = (ds.UserME.Rows[0] as InfoTrackData.UserMERow).Password.ToArray();
            }
            catch (Exception x) { GlobalVar.GV.InsertError("27" + x.Message); }
            return userpassword;
        }

        [WebMethod]
        public bool ChangeUserPassword(int EmployID, byte[] NewPassword, byte[] EmpID)
        {
            if (!GlobalVar.GV.IsSecure(EmpID)) return false;
            try
            {
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.UserMETableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.UserMETableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                InfoTrackData ds = new InfoTrackData();
                adapter.FillByEmployID(ds.UserME, EmployID);
                if (ds.UserME.Rows.Count > 0)
                    (ds.UserME.Rows[0] as InfoTrackData.UserMERow).Password = NewPassword;
                adapter.Update(ds.UserME);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("28" + x.Message); }
            return true;
        }

        //[WebMethod]
        //public int GetTripCodeID(byte[] EmpID, int TripPlanID)
        //{
        //    int tripcodeid = 0;

        //    TripPlanTableAdapter Adapter = new TripPlanTableAdapter();
        //    Adapter.Connection.ConnectionString = GlobalVar.GV.GetERPNaqelConnection();
        //    InfoTrackData.TripPlanDataTable table = new InfoTrackData.TripPlanDataTable();
        //    if (!GlobalVar.GV.IsSecure(EmpID)) return 0;
        //    Adapter.FillByID(table, TripPlanID);
        //    if (table.Rows.Count > 0)
        //        tripcodeid = Convert.ToInt32((table.Rows[0] as InfoTrackData.TripPlanRow).TripCodeID);

        //    return tripcodeid;
        //}

        [WebMethod]
        public int GetTripCodeDetailID(byte[] EmpID, int SystemTripPlanID, int StationID)
        {
            int tripcodedetailid = 0;
            if (!GlobalVar.GV.IsSecure(EmpID)) return tripcodedetailid;
            int EmployID = 0;
            InfoTrack.Common.Security sec = new InfoTrack.Common.Security();
            EmployID = Convert.ToInt32(sec.Decrypt(EmpID));

            try
            {
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter();
                Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                InfoTrackData ds = new InfoTrackData();
                Adapter.FillByTripPlanID(ds.TripPlan, @StationID, SystemTripPlanID);
                if (ds.TripPlan.Rows.Count > 0)
                    tripcodedetailid = Convert.ToInt32((ds.TripPlan.Rows[0] as InfoTrackData.TripPlanRow).TripCodeDetailID);
                if (tripcodedetailid <= 0)
                {
                    App_Data.InfoTrackDataTableAdapters.ExceptionEmployTableAdapter Ada =
                        new App_Data.InfoTrackDataTableAdapters.ExceptionEmployTableAdapter();
                    Ada.FillByEmployAndException(ds.ExceptionEmploy, EmployID, 12);
                    if (ds.ExceptionEmploy.Rows.Count > 0)
                        tripcodedetailid = SystemTripPlanID;
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("29" + x.Message); }
            return tripcodedetailid;
        }

        [WebMethod]
        public InfoTrackSEData.DBUpdateDataTable BringDBUpdateCommand(byte[] EmpID, int MaxID)
        {
            InfoTrackSEData.DBUpdateDataTable dbTable = new InfoTrackSEData.DBUpdateDataTable();
            if (!GlobalVar.GV.IsSecure(EmpID)) return dbTable;
            try
            {
                DBUpdateTableAdapter Adapter = new DBUpdateTableAdapter();
                Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                Adapter.FillGTID(dbTable, MaxID);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("29" + x.Message); }
            return dbTable;
        }

        [WebMethod]
        public List<string> GetConsByWaybillNo(byte[] EmpID, int WaybillNo)
        {
            List<string> ConsList = new List<string>();
            if (!GlobalVar.GV.IsSecure(EmpID)) return ConsList;

            App_Data.InfoTrackData ds = new InfoTrackData();
            App_Data.InfoTrackDataTableAdapters.ViwWaybillConsTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.ViwWaybillConsTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
            adapter.FillByWaybillNo(ds.ViwWaybillCons, WaybillNo);

            for (int i = 0; i < ds.ViwWaybillCons.Count; i++)
                ConsList.Add((ds.ViwWaybillCons[i] as App_Data.InfoTrackData.ViwWaybillConsRow).ConsNo);

            return ConsList;
        }

        #endregion

        #region Check Validation

        [WebMethod]
        public bool IsClientCorrect(int ClientID)
        {
            bool result = false;
            try
            {
                App_Data.InfoTrackData.ClientDataTable dt = new InfoTrackData.ClientDataTable();
                App_Data.InfoTrackDataTableAdapters.ClientTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.ClientTableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                adapter.FillByClientID(dt, ClientID);
                if (dt.Rows.Count > 0)
                    result = true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("30" + x.Message); }
            return result;
        }

        [WebMethod]
        public bool IsCourierCorrrect(int CourierID)
        {
            bool result = false;
            try
            {
                App_Data.InfoTrackData.ViwCourierWDataTable dt = new InfoTrackData.ViwCourierWDataTable();
                App_Data.InfoTrackDataTableAdapters.ViwCourierWTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.ViwCourierWTableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                adapter.FillByCourierID(dt, CourierID);
                if (dt.Rows.Count > 0)
                    result = true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("31" + x.Message); }
            return result;
        }

        [WebMethod]
        public bool IsWaybillExist(string WaybillNo)
        {
            bool result = false;
            try
            {
                App_Data.InfoTrackSEDataTableAdapters.PickUpTableAdapter adapter = new PickUpTableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                if (Convert.ToInt32(adapter.GetCountOfWaybillNo(WaybillNo)) > 0)
                    result = true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("32" + x.Message); }
            return result;
        }

        [WebMethod]
        public bool IsTripCorrrect(int TripID)
        {
            bool result = false;
            try
            {
                App_Data.InfoTrackData.TripPlanDataTable dt = new InfoTrackData.TripPlanDataTable();
                App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                adapter.FillByID(dt, TripID);
                if (dt.Rows.Count > 0)
                    result = true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("33" + x.Message); }
            return result;
        }

        [WebMethod]
        public bool IsTripCorrrects(int TripID, int EmployID)
        {
            bool result = false;
            try
            {
                App_Data.InfoTrackData.TripPlanDataTable dt = new InfoTrackData.TripPlanDataTable();
                App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.TripPlanTableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                adapter.FillByID(dt, TripID);
                if (dt.Rows.Count > 0)
                    result = true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("33" + x.Message); }
            return result;
        }

        //[WebMethod]
        //public bool IsTripCorrrect(int TripID,int EmployID)
        //{
        //    bool result = false;
        //    try
        //    {
        //        Data.InfoTrackData.TripPlanDataTable dt = new InfoTrackData.TripPlanDataTable();
        //        Data.InfoTrackDataTableAdapters.TripPlanTableAdapter adapter = new TripPlanTableAdapter();
        //        adapter.FillByID(dt, TripID);
        //        if (dt.Rows.Count > 0)
        //            result = true;
        //    }
        //    catch (Exception x) { GlobalVar.GV.InsertError("33" + x.Message); }
        //    return result;
        //}

        #endregion

        #region Differents

        [WebMethod]
        public bool PutImage(byte[] ImgIn, string ImageName)
        {
            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream(ImgIn);
                System.Drawing.Bitmap b = (System.Drawing.Bitmap)Image.FromStream(ms);
                try
                {
                    if (!System.IO.File.Exists(GlobalVar.GV.PicPath + ImageName))
                    {
                        try
                        {
                            b.Save(GlobalVar.GV.PicPath + ImageName, System.Drawing.Imaging.ImageFormat.Bmp);
                        }
                        catch (Exception j) { GlobalVar.GV.InsertError(j.Message.ToString()); }
                    }
                    else
                        PicName(ImgIn, ImageName, 0);
                }
                catch { PicName(ImgIn, ImageName, 0); }
            }
            catch { }
            return true;
        }

        private void PicName(byte[] ImgIn, string ImageName, int count)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(ImgIn);
            System.Drawing.Bitmap b = (System.Drawing.Bitmap)Image.FromStream(ms);
            try
            {
                string[] s = ImageName.Split(new char[] { '.' });
                string newname = "";
                newname = s[0] + "(" + count.ToString() + ")." + s[1];
                b.Save(GlobalVar.GV.PicPath + newname, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch { PicName(ImgIn, ImageName, count++); }
        }

        [WebMethod]
        public bool RestPassword(byte[] EmpID, int EmployID, byte[] NewPasswrod, int ITEmployID, string MachineID)
        {
            if (!GlobalVar.GV.IsSecure(EmpID)) return false;
            try
            {
                NaqelAPI.App_Data.InfoTrackDataTableAdapters.UserMETableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.UserMETableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

                adapter.UpdatePassword(NewPasswrod, EmployID);

                try
                {
                    App_Data.InfoTrackSEDataTableAdapters.RestPasswordTableAdapter resAdapter = new RestPasswordTableAdapter();
                    resAdapter.Insert(null, ITEmployID, EmployID, MachineID, false);
                }
                catch { }

                return true;
            }
            catch (Exception x) { GlobalVar.GV.InsertError("*Leave it 34 - " + x.Message); }
            return false;
        }

        [WebMethod]
        public bool HasPrivillageToRest(byte[] EmpID, int EmployID)
        {
            bool Result = false;
            if (!GlobalVar.GV.IsSecure(EmpID)) return false;

            try
            {
                App_Data.InfoTrackDataTableAdapters.UserMETableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.UserMETableAdapter();
                adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
                InfoTrackData.UserMEDataTable dt = new InfoTrackData.UserMEDataTable();

                adapter.FillByID(dt, EmployID);

                if (dt.Rows.Count > 0)
                {
                    if (!(dt.Rows[0] as InfoTrackData.UserMERow).IsHasPrivillageRestPasswordNull() &&
                        (dt.Rows[0] as InfoTrackData.UserMERow).HasPrivillageRestPassword == true)
                        return true;
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("*Leave it 34 - " + x.Message); }

            return Result;
        }

        [WebMethod]
        public void Send()
        {
            GlobalVar.GV.SendEmail();
        }

        #endregion

        #region Booking

        [WebMethod]
        public InfoTrack.NaqelAPI.App_Data.InfoTrackData.ViwBookingToHHDDataTable GetBookingData(List<int> OldBookingList, DateTime PickUpReqDT, int CourierID)
        {
            string OldBooking = "";
            for (int i = 0; i < OldBookingList.Count; i++)
                OldBooking += OldBookingList[i] + ",";
            if (OldBooking != "")
                OldBooking = OldBooking.Remove(OldBooking.Length - 1);

            App_Data.InfoTrackData.ViwBookingToHHDDataTable dtBooking = new App_Data.InfoTrackData.ViwBookingToHHDDataTable();
            SqlConnection conn = new SqlConnection(GlobalVar.GV.GetInfoTrackConnection());
            conn.Open();
            string query = "SELECT * FROM [ViwBookingToHHD] where CourierID = " + CourierID + " and " + GetDateCondition(PickUpReqDT, PickUpReqDT, "PickUpReqDT");
            if (OldBooking != "")
                query += " and ID Not in (" + OldBooking + ")";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            
            adapter.Fill(dtBooking);
            return dtBooking;
        }

        private string GetDateCondition(DateTime fromdate, DateTime todate, string field)
        {
            string condition = "";

            DateTime fDate = new DateTime(fromdate.Year, fromdate.Month, fromdate.Day, 0, 0, 0);
            DateTime tDate = new DateTime(todate.Year, todate.Month, todate.Day, 23, 59, 59);
            condition = string.Format("([" + field + "] BETWEEN '{0}' and '{1}')", fDate.ToString("yyyy-MM-dd HH:mm:ss"), tDate.ToString("yyyy-MM-dd HH:mm:ss"));

            return condition;
        }

        [WebMethod]
        public bool AcknowledgeBooking(int BookingID, int EmployeeID)
        {
            bool Result = false;
            App_Data.InfoTrackData dt = new InfoTrackData();
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.BookingTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.BookingTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            Adapter.FillByID(dt.Booking, BookingID);

            if (dt.Booking.Rows.Count > 0)
            {
                InfoTrackData.BookingRow row = dt.Booking.Rows[0] as InfoTrackData.BookingRow;
                if (row.CurrentStatusID == 1 || row.CurrentStatusID == 2)
                {
                    row.CurrentStatusID = 7;
                    row.AcknowledgeBy = EmployeeID;
                    Adapter.Update(dt.Booking);
                    Result = true;
                }
            }

            return Result;
        }

        #endregion

        //#region   "File Upload"
        //[WebMethod()]
        //public bool UploadFile(byte[] f, string fileName)
        //{
        //    bool Uploaded = false;
        //    try
        //    {
        //        string FilePath =
        //          Path.Combine(Server.MapPath(ConfigurationManager.AppSettings["upload_path"].ToString()), fileName);
        //        MemoryStream ms = new MemoryStream(f);
        //        FileStream fs = new FileStream(FilePath, FileMode.Create);
        //        ms.WriteTo(fs);
        //        ms.Close();
        //        fs.Close();
        //        fs.Dispose();
        //        Uploaded = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Uploaded = false;
        //    }
        //    return Uploaded;
        //}

        //[WebMethod()]
        //public byte[] DownloadFile(string fileName)
        //{
        //    System.IO.FileStream fs1 = null;
        //    string FilePath =
        //    Path.Combine(Server.MapPath(ConfigurationManager.AppSettings["upload_path"].ToString()), fileName);

        //    fs1 = System.IO.File.Open(FilePath, FileMode.Open, FileAccess.Read);
        //    byte[] b1 = new byte[fs1.Length];
        //    fs1.Read(b1, 0, (int)fs1.Length);
        //    fs1.Close();
        //    return b1;
        //}
        //#endregion

        #region Tracking Trip

        [WebMethod]
        public bool SendTripDataToServer(InfoTrackData ds)
        {
            bool result = false;
            try
            {
                //if (!GlobalVar.GV.IsSecure(EmpID)) return result;
                if (CheckConnectionToServerDB())
                {
                    InfoTrackData dsdata = new InfoTrackData();
                    NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripStatusMasterTableAdapter adapterH = new App_Data.InfoTrackDataTableAdapters.TripStatusMasterTableAdapter();
                    NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripStatusDetailsTableAdapter adapterD = new App_Data.InfoTrackDataTableAdapters.TripStatusDetailsTableAdapter();

                    adapterH.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();
                    adapterD.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackSEConnection();

                    System.Data.SqlClient.SqlConnection c = new System.Data.SqlClient.SqlConnection(GlobalVar.GV.GetInfoTrackConnection());
                    c.Open();
                    System.Transactions.CommittableTransaction T = new System.Transactions.CommittableTransaction();
                    c.EnlistTransaction(T);
                    adapterH.Connection = c;
                    adapterD.Connection = c;

                    adapterH.Fill(dsdata.TripStatusMaster);
                    adapterD.Fill(dsdata.TripStatusDetails);

                    int? Count =Convert.ToInt32( adapterH.SelectByTripNo(Convert.ToInt32(ds.TripStatusMaster.Rows[0]["TripNo"])));
                    if (Count == 0)
                        for (int i = 0; i < ds.TripStatusMaster.Rows.Count; i++)
                            (ds.TripStatusMaster.Rows[i] as InfoTrackData.TripStatusMasterRow).SetAdded();

                    for (int i = 0; i < ds.TripStatusDetails.Rows.Count; i++)
                        (ds.TripStatusDetails.Rows[i] as InfoTrackData.TripStatusDetailsRow).SetAdded();
                    try
                    {
                        adapterH.Update(ds.TripStatusMaster);
                        adapterD.Update(ds.TripStatusDetails);
                        T.Commit();
                        c.Close();
                        result = true;
                    }
                    catch (Exception xx)
                    {
                        T.Rollback();
                        c.Close();
                        result = false;
                        GlobalVar.GV.InsertError("53 - " + xx.Message);
                    }
                }
            }
            catch (Exception x) { GlobalVar.GV.InsertError("10 -TripStatus " + x.Message); }
            return result;
        }

        [WebMethod]
        public InfoTrackData.ViewTripStatusDataTable GetTripDataFromServer(int EmployID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.ViewTripStatusTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.ViewTripStatusTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.ViewTripStatusDataTable table = new InfoTrackData.ViewTripStatusDataTable();
            try
            {

                Adapter.FillByCourierID(table, EmployID);
                table.Merge(table);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("24 - " + x.Message); }
            return table;

        }

        [WebMethod]
        public InfoTrackData.TripStatusDataTable BringTripStatusFormServer(byte[] EmpID)
        {
            NaqelAPI.App_Data.InfoTrackDataTableAdapters.TripStatusTableAdapter Adapter = new App_Data.InfoTrackDataTableAdapters.TripStatusTableAdapter();
            Adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            InfoTrackData.TripStatusDataTable table = new InfoTrackData.TripStatusDataTable();
            try
            {
                if (!GlobalVar.GV.IsSecure(EmpID)) return table;
                Adapter.Fill(table);
            }
            catch (Exception x) { GlobalVar.GV.InsertError("23 - " + x.Message); }
            return table;
        }

        #endregion

        #region Update System

        private TransferFile transferFile = new TransferFile();

        [WebMethod(Description = "Web service provides mothed,return the array of byte")]
        public byte[] DownloadFile(string fileName)
        {
            string LastVersionPath = Server.MapPath(".");
            LastVersionPath = LastVersionPath.Remove(LastVersionPath.IndexOf("HHD"));
            LastVersionPath += @"HHD\HHDApplication\";
            return transferFile.ReadBinaryFile(LastVersionPath, fileName);
        }

        [WebMethod(Description = "Web service provides mothed,return the array of byte")]
        public string DownloadFiles(string fileName)
        {
            string result = "";
            result = Server.MapPath(".") + "\\" + fileName; ;
            return File.Exists(result).ToString();
            //return result;
        }

        [WebMethod(Description = "Web service provides mothed，if upload file successfully。")]
        public string UploadFile(byte[] fs, string fileName, bool CreateDirectory, string OldFileName)
        {
            string NewFileName = "";
            DateTime CTime = GetServerTime();
            NewFileName = CTime.Year.ToString() + CTime.Month.ToString() + CTime.Day.ToString() + CTime.Hour.ToString() + CTime.Minute.ToString() + CTime.Second.ToString();

            string LastVersionPath = Server.MapPath(".");
            LastVersionPath = LastVersionPath.Remove(LastVersionPath.IndexOf("HHD"));
            LastVersionPath += @"HHD\HHDApplication\";
            string x = "";

            try
            {
                if (CreateDirectory)
                {
                    Directory.CreateDirectory(LastVersionPath + NewFileName);
                    x = transferFile.WriteBinarFile(fs, LastVersionPath + NewFileName + "\\", fileName);
                }
                else
                    x = transferFile.WriteBinarFile(fs, LastVersionPath + OldFileName + "\\", fileName);
            }
            catch (Exception ex) { GlobalVar.GV.InsertError(ex.Message); }

            return NewFileName;
        }

        class TransferFile
        {
            public TransferFile() { }

            public string WriteBinarFile(byte[] fs, string path, string fileName)
            {
                try
                {
                    MemoryStream memoryStream = new MemoryStream(fs);
                    FileStream fileStream = new FileStream(path + fileName, FileMode.Create);
                    memoryStream.WriteTo(fileStream);
                    memoryStream.Close();
                    fileStream.Close();
                    fileStream = null;
                    memoryStream = null;
                    return "File has already uploaded successfully。";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            /// <summary>
            /// getBinaryFile：Return array of byte which you specified。
            /// </summary>
            /// <param name="filename"></param>
            /// <returns></returns>
            public byte[] ReadBinaryFile(string path, string fileName)
            {
                if (File.Exists(path + fileName))
                {
                    try
                    {
                        ///Open and read a file。
                        FileStream fileStream = File.OpenRead(path + fileName);
                        return ConvertStreamToByteBuffer(fileStream);
                    }
                    catch 
                    {
                        return new byte[0];
                    }
                }
                else
                {
                    return new byte[0];
                }
            }

            /// <summary>
            /// ConvertStreamToByteBuffer：Convert Stream To ByteBuffer。
            /// </summary>
            /// <param name="theStream"></param>
            /// <returns></returns>
            public byte[] ConvertStreamToByteBuffer(System.IO.Stream theStream)
            {
                int b1;
                System.IO.MemoryStream tempStream = new System.IO.MemoryStream();
                while ((b1 = theStream.ReadByte()) != -1)
                {
                    tempStream.WriteByte(((byte)b1));
                }
                return tempStream.ToArray();
            }
        }

        #endregion

        [WebMethod]
        public InfoTrackData GetNewMessage(byte[] EmpID, int EmployID, int StationID)
        {
            InfoTrackData dt = new InfoTrackData();
            App_Data.InfoTrackDataTableAdapters.HHDMessageTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.HHDMessageTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            try
            {
                adapter.FillNewMessages(dt.HHDMessage, StationID, EmployID);
            }
            catch (Exception ee) { GlobalVar.GV.InsertError(ee.Message); }
            //            if (!GlobalVar.GV.IsSecure(EmpID)) return dt;
            //            string Commandtext = @"SELECT *
            //                        FROM         dbo.HHDMessage
            //                        WHERE     (NOT (ID IN
            //                          (SELECT     HHDMessageID
            //                             FROM         dbo.HHDMessageReceived AS HHDMessageReceived_1
            //                             WHERE     (StatusID <> 3) AND (EmployID = " + EmployID + @")))) AND (StatusID <> 3) AND (ForAll = 1) OR
            //                      (NOT (ID IN
            //                          (SELECT     HHDMessageID
            //                             FROM         dbo.HHDMessageReceived AS HHDMessageReceived_1
            //                             WHERE     (StatusID <> 3) AND (EmployID = " + EmployID + @")))) AND (StatusID <> 3) AND (ForSpecificStation = 1) AND (StationID = " + StationID + @") OR
            //                      (NOT (ID IN
            //                          (SELECT     HHDMessageID
            //                             FROM         dbo.HHDMessageReceived AS HHDMessageReceived_1
            //                             WHERE     (StatusID <> 3) AND (EmployID = " + EmployID + @")))) AND (StatusID <> 3) AND (ForSpecificEmploy = 1) AND (EmployID = " + EmployID + ")";

            //            SqlDataAdapter adapter = new SqlDataAdapter(Commandtext, GlobalVar.GV.GetERPNaqelConnection());
            //            adapter.Fill(dt.HHDMessage);
            GlobalVar.GV.InsertError("Messages Count " + dt.HHDMessage.Rows.Count.ToString());
            return dt;
        }

        [WebMethod]
        public void gotMessage(byte[] EmpID, int EmployID, int MessageID)
        {
            if (!GlobalVar.GV.IsSecure(EmpID)) return;
            App_Data.InfoTrackDataTableAdapters.HHDMessageReceivedTableAdapter adapter = new App_Data.InfoTrackDataTableAdapters.HHDMessageReceivedTableAdapter();
            adapter.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();
            adapter.Insert(MessageID, EmployID, 1);
        }

        [WebMethod]
        public InfoTrack.NaqelAPI.ShipmentDetails GetShipmentDetails(byte[] EmpID, int WaybillNo)
        {
            InfoTrack.NaqelAPI.ShipmentDetails Result = new ShipmentDetails();

            App_Data.DataDataContext dcData = new DataDataContext();
            dcData.Connection.ConnectionString = GlobalVar.GV.GetInfoTrackConnection();

            if (dcData.ViwWaybills.Where(P => P.WayBillNo == WaybillNo && P.IsCancelled == false).Count() > 0)
            {
                App_Data.ViwWaybill instance = dcData.ViwWaybills.First(P => P.WayBillNo == WaybillNo && P.IsCancelled == false);
                Result.ClientID = instance.ClientID;
                Result.OrgID = instance.OriginStationID;
                Result.DestID = instance.DestinationStationID;
                Result.Weight = instance.Weight;
                Result.PiecesCount = instance.PicesCount;
            }
            else
                if (dcData.CustomerWayBills.Where(P => P.WayBillNo == WaybillNo && P.StatusID == 1).Count() > 0)
                {
                    App_Data.CustomerWayBill instance = dcData.CustomerWayBills.First(P => P.WayBillNo == WaybillNo && P.StatusID == 1);
                    Result.ClientID = instance.ClientID;
                    Result.OrgID = instance.OriginStationID;
                    Result.DestID = instance.DestinationStationID;
                    Result.Weight = instance.Weight;
                    Result.PiecesCount = instance.PicesCount;
                }

            return Result;
        }

    }
}