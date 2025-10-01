using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using InfoTrack.Common;
using InfoTrack.BusinessLayer.DContext;

namespace InfoTrack.NaqelAPI
{
    class AutomaticManifest
    {
        DocumentDataDataContext dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
        MastersDataContext dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
        HHDDataContext dcHHD = new HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());
        public GlobalVarCommon GF;

        public AutomaticManifest()
        {
            GF = new GlobalVarCommon(GlobalVar.GV.GetInfoTrackConnection(), "1.0.3.8", CultInfo);
            GF.shipment = new Common.Shipment(GlobalVar.GV.GetInfoTrackConnection());
        }

        public enum BillingType : int
        {
            Account = 1,
            Cash = 2,
            ExternalBilling = 3,
            Free = 4,
            COD = 5,
            FOD = 6
        }

        public enum ChargeField : int
        {
            PODCharge = 1,
            CODCharge = 2,
            LabourCharge = 3,
            SpecialCharge = 4,
            OtherCharge = 5,
            StorageCharge = 6
        }

        public enum ServiceTypes : int
        {
            Cargo = 1,
            LineHaul = 2,
            Document = 3,
            Express = 4,
            International = 6,
            InternationalCourier = 7,
            DomesticCourier = 8
        }

        public enum LoadTypes : int
        {
            Express = 1,
            HW = 2,
            LTL = 3,
            Pallet = 7,
            HalfLoad = 9,
            FullLoad = 10,
            Drums = 29,
            ExpressIntl = 30,
            CourierIntl = 32
        }

        private CultureInfo cultureinfo;
        public CultureInfo CultInfo
        {
            get { return cultureinfo; }
            set { cultureinfo = value; }
        }

        public void ManfiestWaybills(int? WaybillNo, int? PickUpBy, int? InvoiceAccount, int? InvoiceAccountSl)
        {
            CultInfo = new CultureInfo("en-US", true);
            GF = new GlobalVarCommon(GlobalVar.GV.GetInfoTrackConnection(), GlobalVar.GV.XMLGeneralVersion, CultInfo);
            GF.shipment = new Common.Shipment(GlobalVar.GV.GetInfoTrackConnection());
            List<ViwCustomerWaybillNotManifested> CustomerWaybillList = new List<ViwCustomerWaybillNotManifested>();
            dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcHHD = new HHDDataContext(GlobalVar.GV.GetInfoTrackConnection());

            dcDocument.CommandTimeout = 1000;
            dcMaster.CommandTimeout = 1000;
            if (WaybillNo.HasValue)
                CustomerWaybillList = dcDocument.ViwCustomerWaybillNotManifesteds.Where(P => P.WayBillNo == WaybillNo.Value).ToList();
            else
                CustomerWaybillList = dcDocument.ViwCustomerWaybillNotManifesteds.ToList();
            //Progress.Visible = true;
            //Progress.Properties.Maximum = CustomerWaybillList.Count();
            //Progress.EditValue = 0;
            for (int i = 0; i < CustomerWaybillList.Count; i++)
            {
                //Progress.Text = (Convert.ToInt32(Progress.Text) + 1).ToString();
                //Progress.Refresh();
                dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                Waybill instance = new Waybill();

                instance.WayBillNo = CustomerWaybillList[i].WayBillNo;
                instance.ClientID = CustomerWaybillList[i].ClientID;
                instance.ClientAddressID = CustomerWaybillList[i].ClientAddressID;
                instance.ClientContactID = CustomerWaybillList[i].ClientContactID;
                instance.ServiceTypeID = CustomerWaybillList[i].ServiceTypeID;
                instance.LoadTypeID = CustomerWaybillList[i].LoadTypeID;
                instance.BillingTypeID = CustomerWaybillList[i].BillingTypeID;
                instance.ConsigneeID = CustomerWaybillList[i].ConsigneeID;
                instance.ConsigneeAddressID = CustomerWaybillList[i].ConsigneeAddressID;
                instance.OriginStationID = CustomerWaybillList[i].OriginStationID;
                instance.DestinationStationID = CustomerWaybillList[i].DestinationStationID;
                instance.PickUpDate = CustomerWaybillList[i].PickUpDate;
                instance.PicesCount = CustomerWaybillList[i].PicesCount;

                if (CustomerWaybillList[i].Weight <= 0)
                {
                    //gridView1.SetRowCellValue(i, gridView1.Columns[7], "Weight is Wrong");

                    //gridView1.RefreshData();
                    continue;
                }
                instance.Weight = CustomerWaybillList[i].Weight;
                instance.Width = CustomerWaybillList[i].Width;
                instance.Length = CustomerWaybillList[i].Length;
                instance.Height = CustomerWaybillList[i].Height;
                instance.VolumeWeight = CustomerWaybillList[i].VolumeWeight;

                //int? TripCodeID = null;
                instance.IsInsurance = CustomerWaybillList[i].IsInsurance;

                //To Doo
                if (InvoiceAccount.HasValue)
                {
                    instance.ServiceCharge = 0;
                }
                else
                    instance.ServiceCharge = CustomerWaybillList[i].ServiceCharge != null ? Convert.ToDouble(CustomerWaybillList[i].ServiceCharge) : 0;

                instance.HandlingCharge = 0;
                instance.PackingCharge = 0;
                instance.StorageCharge = 0;
                instance.DeclaredValue = 0;
                instance.InsuredValue = 0;
                instance.InsuranceCharge = 0;
                instance.SpecialCharge = 0;
                instance.OtherCharge = 0;
                instance.TotalCharge = 0;
                instance.Discount = 0;
                instance.DiscountType = "";
                instance.NetCharge = 0;
                instance.PaidAmount = 0;

                if (InvoiceAccount.HasValue)
                {
                    instance.InvoiceAccount = InvoiceAccount.Value;
                    if (InvoiceAccountSl.HasValue)
                        instance.InvoiceAccountSl = InvoiceAccountSl.Value;
                }
                else
                    instance.InvoiceAccount = CustomerWaybillList[i].ClientID;

                if (!InvoiceAccountSl.HasValue)
                {
                    if (instance.BillingTypeID == Convert.ToInt32(BillingType.Account) || instance.BillingTypeID == Convert.ToInt32(BillingType.ExternalBilling) || instance.BillingTypeID == Convert.ToInt32(BillingType.COD))
                    {
                        if (dcMaster.ClientAddresses.Where(P => P.ClientID == instance.ClientID && P.ForInvoice == true).Count() > 0)
                        {
                          InfoTrack.BusinessLayer.DContext.ClientAddress ClientAddressInstance = dcMaster.ClientAddresses.First(P => P.ClientID == instance.ClientID && P.ForInvoice == true);
                            instance.InvoiceAccountSl = ClientAddressInstance.ID;
                        }
                    }
                }

                instance.OnAccount = 0;
                instance.PlateNo = "";
                instance.Remarks = "";
                instance.Contents = CustomerWaybillList[i].Contents;
                instance.RouteID = 0;
                instance.IsEmergency = false;

                if (PickUpBy.HasValue)
                {
                    instance.PickUpBy = PickUpBy.Value;
                    instance.PickUpDate = DateTime.Now;
                }
                else
                {
                    if (dcHHD.PickUps.Where(P => P.WaybillNo == instance.WayBillNo).Count() > 0)
                    {
                        PickUp PickUpInstance = dcHHD.PickUps.First(P => P.WaybillNo == instance.WayBillNo);
                        UserME UserMeInstance = dcHHD.UserMEs.First(P => P.ID == PickUpInstance.UserID);

                        instance.PickUpBy = UserMeInstance.EmployID;
                        instance.PickUpDate = PickUpInstance.TimeIn;
                    }
                    else
                    {
                        //gridView1.SetRowCellValue(i, "gResult", "Unknow Couerier ID for PickUp.");
                        continue;
                    }
                }

                instance.PODNeeded = false;
                instance.PODDetail = "";
                instance.DeliveryInstruction = CustomerWaybillList[i].DeliveryInstruction;
                instance.IsDelivered = false;
                //int? CancelledBy = null;
                //DateTime? CancelledDate = null;
                instance.IsCancelled = false;
                instance.CancelledReason = "";
                instance.WaybillContact = "";
                //int? TruckType = null;
                //int? DropPoints = null;
                instance.DropCharge = 0;
                instance.Invoiced = false;
                instance.PODNeeded2 = false;
                instance.ConsigneeAttention = "";
                instance.BookingRefNo = CustomerWaybillList[i].BookingRefNo != null ? CustomerWaybillList[i].BookingRefNo : "";
                instance.PODTypeID = CustomerWaybillList[i].PODTypeID != null ? CustomerWaybillList[i].PODTypeID : null;
                instance.ODAStationCharge = 0;
                //int? ODAStationID = null;
                //double? StandardShipment = null;
                //int? StandardTariffID = null;
                //int? AgreementID = null;
                //nt? InsPercentage = null;
                //int? DRouteID = null;
                instance.IsServiceManual = false;
                instance.CreatedBy = -1;
                instance.IsSync = false;
                //int? ReceiptNo = null;
                //int? detail_serial = null;
                //int? invc_account_sl = null;
                //int? consignee_serial = null;
                instance.IsServiceChargeUpdate = false;
                //int? DeliveryBy = null;
                //instance.ManifestedTime = GlobalVar.GV.CurrentDate;
                instance.ManifestedTime = DateTime.Now;
                //int? SalesManID = null;
                instance.RefNo = CustomerWaybillList[i].RefNo != null && CustomerWaybillList[i].RefNo != "" ? CustomerWaybillList[i].RefNo : "";
                instance.LastEvent = "";
                instance.TotalGCCCharges = 0;
                //DateTime? LastEventTime = null;
                //int? DeliveryID = null;
                //int? ODADStationID = null;
                instance.ODADStationCharge = CustomerWaybillList[i].ODAStationCharge != null ? CustomerWaybillList[i].ODAStationCharge : 0;
                //int? BranchID = null;
                //int? TransitTimeDays = null;
                instance.ProductTypeID = CustomerWaybillList[i].ProductTypeID;
                if (CustomerWaybillList[i].IsRTO.HasValue)
                    instance.IsRTO = CustomerWaybillList[i].IsRTO.Value;
                else
                    instance.IsRTO = false;
                //int? OldWaybillNo = null;
                instance.IsMultiDimension = false;
                instance.CollectedAmount = CustomerWaybillList[i].CODCharge;

                instance.AmountForClient = 0;
                instance.IsSyncAx = false;
                instance.CustomDuty = 0;
                ///int? BandDiscountID = null;
                //int? DestClientBranchID = null;
                //int? OrgClientBranchID = null;

                CalculateCharges(instance);
                CheckAutomaticCharges(instance);
                CalculateCharges(instance);

                if (instance.CollectedAmount <= 0)
                    instance.BillingTypeID = Convert.ToInt32(BillingType.Account);

                if (dcDocument.Waybills.Where(P => P.WayBillNo == instance.WayBillNo && P.IsCancelled == false).Count() <= 0)
                {
                    dcDocument.Waybills.InsertOnSubmit(instance);
                    dcDocument.SubmitChanges();
                }

                //gridView1.SetRowCellValue(i, "gResult", "Manifested Successfully.");
                //gridView1.RefreshData();
            }

            //Progress.Visible = false;
            //MessageBox.Show("Finish", GlobalVar.GV.GetLocalizationMessage("SystemName"));
        }

        public void CheckAutomaticCharges(Waybill instance)
        {
            dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            List< AutomaticCharge> listAuto = dcMaster.AutomaticCharges.Where(P => P.ClientID == instance.InvoiceAccount && P.StatusID == 1).ToList();
            for (int i = 0; i < listAuto.Count(); i++)
            {
                if ((!listAuto[i].BillingTypeID.HasValue || listAuto[i].BillingTypeID.Value == instance.BillingTypeID) &&
                    (!listAuto[i].ProductTypeID.HasValue || listAuto[i].ProductTypeID.Value == instance.ProductTypeID) &&
                    (!listAuto[i].ServiceTypeID.HasValue || listAuto[i].ServiceTypeID.Value == instance.ServiceTypeID) &&
                    (!listAuto[i].LoadTypeID.HasValue || listAuto[i].LoadTypeID.Value == instance.LoadTypeID) &&
                    (!listAuto[i].OriginStationID.HasValue || listAuto[i].OriginStationID.Value == instance.OriginStationID) &&
                    (!listAuto[i].DestinationStationID.HasValue || listAuto[i].DestinationStationID.Value == instance.DestinationStationID) &&
                    (!listAuto[i].PODTypeID.HasValue || listAuto[i].PODTypeID.Value == instance.PODTypeID))
                {
                    if (instance.Weight >= listAuto[i].FromWeight && instance.Weight <= listAuto[i].ToWeight)
                    {
                        if (listAuto[i].ChargeFieldID == Convert.ToInt32(ChargeField.PODCharge))
                        {
                            if (instance.HandlingCharge <= 0)
                                instance.HandlingCharge = listAuto[i].Amount;
                            continue;
                        }

                        if (listAuto[i].ChargeFieldID == Convert.ToInt32(ChargeField.CODCharge))
                        {
                            if (instance.HandlingCharge <= 0)
                                instance.HandlingCharge = listAuto[i].Amount;
                            continue;
                        }

                        if (listAuto[i].ChargeFieldID == Convert.ToInt32(ChargeField.LabourCharge))
                        {
                            if (instance.PackingCharge <= 0)
                                instance.PackingCharge = listAuto[i].Amount;
                            continue;
                        }

                        if (listAuto[i].ChargeFieldID == Convert.ToInt32(ChargeField.SpecialCharge))
                        {
                            if (instance.SpecialCharge <= 0)
                                instance.SpecialCharge = listAuto[i].Amount;
                            continue;
                        }

                        if (listAuto[i].ChargeFieldID == Convert.ToInt32(ChargeField.OtherCharge))
                        {
                            if (instance.OtherCharge <= 0)
                                instance.OtherCharge = listAuto[i].Amount;
                            continue;
                        }
                    }
                }
            }
        }

        public void CalculateCharges(Waybill instance)
        {
            CheckInsurance(instance);
            //CheckBandDiscount(instance);

            GetShipment(instance);
            CalculateTotal(instance);

            instance.NetCharge = Convert.ToDouble(instance.TotalCharge) - (Convert.ToDouble(instance.Discount));
            //if (GlobalVar.GV.IsRetailOutlet)
            //{
            //    if (txtNetCharge.EditValue != null && txtNetCharge.EditValue.ToString() != "" && Convert.ToDouble(txtNetCharge.EditValue) > 0)
            //    {
            //        txtNetCharge.EditValue = Math.Round(Convert.ToDouble(txtNetCharge.EditValue), 0);
            //        instance.NetCharge = Convert.ToDouble(txtNetCharge.EditValue);
            //    }
            //}

            instance.OnAccount = Convert.ToDouble(instance.NetCharge) - Convert.ToDouble(instance.PaidAmount);

            //double maxfree = Convert.ToDouble(instance.TotalCharge) * (GlobalVar.GV.MaxDiscount / 100);
            //if (Convert.ToDouble(instance.Discount) > maxfree)
            //    instance.Discount = 0;

            if (Convert.ToInt32(instance.ProductTypeID) == 6)
            {
                if (instance.CollectedAmount > 0)
                    instance.AmountForClient = Convert.ToDouble(instance.CollectedAmount) - Convert.ToDouble(instance.NetCharge);
            }
        }

        public void CheckInsurance(Waybill instance)
        {
            //double val = 0;
            //if (comPercentage.EditValue != null && comPercentage.EditValue.ToString() != "")
            //{
            //    if (Convert.ToInt32(comPercentage.EditValue) == 1)
            //        val = 0.0035;
            //    else
            //        if (Convert.ToInt32(comPercentage.EditValue) == 2)
            //            val = 0.0030;
            //        else
            //            if (Convert.ToInt32(comPercentage.EditValue) == 3)
            //                val = 0.0025;
            //            else
            //                if (Convert.ToInt32(comPercentage.EditValue) == 4)
            //                    val = 0.001;
            //                else
            //                    if (Convert.ToInt32(comPercentage.EditValue) == 5)
            //                        val = 0.0018;
            //}

            //if (txtInsuranceValue.EditValue != null && Convert.ToDouble(txtInsuranceValue.EditValue) > 0 && Convert.ToDouble(txtInsuranceValue.EditValue) > 5000)
            //    txtInsuranceCost.EditValue = Math.Round(Convert.ToDouble(txtInsuranceValue.EditValue) * val);
            //else
            //    txtInsuranceCost.EditValue = 0;
        }

        //private void CheckBandDiscount( Waybill instance)
        //{
        //    if (comBands.EditValue != null && comBands.EditValue.ToString() != "" && Convert.ToInt32(comBands.EditValue) > 0)
        //    {
        //        instance.BandDiscountID = Convert.ToInt32(comBands.EditValue);
        //        double discountPercentage = 0;
        //        discountPercentage = (masterData.Bands.Rows[Convert.ToInt32(comBands.EditValue) - 1] as  MasterData.BandsRow).DiscountPercentage;

        //        if (instance.StandardShipment != null && Convert.ToInt32(instance.StandardShipment) > 0)
        //        {
        //            TxtDiscount.EditValue = Convert.ToDouble(instance.StandardShipment) * (discountPercentage / 100);
        //            if (GlobalVar.GV.IsRetailOutlet)
        //            {
        //                if (TxtDiscount.EditValue != null && TxtDiscount.EditValue.ToString() != "" && Convert.ToDouble(TxtDiscount.EditValue) > 0)
        //                {
        //                    TxtDiscount.EditValue = Math.Round(Convert.ToDouble(TxtDiscount.EditValue), 0);
        //                    instance.Discount = Convert.ToDouble(TxtDiscount.EditValue);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        instance.SetBandDiscountIDNull();
        //        TxtDiscount.EditValue = 0;
        //    }
        //}

        void GetShipment( Waybill instance)
        {
            dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            double StandardShipment = 0;
            double NewWeights = Convert.ToDouble(instance.Weight);
            int clientid = 0;

            #region Domestic updated Cash Rates 2013

            //Documents 
            //    1st 0.5 kg 85 SAR
            //    Additional 0.5 kg 15 SAR

            //Non documents & Express shipments
            //    1st  20 kg 100 SAR
            //    Additional kg for main Hub 1.75 SAR
            //    Additional kg for sub-stations  2.25 SAR
            //    ODA charges as per attach 

            //Kindly note discounting approval as follow:
            //    Max offered discount is 5% for CS supervisor
            //    Any other discounts request must be approved by:
            //    Customer service Manager for pick up customers through call center calls
            //    Retail & Mass Mail Manager for walk in & retail customers.
            bool IsCorporate = false;
            if (instance.InvoiceAccount != null && instance.InvoiceAccount.ToString() != "" && Convert.ToInt32(instance.InvoiceAccount) > 0)
                clientid = Convert.ToInt32(instance.InvoiceAccount);
            if (clientid > 0)
            {
                if (dcMaster.Clients.First(P => P.ID == clientid).IsCorporate)
                    IsCorporate = true;
            }

            if (Convert.ToDouble(instance.Weight) > 0 &&
                instance.ServiceTypeID.Equals(Convert.ToInt32(ServiceTypes.Express)) &&
                instance.LoadTypeID.Equals(Convert.ToInt32(LoadTypes.Express)) &&
                instance.BillingTypeID.Equals(Convert.ToInt32(BillingType.Cash)))
            {
                double AdditionalWeight = 0, TotalCharge = 0, AdditionalCharge = 0;
                if (dcDocument == null)
                    dcDocument = new DocumentDataDataContext();
                int CntAgreement = dcMaster.Agreements.Where(P => P.ClientID == clientid
                          && P.LoadTypeID == Convert.ToInt32(instance.LoadTypeID)
                          && Convert.ToDateTime(instance.PickUpDate) >= P.FromDate
                          && Convert.ToDateTime(instance.PickUpDate) <= P.ToDate
                          && P.IsTerminated == false
                          && P.IsCancelled == false
                          && P.StatusID == 1).Count();

                if (CntAgreement == 0)
                {
                    if (IsBothHub(Convert.ToInt32(instance.OriginStationID), Convert.ToInt32(instance.DestinationStationID)))
                        AdditionalCharge = 1.75;
                    else
                        AdditionalCharge = 2.25;

                    if (Convert.ToDouble(instance.Weight) > 0 && Convert.ToDouble(instance.Weight) <= 20)
                        TotalCharge = 100;
                    else
                    {
                        AdditionalWeight = Math.Ceiling((NewWeights - 20)); ;
                        TotalCharge = 100 + (AdditionalWeight * AdditionalCharge);
                    }
                    instance.StandardShipment = TotalCharge;
                    return;
                }
                else
                    GetShipmentCharge(NewWeights, clientid, StandardShipment, instance);
            }
            else
                if (Convert.ToDouble(instance.Weight) > 0 &&
                instance.ServiceTypeID.Equals(Convert.ToInt32(ServiceTypes.Document)) &&
                instance.BillingTypeID.Equals(Convert.ToInt32(BillingType.Cash)))
                {
                    if (dcDocument == null)
                        dcDocument = new DocumentDataDataContext(GlobalVar.GV.GetInfoTrackConnection());
                    int CntAgreement = dcMaster.Agreements.Where(P => P.ClientID == clientid
                              && P.LoadTypeID == Convert.ToInt32(instance.LoadTypeID)
                              && Convert.ToDateTime(instance.PickUpDate) >= P.FromDate
                              && Convert.ToDateTime(instance.PickUpDate) <= P.ToDate
                              && P.IsTerminated == false
                              && P.IsCancelled == false
                              && P.StatusID == 1).Count();
                    if (CntAgreement == 0)
                    {
                        double AdditionalWeight = 0, TotalCharge = 0;
                        if (Convert.ToDouble(instance.Weight) > 0 && Convert.ToDouble(instance.Weight) <= 0.5)
                            TotalCharge = 85;
                        else
                        {
                            AdditionalWeight = Math.Ceiling((NewWeights - 0.5) / 0.5);
                            TotalCharge = 85 + (AdditionalWeight * 15);
                        }
                        instance.StandardShipment = TotalCharge;
                        return;
                    }
                }

            #endregion

            GetShipmentCharge(NewWeights, clientid, StandardShipment, instance);
        }

        void CalculateTotal(Waybill instance)
        {
            instance.TotalCharge = Convert.ToDouble(Convert.ToDouble(instance.InsuranceCharge) +
                                              Convert.ToDouble(instance.ServiceCharge) +
                                              Convert.ToDouble(instance.SpecialCharge) +
                                              Convert.ToDouble(instance.StorageCharge) +
                                              Convert.ToDouble(instance.ODAStationCharge) +
                                              Convert.ToDouble(instance.ODADStationCharge) +
                                              Convert.ToDouble(instance.PackingCharge) +
                                              Convert.ToDouble(instance.HandlingCharge) +
                                              Convert.ToDouble(instance.TotalGCCCharges) +
                                              Convert.ToDouble(instance.OtherCharge));

        }

        void GetShipmentCharge(double NewWeights, int clientid, double StandardShipment, Waybill instance)
        {
            if (instance.BillingTypeID != null && instance.BillingTypeID.ToString() != "" &&
                Convert.ToInt32(instance.BillingTypeID) == Convert.ToInt32(BillingType.Free))
            {
                instance.StandardShipment = 0;
                instance.ServiceCharge = 0;
                return;
            }

            if (instance.LoadTypeID != null && instance.LoadTypeID.ToString() != "")
            {
                if (instance.Weight != null && instance.Weight.ToString() != "" && Convert.ToDouble(instance.Weight) > 0)
                    if (instance.VolumeWeight != null && instance.VolumeWeight.ToString() != "" && Convert.ToDouble(instance.VolumeWeight) > 0)
                        if (instance.ServiceTypeID != null && instance.ServiceTypeID.ToString() != "" && Convert.ToInt32(instance.ServiceTypeID) > 0 &&
                            (Convert.ToInt32(instance.ServiceTypeID) == Convert.ToInt32(ServiceTypes.Express) || Convert.ToInt32(instance.ServiceTypeID) == Convert.ToInt32(ServiceTypes.DomesticCourier)))
                            if (Convert.ToDouble(instance.Weight) >= Convert.ToDouble(instance.VolumeWeight))
                                NewWeights = Convert.ToDouble(instance.Weight);
                            else
                                NewWeights = Convert.ToDouble(instance.VolumeWeight);

                if (instance.LoadTypeID != null && instance.LoadTypeID.ToString() != "" &&
                    (Convert.ToInt32(instance.LoadTypeID) == Convert.ToInt32(LoadTypes.Pallet) || Convert.ToInt32(instance.LoadTypeID) == Convert.ToInt32(LoadTypes.Drums)))
                    if (instance.PicesCount != null && instance.PicesCount.ToString() != "")
                        NewWeights = Convert.ToDouble(instance.PicesCount);

                if (instance.LoadTypeID != null && instance.LoadTypeID.ToString() != ""
                    && (Convert.ToInt32(instance.LoadTypeID) == Convert.ToInt32(LoadTypes.HalfLoad) || Convert.ToInt32(instance.LoadTypeID) == Convert.ToInt32(LoadTypes.FullLoad)))
                    NewWeights = 1;
            }

            if (instance.BillingTypeID != null &&
                instance.BillingTypeID.ToString() != "" &&
                Convert.ToInt32(instance.BillingTypeID) > 0)
            {
                if (Convert.ToInt32(instance.BillingTypeID) == Convert.ToInt32(BillingType.Account) &&
                    instance.ClientID != null && instance.ClientID.ToString() != "" &&
                    Convert.ToInt32(instance.ClientID) > 0)
                    clientid = Convert.ToInt32(instance.ClientID);
                else
                    if (Convert.ToInt32(instance.BillingTypeID) == Convert.ToInt32(BillingType.ExternalBilling) &&
                        instance.InvoiceAccount != null &&
                        instance.InvoiceAccount.ToString() != "" &&
                        Convert.ToInt32(instance.InvoiceAccount) > 0)
                        clientid = Convert.ToInt32(instance.InvoiceAccount);
                    else
                        if ((Convert.ToInt32(instance.BillingTypeID) == Convert.ToInt32(BillingType.COD) ||
                            (Convert.ToInt32(instance.BillingTypeID) == Convert.ToInt32(BillingType.FOD)) &&
                            instance.InvoiceAccount != null &&
                            instance.InvoiceAccount.ToString() != "" &&
                            Convert.ToInt32(instance.InvoiceAccount) > 0))
                            clientid = Convert.ToInt32(instance.ClientID);
            }

            if (clientid > 0
                && instance.BillingTypeID != null && instance.BillingTypeID.ToString() != "" && Convert.ToInt32(instance.BillingTypeID) > 0
                && instance.LoadTypeID != null && instance.LoadTypeID.ToString() != "" && Convert.ToInt32(instance.LoadTypeID) > 0
                && instance.PickUpDate != null && instance.PickUpDate.ToString() != ""
                && instance.OriginStationID != null && instance.OriginStationID.ToString() != "" && Convert.ToInt32(instance.OriginStationID) > 0
                && instance.DestinationStationID != null && instance.DestinationStationID.ToString() != "" && Convert.ToInt32(instance.DestinationStationID) > 0
                && instance.Weight != null && instance.Weight.ToString() != "")
            {

                if (Convert.ToInt32(instance.BillingTypeID) != Convert.ToInt32(BillingType.Free))
                {
                    bool IsNeedFraction = false;
                    int HWStarting = 250;
                    if (dcDocument == null)
                        dcDocument = new DocumentDataDataContext();
                    Client instanceClient = dcMaster.Clients.First(P => P.ID == Convert.ToInt32(clientid));
                    {
                        if (instanceClient.ISNeedFraction.HasValue)
                            IsNeedFraction = dcMaster.Clients.First(P => P.ID == Convert.ToInt32(clientid)).ISNeedFraction.Value;
                        if (instanceClient.HWStarting.HasValue)
                            HWStarting = instanceClient.HWStarting.Value;
                    }

                    if (GF.shipment == null)
                        GF.shipment = new Common.Shipment(GlobalVar.GV.GetInfoTrackConnection());

                    bool IsRTO = false;
                    if (instance.IsRTO.HasValue)
                        IsRTO = instance.IsRTO.Value;

                    double res = GF.shipment.GetShipmentValue(clientid,
                                                  Convert.ToInt32(instance.LoadTypeID),
                                                  Convert.ToDateTime(instance.PickUpDate),
                                                  Convert.ToInt32(instance.OriginStationID),
                                                  Convert.ToInt32(instance.DestinationStationID),
                                                  Convert.ToDouble(NewWeights), IsNeedFraction, HWStarting, instance.ClientID, instance.BillingTypeID, IsRTO);
                    instance.StandardShipment = res;
                    if (instance != null)
                        instance.ServiceCharge = res;
                }
                else
                    instance.StandardShipment = 0;
                if (GF.shipment.AgreementID.HasValue)
                    try
                    {
                        if (instance != null)
                            instance.AgreementID = GF.shipment.AgreementID.Value;
                    }
                    catch { }

                StandardShipment = GF.shipment.GetFromStandardTariff(Convert.ToDateTime(instance.PickUpDate),
                                                  Convert.ToInt32(instance.LoadTypeID), Convert.ToInt32(instance.OriginStationID),
                                                  Convert.ToInt32(instance.DestinationStationID),
                                                  Convert.ToDouble(NewWeights), 0, null);

                if (instance != null)
                    instance.StandardShipment = StandardShipment;
                if (instance != null)
                    if (GF.shipment.StandardTariffID != null && GF.shipment.StandardTariffID > 0)
                        instance.StandardTariffID = GF.shipment.StandardTariffID.Value;
            }
            else
                if (instance.LoadTypeID != null && instance.LoadTypeID.ToString() != "" && Convert.ToInt32(instance.LoadTypeID) > 0
                && instance.PickUpDate != null
                && instance.OriginStationID != null && instance.OriginStationID.ToString() != "" && Convert.ToInt32(instance.OriginStationID) > 0
                && instance.DestinationStationID != null && instance.DestinationStationID.ToString() != "" && Convert.ToInt32(instance.DestinationStationID) > 0
                && instance.Weight != null && instance.Weight.ToString() != "" && Convert.ToDouble(instance.Weight) > 0)
                {
                    GF.shipment.StandardTariffID = null;
                    instance.StandardShipment = GF.shipment.GetFromStandardTariff(Convert.ToDateTime(instance.PickUpDate),
                                                  Convert.ToInt32(instance.LoadTypeID), Convert.ToInt32(instance.OriginStationID),
                                                  Convert.ToInt32(instance.DestinationStationID),
                                                  Convert.ToDouble(NewWeights), 0, null);
                    if (instance != null)
                        instance.StandardShipment = Convert.ToDouble(instance.StandardShipment);
                    if (instance != null)
                        if (GF.shipment.StandardTariffID != null && GF.shipment.StandardTariffID > 0)
                            instance.StandardTariffID = GF.shipment.StandardTariffID.Value;
                    if (instance != null)
                        instance.AgreementID = 0;
                }

            //if (GlobalVar.GV.IsRetailOutlet)
            //    instance.StandardShipment = Math.Round(Convert.ToDouble(instance.StandardShipment), 0);
        }

        bool IsBothHub(int FromStation, int ToStation)
        {
            bool result = false;
            List<Station> StationList = new List<Station>();
            dcMaster = new MastersDataContext(GlobalVar.GV.GetInfoTrackConnection());
            StationList = dcMaster.Stations.ToList();
            if (StationList.First(P => P.ID == FromStation).IsHub && StationList.First(P => P.ID == ToStation).IsHub)
                result = true;

            return result;
        }
    }
}