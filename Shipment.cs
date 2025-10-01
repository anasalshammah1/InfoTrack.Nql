using InfoTrack.Common.App_Data;
using System;
using System.Collections.Generic;
using System.Linq;


//Applogincheckin
namespace InfoTrack.Common
{
    public class Shipments
    {
        public MastersDataContext dcMaster;
        bool find = false;

        #region Properties

        private int? agreementid = null;
        public int? AgreementID
        {
            get { return agreementid; }
            set { agreementid = value; }
        }

        private int? agreementrouteid = null;
        public int? AgreementRouteID
        {
            get { return agreementrouteid; }
            set { agreementrouteid = value; }
        }

        private int? agreementslabid = null;
        public int? AgreementSlabID
        {
            get { return agreementslabid; }
            set { agreementslabid = value; }
        }

        private int? standardtariffid = null;
        public int? StandardTariffID
        {
            get { return standardtariffid; }
            set { standardtariffid = value; }
        }

        private int? standardtariffdetailid = null;
        public int? StandardTariffDetailID
        {
            get { return standardtariffdetailid; }
            set { standardtariffdetailid = value; }
        }

        private int? internationalstandardtariffid = null;
        public int? InternationalStandardTariffID
        {
            get { return internationalstandardtariffid; }
            set { internationalstandardtariffid = value; }
        }

        private int? internationalstandardtariffdetailid = null;
        public int? InternationalStandardTariffDetailID
        {
            get { return internationalstandardtariffdetailid; }
            set { internationalstandardtariffdetailid = value; }
        }

        private double heavyweightvalue = 0;
        public double HeavyWeightValue
        {
            get { return heavyweightvalue; }
            set { heavyweightvalue = value; }
        }

        private double expressvalue = 0;
        public double ExpressValue
        {
            get { return expressvalue; }
            set { expressvalue = value; }
        }

        private bool fromagreement = false;
        public bool FromAgreement
        {
            get { return fromagreement; }
            set { fromagreement = value; }
        }

        private int HWStarting = 250;

        private int Express
        {
            get { return 1; }
        }

        private int HeavyWeight
        {
            get { return 2; }
        }

        private int LTL
        {
            get { return 3; }
        }

        private int Pallet
        {
            get { return 7; }
        }

        private int AviationKG
        {
            get { return 75; }
        }

        private List<Station> StationList = new List<Station>();
        //private Dictionary<string, string> dic;

        #endregion

        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultsConnectionString"].ConnectionString;
        public Shipments(string ConnectionString)
        {
            dcMaster = new MastersDataContext(connectionString);
            StationList = dcMaster.Stations.ToList();
        }

        bool FromExpress = false;
        bool FoundExpressSlab = false;
        bool FoundHWSlab = false;
        bool UseHW = false;

        #region Domestics

        public double GetShipmentValue(int ClientID, int LoadTypeID, DateTime date, int FromStation, int ToStation, double Weight, bool IsNeedFraction, int hwstarting
            , int ShipperID, int BillingTypeID, bool IsRTO)
        {
            dcMaster = new MastersDataContext(connectionString);
            int ServiceTypeIDs = Convert.ToInt32(dcMaster.LoadTypes.First(C => C.ID == LoadTypeID).ServiceTypeID);
            if (!dcMaster.ServiceTypes.First(P => P.ID == ServiceTypeIDs).HasFraction)
                Weight = System.Math.Round(Weight, 0);
            date = new DateTime(date.Year, date.Month, date.Day);
            HWStarting = hwstarting;
            FromExpress = false;
            FoundExpressSlab = false;
            FoundHWSlab = false;
            UseHW = false;

            if (LoadTypeID == 2)
                UseHW = true;

            find = false;
            AgreementID = null;
            AgreementRouteID = null;
            AgreementSlabID = null;
            StandardTariffID = null;
            StandardTariffDetailID = null;
            HeavyWeightValue = 0;
            ExpressValue = 0;
            //dic = new Dictionary<string, string>();

            double ShipmentValue = 0;
            if (Weight >= HWStarting && LoadTypeID == Express)
                ShipmentValue = GetShipmentValues(ClientID, HeavyWeight, date, FromStation, ToStation, Weight, IsNeedFraction, false, ShipperID, BillingTypeID, IsRTO);
            else
                ShipmentValue = GetShipmentValues(ClientID, LoadTypeID, date, FromStation, ToStation, Weight, IsNeedFraction, false, ShipperID, BillingTypeID, IsRTO);

            if (ShipmentValue > 0 && AgreementID.HasValue)
            {
                AgreementsDataContext dcAgreement = new AgreementsDataContext(connectionString);
                App_Data.Agreement agrInstance = dcAgreement.Agreements.First(P => P.ID == AgreementID.Value);
                if (agrInstance.AddPercentage.HasValue && agrInstance.AddPercentage.Value > 0)
                    ShipmentValue = ShipmentValue + (ShipmentValue * (agrInstance.AddPercentage.Value / 100));

                List<AgreementIncreasing> PercentageList = dcAgreement.AgreementIncreasings.Where(P => P.AgreementID == AgreementID.Value &&
                                                                                          P.StatusID == 1 &&
                                                                                          date >= P.FromDate &&
                                                                                          date <= P.ToDate).ToList();
                for (int i = 0; i < PercentageList.Count; i++)
                {
                    if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.Increase))
                        ShipmentValue = ShipmentValue + (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                        if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.Decrease))
                        ShipmentValue = ShipmentValue - (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                            if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.DecreaseRTOWaybills))
                    {
                        if (ShipperID == 1024600 && IsRTO && BillingTypeID == 3)
                            ShipmentValue = ShipmentValue - (ShipmentValue * (PercentageList[i].Percentage / 100));
                    }
                    else
                                if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.FixedAmountforRTOWaybills))
                        if ((ShipperID == 1024600 || ShipperID == 1024602) && IsRTO && BillingTypeID == 3)
                            ShipmentValue = PercentageList[i].Percentage;
                }
                //ShipmentValue = ShipmentValue + (ShipmentValue * (PercentageList[i].Percentage / 100));
            }
            else
                if (ShipmentValue == 0 && AgreementID.HasValue && HasBandAgreement(AgreementID.Value))
            {
                AgreementsDataContext dcAgreement = new AgreementsDataContext(connectionString);
                App_Data.Agreement agrInstance = dcAgreement.Agreements.First(P => P.ID == AgreementID.Value);
                if (agrInstance.AddPercentage.HasValue && agrInstance.AddPercentage.Value > 0)
                    ShipmentValue = ShipmentValue + (ShipmentValue * (agrInstance.AddPercentage.Value / 100));

                List<AgreementIncreasing> PercentageList = dcAgreement.AgreementIncreasings.Where(P => P.AgreementID == AgreementID.Value &&
                                                                                          P.StatusID == 1 &&
                                                                                          date >= P.FromDate &&
                                                                                          date <= P.ToDate).ToList();
                if (AgreementID.HasValue && ShipmentValue <= 0)
                    if (dcAgreement.Agreements.First(P => P.ID == AgreementID.Value).StandardTariffID.HasValue)
                        ShipmentValue = GetFromStandardTariff(dcAgreement.Agreements.First(P => P.ID == AgreementID.Value).StandardTariffID.Value, FromStation, ToStation, Weight, 0, 0);

                for (int i = 0; i < PercentageList.Count; i++)
                {
                    if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.Increase))
                        ShipmentValue = ShipmentValue + (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                        if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.Decrease))
                        ShipmentValue = ShipmentValue - (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                            if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.IncreaseFromLocalStandardTarif))
                        ShipmentValue = ShipmentValue + (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                                if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.DecreaseFromLocalStandardTariff))
                        ShipmentValue = ShipmentValue - (ShipmentValue * (PercentageList[i].Percentage / 100));
                    else
                                    if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.DecreaseRTOWaybills))
                    {
                        if (ShipperID == 1024600 && IsRTO && BillingTypeID == 3)
                            ShipmentValue = ShipmentValue - (ShipmentValue * (PercentageList[i].Percentage / 100));
                    }
                    else
                                        if ((PercentageList[i] as AgreementIncreasing).AgreementIncDecTypeID == Convert.ToInt32(GlobalVarCommon.AgreementType.FixedAmountforRTOWaybills))
                        if ((ShipperID == 1024600 || ShipperID == 1024602) && IsRTO && BillingTypeID == 3)
                            ShipmentValue = PercentageList[i].Percentage;
                }
            }

            if (IsNeedFraction)
                return Math.Round(ShipmentValue, 2);
            else
                return Math.Round(ShipmentValue, 0, MidpointRounding.AwayFromZero);
        }

        private bool HasBandAgreement(int AgrID)
        {
            bool result = false;

            App_Data.AgreementsDataContext dcAgg = new AgreementsDataContext();
            if (dcAgg.ViwBandAgreements.Where(P => P.ID == AgrID && (P.AgreementIncDecTypeID == 3 || P.AgreementIncDecTypeID == 4)).Count() > 0)
                result = true;

            return result;
        }

        private double GetShipmentValues(int ClientID, int LoadTypeID, DateTime date, int FromStation, int ToStation, double Weight, bool IsNeedFraction, bool UseExpress
            , int ShipperID, int BillingTypeID, bool IsRTO)
        {
            double ShipmentValue = 0;
            FromAgreement = false;
            dcMaster = new MastersDataContext(connectionString);

            AgreementsDataContext dcAgreement = new AgreementsDataContext(connectionString);
            List<Agreement> agreementList = new List<Agreement>();
            //string clientIds = GlobalVarCommon.GV.GetSystemVariables("CashCustomerIDs");
            //List<string> clientId = clientIds.Split(',').ToList<string>();

            if (!InBlackList(ClientID, date) /*|| (BillingTypeID == 2 && clientId.Contains(ClientID.ToString()))*/)
            {
                if (UseExpress)
                    agreementList = dcAgreement.Agreements.Where(P => P.ClientID == ClientID
                    && P.LoadTypeID == Express
                    && date >= P.FromDate
                    && date <= P.ToDate
                    && P.IsTerminated == false
                    && P.IsCancelled == false
                    && P.StatusID == 1).ToList();
                else
                    agreementList = dcAgreement.Agreements.Where(P => P.ClientID == ClientID
                        && P.LoadTypeID == LoadTypeID
                        && date >= P.FromDate
                        && date <= P.ToDate
                        && P.IsTerminated == false
                        && P.IsCancelled == false
                        && P.StatusID == 1).ToList();
            }

            if (agreementList.Count > 0)
            {
                find = false;
                int FromNEWS = 0, ToNEWS = 0;
                FromNEWS = GetNEWS(FromStation);
                ToNEWS = GetNEWS(ToStation);
                List<int> AlreadyCheckedRouteList;

                for (int i = 0; i < agreementList.Count; i++)
                {
                    if (find)
                        break;

                    AlreadyCheckedRouteList = new List<int>();
                    List<AgreementRoute> routeList = dcAgreement.AgreementRoutes.Where(P => P.AgreementID == agreementList[i].ID &&
                                                                                       P.StatusID == 1).ToList();
                    bool HasFraction = false;
                    try
                    {
                        if (dcMaster.ViwLoadTypes.First(P => P.ID == LoadTypeID).HasFraction.Value)
                            HasFraction = true;
                    }
                    catch { }
                    int? currentroutes = null;
                    currentroutes = GetAgreementRoute(routeList, FromStation, ToStation, FromNEWS, ToNEWS, AlreadyCheckedRouteList);
                    if (currentroutes.HasValue)
                    {
                        AlreadyCheckedRouteList.Add(currentroutes.Value);
                        ShipmentValue = GetShipment(Weight, routeList[currentroutes.Value], FromStation, ToStation, date, dcAgreement, LoadTypeID, agreementList[i], HasFraction);
                        if (ShipmentValue > 0)
                        {
                            find = true;
                            break;
                        }
                        else
                            continue;
                    }
                    else
                        if (agreementList[i].StandardTariffID.HasValue)
                    {
                        ShipmentValue = GetFromStandardTariff(agreementList[i].StandardTariffID.Value, FromStation, ToStation, Weight, 0, agreementList[i].LoadTypeID);
                        if (ShipmentValue > 0)
                        {
                            find = true;
                            AgreementID = agreementList[i].ID;
                            break;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
                        if (ShipmentValue > 0)
                        {
                            find = true;
                            break;
                        }
                        else
                            continue;
                    }
                }
            }
            else
            {
                if (LoadTypeID == HeavyWeight)
                    ShipmentValue = GetShipmentValues(ClientID, Express, date, FromStation, ToStation, Weight, IsNeedFraction, true, ShipperID, BillingTypeID, IsRTO);
                else
                    //if (LoadTypeID == Pallet || LoadTypeID == LTL)
                    //{
                    //    ShipmentValue = 0;
                    //    find = true;
                    //}
                    if (LoadTypeID == Pallet)
                {
                    ShipmentValue = 0;
                    find = true;
                }
                else
                        if (LoadTypeID == LTL)
                {
                    ShipmentValue = GetFromStandardTariff(date, LTL, FromStation, ToStation, Weight, 0, null);
                    //ShipmentValue = 0;
                    find = true;
                }
                else
                            if (!find)
                {
                    if (Weight >= HWStarting && LoadTypeID == Express && !UseExpress)
                        ShipmentValue = GetShipmentValue(ClientID, HeavyWeight, date, FromStation, ToStation, Weight, IsNeedFraction, HWStarting, ShipperID, BillingTypeID, IsRTO);
                    else
                        ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
                }
                else
                                if (find)
                {
                    if (Weight >= HWStarting && (LoadTypeID == Express) && ShipmentValue == 0 && !UseExpress)
                        ShipmentValue = GetShipmentValue(ClientID, HeavyWeight, date, FromStation, ToStation, Weight, IsNeedFraction, HWStarting, ShipperID, BillingTypeID, IsRTO);
                }
            }

            if (!find)
            {
                ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
                if (ShipmentValue > 0)
                    find = true;
            }

            if ((LoadTypeID == HeavyWeight || LoadTypeID == Express) && Weight >= HWStarting && !UseExpress)
            {
                if (LoadTypeID == HeavyWeight)
                    HeavyWeightValue = ShipmentValue;

                FromExpress = false;
                ExpressValue = GetShipmentValues(ClientID, Express, date, FromStation, ToStation, Weight, IsNeedFraction, true, ShipperID, BillingTypeID, IsRTO);

                if (FoundExpressSlab && !UseHW && !FoundHWSlab)
                    ShipmentValue = ExpressValue;
                else
                    if (HeavyWeightValue < ExpressValue && HeavyWeightValue > 0)
                    ShipmentValue = HeavyWeightValue;
                else
                        if (ExpressValue > 0 && FromExpress)
                    ShipmentValue = ExpressValue;
            }

            //if (ShipmentValue == 0)
            //{
            //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null, HWStarting);
            //}

            return ShipmentValue;
        }

        private bool AlreadyChecked(List<int> AvailbleRouteList, int j)
        {
            bool result = false;

            if (AvailbleRouteList.IndexOf(j) >= 0)
                result = true;

            return result;
        }

        private int? GetAgreementRoute(List<AgreementRoute> routeList, int FromStation, int ToStation, int FromNEWS, int ToNEWS, List<int> AvailbleRouteList)
        {
            int? route = null;

            #region Conditions

            #region From Specific Station to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == ToStation && routeList[j].DestinationStationID == FromStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Sub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == 0 && !IsStationHub(ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == 0 && !IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Hub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == -1 && IsStationHub(ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == -1 && IsStationHub(FromStation) && (routeList[j].OriginStationID == ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Hub Station)

            if (IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                {
                    bool same = FromStation == ToStation;
                    if (!same)
                    {
                        if (routeList[j].OriginStationID != routeList[j].DestinationStationID)
                        {
                            if ((routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation)) ||
                            (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation))
                                return j;
                        }
                    }
                    else
                    {
                        if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation))
                            return j;
                    }
                }

            if (IsStationHub(FromStation))
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (find)
                        break;

                    bool same = FromStation == ToStation;
                    if (!same)
                    {
                        if (routeList[j].OriginStationID != routeList[j].DestinationStationID)
                        {
                            if ((routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation)) ||
                            (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation))
                                return j;
                        }
                    }
                    else
                    {
                        if (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation)
                            return j;
                    }
                }
            }

            #endregion

            #region From Specific Station to (Hub - Western , Eastern , Central)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToNEWS)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromNEWS && routeList[j].OriginStationID == ToStation)
                    return j;

            #endregion

            #region From Specific Station To (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == -2)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == ToStation)
                    return j;

            #endregion

            #region Fom (Hub) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -1 && IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && IsStationHub(ToStation) && routeList[j].OriginStationID == -1)
                    return j;

            #endregion

            #region Fom (Hub Station) to Specific Station

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                        return j;

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToStation && IsStationHub(ToStation))
                        return j;

            #endregion

            #region Fom (Hub - Western , Eastern , Central ) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromNEWS && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToNEWS)
                    return j;

            #endregion

            #region Fom (Sub) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == 0 && !IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == 0 && !IsStationHub(ToStation))
                    return j;

            #endregion

            #region From All Saudia Truck To Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -2 && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == -2)
                    return j;

            #endregion

            #region From (Hub) To (Hub)

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == -1)
                        return j;

            #endregion

            #region From (Hu Stationb) To (Hub Station)

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation)
                        return j;

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == ToStation && routeList[j].DestinationStationID == FromStation)
                        return j;

            #endregion

            #region From (Sub) To (Hub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == 0 && !IsStationHub(FromStation) && (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA)))
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA)) && routeList[j].OriginStationID == 0))
                    return j;

            #endregion

            #region From (Hub) To (Sub)

            if (IsHubToSub(FromStation, ToStation) || IsHubToSub(ToStation, FromStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0 && IsHubToSub(FromStation, ToStation))
                        return j;

            if (IsHubToSub(FromStation, ToStation) || IsHubToSub(ToStation, FromStation))
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (find)
                        break;
                    if ((routeList[j].DestinationStationID == 0 && routeList[j].OriginStationID == -1 && IsHubToSub(ToStation, FromStation)))
                    {
                        bool f = false;
                        for (int n = 0; n < routeList.Count; n++)
                        {
                            if (routeList[n].DestinationStationID == 0 && routeList[n].OriginStationID == -1)
                            {
                                f = true;
                                break;
                            }
                        }
                        if (f) break;

                        return j;
                    }
                }
            }

            #endregion

            #region From (Sub) To (Hub)
            //notes
            if (IsSubToHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == 0 && routeList[j].DestinationStationID == -1)
                        return j;

            //if (IsSubToHub(ToStation, FromStation))
            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == 0 && IsStationHub(ToStation) && routeList[j].OriginStationID == -1 && !IsStationHub(FromStation)))
                    return j;

            #endregion

            #region From (Sub) To (Sub)

            if (IsBothSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == 0 && routeList[j].DestinationStationID == 0)
                        return j;

            #endregion

            #region From (Hub) to (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -1 && IsStationHub(FromStation) && routeList[j].DestinationStationID == -2)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == -1 && IsStationHub(ToStation)))
                    return j;

            #endregion

            #region From (Hub Station) to (All Saudia Truck)

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && IsStationHub(FromStation) && routeList[j].DestinationStationID == -2)
                        return j;


            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if ((routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == ToStation && IsStationHub(ToStation)))
                        return j;

            #endregion

            #region From (Hub - Western , Eastern , Central) to (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromNEWS && routeList[j].DestinationStationID == 0)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == 0 && routeList[j].DestinationStationID == ToNEWS))
                    return j;

            #endregion



            #region From (Sub Station) To (Hub Station)

            if (IsSubToHub(FromStation, ToStation) || IsHubToSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsSubToHub(FromStation, ToStation))
                        return j;

            if (IsSubToHub(FromStation, ToStation) || IsHubToSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if ((routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToStation && IsHubToSub(FromStation, ToStation)))
                        return j;

            #endregion

            #region From Sub to All Saudia Truck
            //if (!find)
            //{
            //    if (!IsStationHub(FromStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region From All Saudia Truck To Hub
            //if (!find)
            //{
            //    if (IsStationHub(ToStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            if (routeList[j].DestinationStationID == -1)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region From All Saudia Truck To Sub
            //if (!find)
            //{
            //    if (!IsStationHub(ToStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            if (routeList[j].DestinationStationID == 0)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region Centeral , Western , Eastern

            //From Centeral To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Eastern To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Western To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //From Centeral To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //---------From Centeral To Eastern Vis Vers
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Easter To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //-----------From Easter To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Centeral To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //-----------From Centeral To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Western To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //-----------From Western To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //From Eastern To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //----------From Eastern To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Western To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //-------From Western To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            #endregion

            //#region Centeral , Western , Eastern

            ////From Centeral To Centeral
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
            //        return j;
            //}

            ////From Eastern To Eastern
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Western To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Centeral To Eastern
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Eastern To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}






            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if ((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.RUH)))
            //        return j;
            //}

            ////From Centeral To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
            //        return j;
            //}

            ////From Eastern To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Eastern To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if ((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.JED)))
            //        return j;
            //}

            //#endregion

            #region From All Saudia Truck To All Saudia Truck

            for (int w = 0; w < routeList.Count; w++)
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (routeList[j].OriginStationID == -2 && routeList[j].DestinationStationID == -2)
                    {
                        if (AlreadyChecked(AvailbleRouteList, j))
                            continue;
                        else
                            return j;
                    }
                }
            }

            #endregion

            #endregion

            return route;
        }
        #region Sub and Hub

        private bool IsHubToSub(int FromStation, int ToStation)
        {
            bool result = false;
            if (StationList.First(P => P.ID == FromStation).IsHub && StationList.First(P => P.ID == ToStation).IsHub == false)
                result = true;

            return result;
        }

        private bool IsSubToHub(int FromStation, int ToStation)
        {
            bool result = false;
            if (StationList.First(P => P.ID == FromStation).IsHub == false && StationList.First(P => P.ID == ToStation).IsHub)
                result = true;

            return result;
        }

        private bool IsBothHub(int FromStation, int ToStation)
        {
            bool result = false;
            if (StationList.First(P => P.ID == FromStation).IsHub && StationList.First(P => P.ID == ToStation).IsHub)
                result = true;

            return result;
        }

        private bool IsBothSub(int FromStation, int ToStation)
        {
            bool result = false;
            if (!StationList.First(P => P.ID == FromStation).IsHub && !StationList.First(P => P.ID == ToStation).IsHub)
                result = true;

            return result;
        }

        private bool IsStationHub(int FromStation)
        {
            bool result = false;
            if (StationList.First(P => P.ID == FromStation).IsHub)
                result = true;

            return result;
        }

        #endregion

        private double GetShipment(double Weight, AgreementRoute CurrentRoute, int FromStation, int ToStation, DateTime date,
                                   AgreementsDataContext dcAgreement, int LoadTypeID, Agreement CurrentAgreement, bool HasFraction)
        {
            double ShipmentValue = 0;
            find = false;

            //Get Value Form Current Route
            if (Weight <= CurrentRoute.MinWeight)
            {
                if (LoadTypeID != HeavyWeight && LoadTypeID != LTL)
                {
                    ShipmentValue = CurrentRoute.MinCharge;
                    find = true;
                    FromExpress = true;
                    AgreementID = CurrentAgreement.ID;
                    AgreementRouteID = CurrentRoute.ID;
                    //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                    return ShipmentValue;
                }
                else
                {
                    if (CurrentAgreement.StandardTariffID != null)
                    {
                        ShipmentValue = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                        double discount = 0;
                        discount = ShipmentValue * (CurrentRoute.MinCharge / 100);
                        ShipmentValue = ShipmentValue - discount;
                        find = true;
                        FromExpress = true;
                        AgreementID = CurrentAgreement.ID;
                        AgreementRouteID = CurrentRoute.ID;
                        //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                        return ShipmentValue;
                    }
                    else
                    {
                        ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, agreementid);// CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                        double discount = 0;
                        discount = ShipmentValue * (CurrentRoute.MinCharge / 100);
                        ShipmentValue = ShipmentValue - discount;
                        find = true;
                        FromExpress = true;
                        AgreementID = CurrentAgreement.ID;
                        AgreementRouteID = CurrentRoute.ID;
                        //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                        return ShipmentValue;
                    }
                    //ShipmentValue = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                    //double discount = 0;
                    //discount = ShipmentValue * (CurrentRoute.MinCharge / 100);
                    //ShipmentValue = ShipmentValue - discount;
                    //find = true;
                    //FromExpress = true;
                    //AgreementID = CurrentAgreement.ID;
                    //AgreementRouteID = CurrentRoute.ID;
                    ////AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                    //return ShipmentValue;
                }
            }
            else
            {
                // Else Find Value From Slabs
                List<AgreementSlab> slabList = dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID && P.StatusID == 1).ToList();
                ShipmentValue = CheckInSlabs(Weight, slabList, ShipmentValue, CurrentRoute, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }

        private double CheckInSlabs(double Weight, List<AgreementSlab> slabList, double ShipmentValue,
                                   AgreementRoute CurrentRoute, Agreement CurrentAgreement, AgreementsDataContext dcAgreement,
                                  int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double Results = ShipmentValue;
            find = false;
            for (int r = 0; r < slabList.Count; r++)
            {
                if (find)
                    break;

                //If Current Slab is suitable depend on weight > from weight and < to weight.
                if (Weight >= slabList[r].FromWeight && Weight <= slabList[r].ToWeight)
                {
                    if (LoadTypeID == 1)
                        FoundExpressSlab = true;
                    else
                        if (LoadTypeID == 2)
                        FoundHWSlab = true;
                    // If Current Slab Charge Type FOC ( Free Of Charge ).
                    if (slabList[r].ChargeTypeID == 4)
                    {
                        // If Current Slab Charge Type (FOC) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            Results = 0;
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            //AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (FOC) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            List<AgreementSlab> slabListDetail = dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                                        && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetFOCShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                        // If Current Slab Charge Type Fixed.
                        if (slabList[r].ChargeTypeID == 3)
                    {
                        // If Current Slab Charge Type (Fixed) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            Results = slabList[r].Amount;
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            //AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (Fixed) and Cumulative ( From Previous, From Beinning , Slab Option ).
                        {
                            List<AgreementSlab> slabListDetail = dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                                        && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetFixedShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                            // If Current Slab Charge Type %.
                            if (slabList[r].ChargeTypeID == 2)
                    {
                        // If Current Slab Charge Type (%) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            //If Standard Taiff in the Top Specified
                            if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                            {
                                Results = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                                double val = Results * (slabList[r].Amount / 100);
                                Results = Results - val;
                                find = true;
                                FromExpress = true;
                                AgreementID = CurrentAgreement.ID;
                                AgreementRouteID = CurrentRoute.ID;
                                AgreementSlabID = slabList[r].ID;
                                break;
                            }
                            else
                            {
                                //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                                standardtariffList = new List<StandardTariff>();
                                standardtariffList = dcMaster.StandardTariffs.Where(P => P.LoadTypeID == LoadTypeID
                                            && CurrentAgreement.FromDate >= P.FromDate && CurrentAgreement.FromDate <= P.ToDate && P.StatusID == 1).ToList();
                                if (standardtariffList.Count > 0)
                                    Results = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                                else
                                    Results = GetFromStandardTariff(CurrentRoute, FromStation, ToStation, LoadTypeID, Weight, date, HWStarting);
                                double val = Results * (slabList[r].Amount / 100);
                                Results = Results - val;
                                find = true;
                                FromExpress = true;
                                AgreementID = CurrentAgreement.ID;
                                AgreementRouteID = CurrentRoute.ID;
                                AgreementSlabID = slabList[r].ID;
                                break;
                            }
                        }
                        else
                        // If Current Slab Charge Type (%) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            List<AgreementSlab> slabListDetail =
                                        dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                                        && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetPercentageShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                                // If Current Slab Charge Type Kgs.
                                if (slabList[r].ChargeTypeID == 1)
                    {
                        // If Current Slab Charge Type (Kgs) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            if (!HasFraction)
                                Results = Weight * slabList[r].Amount;
                            else
                            {
                                double val = ((slabList[r].FromWeight * 1000) - 1) / 1000;
                                Weight = Weight - val;
                                Results = Math.Truncate(Weight / slabList[r].ForEachFraction) * slabList[r].Amount;
                                if (Convert.ToDouble((Weight) % slabList[r].ForEachFraction) > 0)
                                    Results += slabList[r].Amount;
                            }

                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            ////AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (Kgs) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            List<AgreementSlab> slabListDetail =
                                        dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                                        && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetKGShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                }
            }
            return Results;
        }

        private double GetKGShipment(double Weight, AgreementRoute CurrentRoute, List<AgreementSlab> slabList, int P,
                                   int CumulativeID, double PreviousShipmentValue, Agreement CurrentAgreement, AgreementsDataContext dcAgreement,
                                   int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (Kgs) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                    if (!HasFraction)
                    {
                        ShipmentValue += Weight * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                    else
                    {
                        ShipmentValue += (Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                else
                    if (CurrentRoute.MinCharge > 0)
                {
                    ShipmentValue += CurrentRoute.MinCharge;
                    //AddData(SName.AgreementRoute, slabList[P].ID, ShipmentValue);
                }
                else
                    ShipmentValue += GetFromStandardTariff(CurrentRoute, FromStation, ToStation, LoadTypeID, CurrentRoute.MinWeight, date, HWStarting);
            }
            else
                // If Current Slab Charge Type (Kgs) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    if (!HasFraction)
                    {
                        ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    if (!HasFraction)
                    {
                        ShipmentValue += (Weight - CurrentRoute.MinWeight) * slabList[P].Amount;
                        //AddData(SName.AgreementRoute, CurrentRoute.ID, ShipmentValue);
                    }
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (Kgs) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    if (!HasFraction)
                        ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    if (!HasFraction)
                        ShipmentValue += (Weight - CurrentRoute.MinWeight) * slabList[P].Amount;
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (Kgs) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
                ShipmentValue = GetSlabOptionValue(Weight, CurrentRoute, slabList);

            return ShipmentValue;
        }

        private double GetSlabOptionValue(double Weight, AgreementRoute CurrentRoute, List<AgreementSlab> slabList)
        {
            double ShipmentValue = 0;

            bool f = false;
            double newWeight = Weight - CurrentRoute.MinWeight;
            for (int i = 0; i < slabList.Count; i++)
            {
                if (newWeight >= slabList[i].FromWeight && newWeight <= slabList[i].ToWeight)
                {
                    ShipmentValue = newWeight * slabList[i].Amount;
                    f = true;
                    break;
                }
            }

            if (!f && Weight > CurrentRoute.MinWeight)
                ShipmentValue = newWeight * slabList[0].Amount;
            ShipmentValue += CurrentRoute.MinCharge;

            return ShipmentValue;
        }

        private double GetFixedShipment(double Weight, AgreementRoute CurrentRoute, List<AgreementSlab> slabList, int P,
                                   int CumulativeID, double PreviousShipmentValue, Agreement CurrentAgreement, AgreementsDataContext dcAgreement,
                                   int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (Fixed) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                    if (!HasFraction)
                        ShipmentValue += slabList[P].Amount;
                    else
                        ShipmentValue += CurrentRoute.MinCharge;
                else
                    ShipmentValue += slabList[P].Amount;
            }
            else
                // If Current Slab Charge Type (Fixed) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    ShipmentValue += slabList[P].Amount;

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += slabList[P].Amount;
                    ShipmentValue += CurrentRoute.MinCharge;
                }
            }
            else
                    // If Current Slab Charge Type (Fixed) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    ShipmentValue += slabList[P].Amount;

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += slabList[P].Amount;
                    ShipmentValue += CurrentRoute.MinCharge;
                }
            }
            else
                        // If Current Slab Charge Type (Fixed) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.MinCharge;

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetKGShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                    //If Comming Slab Charge Type is %
                    if (slabList[P - 1].ChargeTypeID == 2)
                    ShipmentValue = GetPercentageShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetFixedShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                            //If Comming Slab Charge Type is FOC
                            if (slabList[P - 1].ChargeTypeID == 4)
                    ShipmentValue = GetFOCShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }

        private double GetFOCShipment(double Weight, AgreementRoute CurrentRoute, List<AgreementSlab> slabList, int P,
                               int CumulativeID, double PreviousShipmentValue, Agreement CurrentAgreement, AgreementsDataContext dcAgreement,
                               int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (fOC) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                }
                else
                {
                    ShipmentValue += CurrentRoute.MinCharge;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                }
            }
            else
                // If Current Slab Charge Type (fOC) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (fOC) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                            if (P == 0)
                {
                    ShipmentValue += (Weight - CurrentRoute.MinWeight) * slabList[P].Amount;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (fOC) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.MinCharge;
                //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetKGShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                    //If Comming Slab Charge Type is %
                    if (slabList[P - 1].ChargeTypeID == 2)
                    ShipmentValue = GetPercentageShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetFixedShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                            //If Comming Slab Charge Type is FOC
                            if (slabList[P - 1].ChargeTypeID == 4)
                    ShipmentValue = GetFOCShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }

        private double GetPercentageShipment(double Weight, AgreementRoute CurrentRoute, List<AgreementSlab> slabList, int P,
                               int CumulativeID, double PreviousShipmentValue, Agreement CurrentAgreement, AgreementsDataContext dcAgreement,
                               int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (%) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0 && LoadTypeID != 2 && LoadTypeID != 3)
                    ShipmentValue += 0;
                else
                    if (P == 0 && (LoadTypeID == 2 || LoadTypeID == 3))
                {
                    //If Standard Taiff in the Top Specified
                    if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    {
                        double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                    else
                    {
                        //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                        standardtariffList = new List<StandardTariff>();
                        standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                                    && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                        double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                }
                else
                        if (CurrentRoute.MinCharge > 0)
                    ShipmentValue += CurrentRoute.MinCharge;
                else
                    ShipmentValue += GetFromStandardTariff(CurrentRoute, FromStation, ToStation, LoadTypeID, CurrentRoute.MinWeight, date, HWStarting);
            }
            else
                // If Current Slab Charge Type (%) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    //ShipmentValue += 0;

                    //If Standard Taiff in the Top Specified
                    if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    {
                        double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                    else
                    {
                        //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                        standardtariffList = new List<StandardTariff>();
                        standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                                    && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                        double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);

                }
                else
                    if (P == 0)
                {
                    //ShipmentValue += 0;

                    //If Standard Taiff in the Top Specified
                    if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    {
                        double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                    else
                    {
                        //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                        standardtariffList = new List<StandardTariff>();
                        standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                                    && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                        double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (%) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    //ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;

                    //If Standard Taiff in the Top Specified
                    if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    {
                        double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                    else
                    {
                        //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                        standardtariffList = new List<StandardTariff>();
                        standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                                    && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                        double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    //ShipmentValue += (Weight - CurrentRoute.MinWeight) * slabList[P].Amount;

                    //If Standard Taiff in the Top Specified
                    if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    {
                        double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - CurrentRoute.MinWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }
                    else
                    {
                        //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                        standardtariffList = new List<StandardTariff>();
                        standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                                    && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                        double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - CurrentRoute.MinWeight, 0, LoadTypeID, CurrentRoute);
                        double val = v * (slabList[P].Amount / 100);
                        ShipmentValue += v - val;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (%) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.MinCharge;

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                    //If Comming Slab Charge Type is %
                    if (slabList[P - 1].ChargeTypeID == 2)
                    ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                            //If Comming Slab Charge Type is FOC
                            if (slabList[P - 1].ChargeTypeID == 4)
                    ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }

        private enum RegionNEWS : int
        {
            RUH = -3,
            JED = -4,
            DHA = -5
        }

        private enum SName : int
        {
            Agreement = 1,
            AgreementRoute = 2,
            AgreementSlab = 3,
            StandardTariff = 4,
            StandardTariffDetail = 5
        }

        private int GetNEWS(int StationID)
        {
            int result = 0;

            if (dcMaster == null)
                dcMaster = new MastersDataContext(connectionString);
            if (dcMaster.ViwSatelliteCities.Where(P => P.StationID == StationID).Count() > 0)
                result = dcMaster.ViwSatelliteCities.First(P => P.StationID == StationID).SatelliteCityID;

            if (result == 1)
                result = Convert.ToInt32(RegionNEWS.RUH);
            else
                if (result == 2)
                result = Convert.ToInt32(RegionNEWS.JED);
            else
                    if (result == 3)
                result = Convert.ToInt32(RegionNEWS.DHA);

            return result;
        }

        private bool InBlackList(int ClientID, DateTime dates)
        {
            bool result = false;
            dcMaster = new MastersDataContext(connectionString);
            int count = dcMaster.ClientBlackLists.Where(P => P.ClientID == ClientID && dates >= P.FromDate && dates <= P.ToDate && P.StatusID != 3 && P.Amount == 0).Count();
            if (count > 0)
                result = true;
            return result;
        }

        private int GetDefaultStandardTariffID(int LoadTypeID)
        {
            int standardtariffid = 0;
            standardtariffList = new List<StandardTariff>();
            standardtariffList = dcMaster.StandardTariffs.Where(P => P.LoadTypeID == LoadTypeID && P.StatusID == 1).ToList();
            for (int i = 0; i < standardtariffList.Count; i++)
                if (standardtariffList[i].ToDate > DateTime.Now)
                    return standardtariffList[i].ID;
            return standardtariffid;
        }

        #region Standard Tariff Calculation

        bool found = false;
        List<StandardTariff> standardtariffList;
        public double GetFromStandardTariff(DateTime date, int LoadTypeID, int FromStation, int ToStation, double Weight, double MWeight, int? agreeID)
        {
            double ShipmentValue = 0;
            standardtariffList = new List<StandardTariff>();
            found = false;
            dcMaster = new MastersDataContext(connectionString);

            if (Weight > HWStarting && LoadTypeID == Express)
                LoadTypeID = HeavyWeight;

            #region Get Standard Tariff No By Agreement Date

            if (!StandardTariffID.HasValue)
            {
                if (agreeID.HasValue)
                {
                    App_Data.AgreementsDataContext dcAgreement = new AgreementsDataContext(connectionString);
                    Agreement instanceAgreement = dcAgreement.Agreements.First(P => P.ID == agreeID);
                    standardtariffList = dcMaster.StandardTariffs.Where(P => P.LoadTypeID == LoadTypeID
                    && P.StatusID == 1).ToList();
                    for (int i = 0; i < standardtariffList.Count; i++)
                    {
                        if (instanceAgreement.FromDate >= standardtariffList[i].FromDate && instanceAgreement.FromDate <= standardtariffList[i].ToDate)
                        {
                            StandardTariffID = standardtariffList[i].ID;
                            break;
                        }
                    }
                }
            }

            #endregion

            if (!StandardTariffID.HasValue)
                StandardTariffID = GetDefaultStandardTariffID(LoadTypeID);

            int? stt = StandardTariffID;
            if (!StandardTariffID.HasValue)
                standardtariffList = dcMaster.StandardTariffs.Where(P => P.LoadTypeID == LoadTypeID
                    && date >= P.FromDate && date <= P.ToDate && P.StatusID == 1).ToList();
            else
                standardtariffList = dcMaster.StandardTariffs.Where(P => P.ID == stt.Value).ToList();

            for (int i = 0; i < standardtariffList.Count; i++)
            {
                List<StandardTariffDetail> detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == FromStation && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                #region In The Standard Tariff From Station -> To Station

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == FromStation && detailList[j].DestinationStationID == ToStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region In The Standard Tariff To Station -> From Station

                detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == ToStation && P.DestinationStationID == FromStation && P.StatusID == 1).ToList();

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == ToStation && detailList[j].DestinationStationID == FromStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region If not found by station , station find by Western, Eastern, ...

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    int regionNEWS = 0;
                    regionNEWS = GetNEWS(FromStation);

                    if (regionNEWS != 0)
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                            && P.OriginStationID == regionNEWS && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if (detailList[j].OriginStationID == regionNEWS && detailList[j].DestinationStationID == ToStation)
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion


                #region HUB -> SUB || SUB -> HUB

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    if (IsHubToSub(FromStation, ToStation) || IsSubToHub(FromStation, ToStation))
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                            && ((P.OriginStationID == -1 && P.DestinationStationID == 0) ||
                            (P.OriginStationID == 0 && P.DestinationStationID == -1))).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if ((detailList[j].OriginStationID == -1 && detailList[j].DestinationStationID == 0) ||
                                (detailList[j].OriginStationID == 0 && detailList[j].DestinationStationID == -1))
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion

                #region HUB -> HUB

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    if (IsBothHub(FromStation, ToStation))
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                            && P.OriginStationID == -1 && P.DestinationStationID == -1).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if (detailList[j].OriginStationID == -1 && detailList[j].DestinationStationID == -1)
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion

                #region SUB -> SUB

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    if (IsBothSub(FromStation, ToStation))
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                            && P.OriginStationID == 0 && P.DestinationStationID == 0).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if (detailList[j].OriginStationID == 0 && detailList[j].DestinationStationID == 0)
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            if (!found)
                ShipmentValue = GetFromStandardTariff(LoadTypeID, FromStation, ToStation, Weight, MWeight);

            return ShipmentValue;
        }

        public double GetFromStandardTariff(int LoadTypeID, int FromStation, int ToStation, double Weight, double MWeight)
        {
            double ShipmentValue = 0;
            List<StandardTariff> standardtariffList;

            found = false;
            dcMaster = new MastersDataContext(connectionString);

            if (Weight > HWStarting && LoadTypeID == Express)
                LoadTypeID = HeavyWeight;

            int stID = GetDefaultStandardTariffID(LoadTypeID);
            if (stID <= 0)
                return 0;

            standardtariffList = dcMaster.StandardTariffs.Where(P => P.ID == stID).ToList();

            for (int i = 0; i < standardtariffList.Count; i++)
            {
                List<StandardTariffDetail> detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == FromStation && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                #region In The Standard Tariff From Station -> To Station

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == FromStation && detailList[j].DestinationStationID == ToStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region In The Standard Tariff To Station -> From Station

                detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == ToStation && P.DestinationStationID == FromStation && P.StatusID == 1).ToList();

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == ToStation && detailList[j].DestinationStationID == FromStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region If not found by station , station find by Western, Eastern, ...

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    int regionNEWS = 0;
                    regionNEWS = GetNEWS(FromStation);

                    if (regionNEWS != 0)
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                            && P.OriginStationID == regionNEWS && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if (detailList[j].OriginStationID == regionNEWS && detailList[j].DestinationStationID == ToStation)
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            return ShipmentValue;
        }

        private double GetFromStandardTariff(int StandardTariffID, int FromStation, int ToStation, double Weight, double MWeight, int LoadTypeID, AgreementRoute AgreeRoute)
        {
            double ShipmentValue = 0;
            standardtariffList = new List<StandardTariff>();
            found = false;
            dcMaster = new MastersDataContext(connectionString);

            List<StandardTariffDetail> detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                        && P.OriginStationID == FromStation && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

            #region In The Standard Tariff From Station -> To Station

            for (int j = 0; j < detailList.Count; j++)
            {
                if (detailList[j].OriginStationID == FromStation && detailList[j].DestinationStationID == ToStation)
                {
                    if (LoadTypeID == Express)
                    {
                        for (int k = 0; k < detailList.Count; k++)
                            if (detailList[k].RatePerUnit != 0)
                            {
                                j = k;
                                break;
                            }

                        if (detailList[j].RatePerUnit != 0)
                        {
                            ShipmentValue = ((Weight - MWeight) * detailList[j].RatePerUnit);// +detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                    }
                    else
                        //if (LoadTypeID == LTL)
                        //{
                        //    for (int k = 0; k < detailList.Count; k++)
                        //        if (detailList[k].RatePerUnit != 0)
                        //        {
                        //            j = k;
                        //            break;
                        //        }

                        //    if (detailList[j].RatePerUnit != 0)
                        //    {
                        //        ShipmentValue = ((Weight - MWeight) * detailList[j].RatePerUnit);// +detailList[j].BaseCharge;
                        //        StandardTariffDetailID = detailList[j].ID;
                        //        return ShipmentValue;
                        //    }
                        //}
                        //else
                        if ((Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit) || (AgreeRoute.MinWeight == 250 && detailList[j].ToUnit == 200))
                    {
                        found = true;
                        //StandardTariffID = StandardTariffID;
                        //GlobalVar.StandardTariffDetailID = detailList[i].ID;

                        if (LoadTypeID == HeavyWeight)
                        {
                            App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                            if (detailList[j].IsFixed == true)
                            {
                                ShipmentValue = detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                            else
                            {
                                if (j == 1 && currentStandardDetail.FromUnit == 250 && MWeight == 200)
                                {
                                    ShipmentValue = ((Weight - 200) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                                else
                                {
                                    ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                            }
                        }
                        else
                        {
                            App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                            if (detailList[j].IsFixed == true)
                            {
                                ShipmentValue = detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                            else
                            {
                                ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                        }
                    }
                }
            }

            #endregion

            #region In The Standard Tariff To Station -> From Station

            detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                       && P.OriginStationID == ToStation && P.DestinationStationID == FromStation && P.StatusID == 1).ToList();

            for (int j = 0; j < detailList.Count; j++)
            {
                if (detailList[j].OriginStationID == ToStation && detailList[j].DestinationStationID == FromStation)
                {
                    if (LoadTypeID == Express)
                    {
                        if (detailList[j].RatePerUnit != 0)
                        {
                            ShipmentValue = ((Weight - MWeight) * detailList[j].RatePerUnit);// +detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                    }
                    else
                        //if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        if ((Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit) || (AgreeRoute.MinWeight == 250 && detailList[j].ToUnit == 200))
                    {
                        found = true;
                        //StandardTariffID = StandardTariffID;
                        //GlobalVar.StandardTariffDetailID = detailList[i].ID;

                        if (LoadTypeID == HeavyWeight)
                        {
                            App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                            if (detailList[j].IsFixed == true)
                            {
                                ShipmentValue = detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                            else
                            {
                                if (j == 1 && currentStandardDetail.FromUnit == 250 && MWeight == 200)
                                {
                                    ShipmentValue = ((Weight - 200) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                                else
                                {
                                    ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                            }
                        }
                        else
                        {
                            App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                            if (detailList[j].IsFixed == true)
                            {
                                ShipmentValue = detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                            else
                            {
                                ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                StandardTariffDetailID = detailList[j].ID;
                                return ShipmentValue;
                            }
                        }
                    }
                }
            }

            #endregion

            #region If not found by station , station find by Western, Eastern, ...

            if (!found)
            {
                dcMaster = new MastersDataContext(connectionString);
                int regionNEWS = 0;
                regionNEWS = GetNEWS(FromStation);

                if (regionNEWS != 0)
                {
                    detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                        && P.OriginStationID == regionNEWS && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                    for (int j = 0; j < detailList.Count; j++)
                    {
                        if (detailList[j].OriginStationID == regionNEWS && detailList[j].DestinationStationID == ToStation)
                        {
                            if (LoadTypeID == Express)
                            {
                                if (detailList[j].RatePerUnit != 0)
                                {
                                    ShipmentValue = ((Weight - MWeight) * detailList[j].RatePerUnit);// +detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                            }
                            else
                                //if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                if ((Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit) || (AgreeRoute.MinWeight == 250 && detailList[j].ToUnit == 200))
                            {
                                found = true;
                                //StandardTariffID = StandardTariffID;
                                //GlobalVar.StandardTariffDetailID = detailList[i].ID;

                                if (LoadTypeID == HeavyWeight)
                                {
                                    App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                                    if (detailList[j].IsFixed == true)
                                    {
                                        ShipmentValue = detailList[j].BaseCharge;
                                        StandardTariffDetailID = detailList[j].ID;
                                        return ShipmentValue;
                                    }
                                    else
                                    {
                                        if (j == 1 && currentStandardDetail.FromUnit == 250 && MWeight == 200)
                                        {
                                            ShipmentValue = ((Weight - 200) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                            StandardTariffDetailID = detailList[j].ID;
                                            return ShipmentValue;
                                        }
                                        else
                                        {
                                            ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                            StandardTariffDetailID = detailList[j].ID;
                                            return ShipmentValue;
                                        }
                                    }
                                }
                                else
                                {
                                    App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                                    if (detailList[j].IsFixed == true)
                                    {
                                        ShipmentValue = detailList[j].BaseCharge;
                                        StandardTariffDetailID = detailList[j].ID;
                                        return ShipmentValue;
                                    }
                                    else
                                    {
                                        ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                        StandardTariffDetailID = detailList[j].ID;
                                        return ShipmentValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            if (!found)
                ShipmentValue = GetFromStandardTariff(LoadTypeID, FromStation, ToStation, Weight, MWeight);

            return ShipmentValue;
        }

        private double GetFromStandardTariff(int StandardTariffID, int FromStation, int ToStation, double Weight, double MWeight, int x)
        {
            double ShipmentValue = 0;
            standardtariffList = new List<StandardTariff>();
            found = false;
            dcMaster = new MastersDataContext(connectionString);

            standardtariffList = dcMaster.StandardTariffs.Where(P => P.ID == StandardTariffID).ToList();
            List<StandardTariffDetail> detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                       && P.OriginStationID == FromStation && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

            #region In The Standard Tariff From Station -> To Station

            for (int j = 0; j < detailList.Count; j++)
            {
                if (detailList[j].OriginStationID == FromStation && detailList[j].DestinationStationID == ToStation)
                {
                    if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                    {
                        found = true;
                        App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                        if (detailList[j].IsFixed == true)
                        {
                            ShipmentValue = detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                        else
                        {
                            ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                    }
                }
            }

            #endregion

            #region In The Standard Tariff To Station -> From Station

            detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                       && P.OriginStationID == ToStation && P.DestinationStationID == FromStation && P.StatusID == 1).ToList();

            for (int j = 0; j < detailList.Count; j++)
            {
                if (detailList[j].OriginStationID == ToStation && detailList[j].DestinationStationID == FromStation)
                {
                    if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                    {
                        found = true;
                        App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                        if (detailList[j].IsFixed == true)
                        {
                            ShipmentValue = detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                        else
                        {
                            ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                            StandardTariffDetailID = detailList[j].ID;
                            return ShipmentValue;
                        }
                    }
                }
            }

            #endregion

            #region If not found by station , station find by Western, Eastern, ...

            if (!found)
            {
                dcMaster = new MastersDataContext(connectionString);
                int regionNEWS = 0;
                regionNEWS = GetNEWS(FromStation);

                if (regionNEWS != 0)
                {
                    detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == StandardTariffID
                        && P.OriginStationID == regionNEWS && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                    for (int j = 0; j < detailList.Count; j++)
                    {
                        if (detailList[j].OriginStationID == regionNEWS && detailList[j].DestinationStationID == ToStation)
                        {
                            if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                            {
                                found = true;

                                App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                                if (detailList[j].IsFixed == true)
                                {
                                    ShipmentValue = detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                                else
                                {
                                    ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                                    StandardTariffDetailID = detailList[j].ID;
                                    return ShipmentValue;
                                }
                            }
                        }
                    }
                }
            }

            #endregion


            #region HUB -> SUB || SUB -> HUB

            if (!found)
            {
                dcMaster = new MastersDataContext(connectionString);
                if (IsHubToSub(FromStation, ToStation) || IsSubToHub(FromStation, ToStation))
                {
                    detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                        && ((P.OriginStationID == -1 && P.DestinationStationID == 0) ||
                        (P.OriginStationID == 0 && P.DestinationStationID == -1))).ToList();

                    for (int j = 0; j < detailList.Count; j++)
                    {
                        if ((detailList[j].OriginStationID == -1 && detailList[j].DestinationStationID == 0) ||
                            (detailList[j].OriginStationID == 0 && detailList[j].DestinationStationID == -1))
                        {
                            if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                            {
                                ShipmentValue = GetShipValue(0, dcMaster.StandardTariffs.First(P => P.ID == StandardTariffID).LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                break;
                            }
                        }
                    }
                }
            }

            #endregion

            #region HUB -> HUB

            if (!found)
            {
                dcMaster = new MastersDataContext(connectionString);
                if (IsBothHub(FromStation, ToStation))
                {
                    detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                        && P.OriginStationID == -1 && P.DestinationStationID == -1).ToList();

                    for (int j = 0; j < detailList.Count; j++)
                    {
                        if (detailList[j].OriginStationID == -1 && detailList[j].DestinationStationID == -1)
                        {
                            if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                            {
                                ShipmentValue = GetShipValue(0, dcMaster.StandardTariffs.First(P => P.ID == StandardTariffID).LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                break;
                            }
                        }
                    }
                }
            }

            #endregion

            #region SUB -> SUB

            if (!found)
            {
                dcMaster = new MastersDataContext(connectionString);
                if (IsBothSub(FromStation, ToStation))
                {
                    detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID && P.StatusID == 1
                        && P.OriginStationID == 0 && P.DestinationStationID == 0).ToList();

                    for (int j = 0; j < detailList.Count; j++)
                    {
                        if (detailList[j].OriginStationID == 0 && detailList[j].DestinationStationID == 0)
                        {
                            if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                            {
                                ShipmentValue = GetShipValue(0, dcMaster.StandardTariffs.First(P => P.ID == StandardTariffID).LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                break;
                            }
                        }
                    }
                }
            }

            #endregion
            if (!found)
                ShipmentValue = GetFromStandardTariff(dcMaster.StandardTariffs.First(P => P.ID == StandardTariffID).LoadTypeID, FromStation, ToStation, Weight, MWeight);

            return ShipmentValue;
        }

        private double GetFromStandardTariff(AgreementRoute CurrentRoute, int FromStation, int ToStation, int LoadTypeID, double Weight, DateTime date, int HWStating)
        {
            double ShipmentValue = 0;
            dcMaster = new MastersDataContext(connectionString);
            App_Data.AgreementsDataContext dcAgreement = new AgreementsDataContext(connectionString);
            App_Data.Agreement instanceAgreement = dcAgreement.Agreements.First(P => P.ID == CurrentRoute.AgreementID);

            int? StandardTariffID = null;
            if (instanceAgreement.StandardTariffID.HasValue)
                StandardTariffID = instanceAgreement.StandardTariffID.Value;

            if (StandardTariffID.HasValue)
                ShipmentValue += GetFromStandardTariff(StandardTariffID.Value, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
            else
                ShipmentValue += GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, instanceAgreement.ID);

            return ShipmentValue;
        }

        public double GetShipValue(int i, int LoadTypeID, List<StandardTariffDetail> detailList, double Weight, double MWeight, int j, double ShipValue)
        {
            double ShipmentValue = ShipValue;

            found = true;
            StandardTariffID = standardtariffList[i].ID;

            if (LoadTypeID == HeavyWeight)
            {
                App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                if (detailList[j].IsFixed == true)
                {
                    ShipmentValue = detailList[j].BaseCharge;
                    StandardTariffDetailID = detailList[j].ID;
                    return ShipmentValue;
                }
                else
                {
                    if (j == 1 && currentStandardDetail.FromUnit == 250 && MWeight == 200)
                    {
                        ShipmentValue = ((Weight - 200) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                        StandardTariffDetailID = detailList[j].ID;
                        return ShipmentValue;
                    }
                    else
                    {
                        ShipmentValue = ((Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit) + detailList[j].BaseCharge;
                        StandardTariffDetailID = detailList[j].ID;
                        return ShipmentValue;
                    }
                }
            }
            else
            {
                App_Data.StandardTariffDetail currentStandardDetail = detailList[j];
                if (detailList[j].IsFixed == true)
                {
                    ShipmentValue = detailList[j].BaseCharge;
                    StandardTariffDetailID = detailList[j].ID;
                    return ShipmentValue;
                }
                else
                    if (currentStandardDetail.ForEachFraction == 1)
                {
                    double v = (Weight - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit;
                    ShipmentValue = v + detailList[j].BaseCharge;
                    StandardTariffDetailID = detailList[j].ID;
                    return ShipmentValue;
                }
                else
                {
                    double v = 0;//(((Weight / currentStandardDetail.ForEachFraction) - currentStandardDetail.FromUnit) * detailList[j].RatePerUnit);
                    v = ((Weight - currentStandardDetail.FromUnit) / currentStandardDetail.ForEachFraction) * detailList[j].RatePerUnit;
                    ShipmentValue = v + detailList[j].BaseCharge;
                    StandardTariffDetailID = detailList[j].ID;
                    return ShipmentValue;
                }
            }

            //return ShipmentValue;
        }

        public double GetFromStandardTariff(int stID, int LoadTypeID, int FromStation, int ToStation, double Weight, double MWeight)
        {
            double ShipmentValue = 0;
            List<StandardTariff> standardtariffList;

            found = false;
            dcMaster = new MastersDataContext(connectionString);

            if (Weight > HWStarting && LoadTypeID == Express)
                LoadTypeID = HeavyWeight;

            //int stID = GetDefaultStandardTariffID(LoadTypeID);
            if (stID <= 0)
                return 0;

            standardtariffList = dcMaster.StandardTariffs.Where(P => P.ID == stID).ToList();

            for (int i = 0; i < standardtariffList.Count; i++)
            {
                List<StandardTariffDetail> detailList =
                        dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == FromStation && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                #region In The Standard Tariff From Station -> To Station

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == FromStation && detailList[j].DestinationStationID == ToStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region In The Standard Tariff To Station -> From Station

                detailList = dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                        && P.OriginStationID == ToStation && P.DestinationStationID == FromStation && P.StatusID == 1).ToList();

                for (int j = 0; j < detailList.Count; j++)
                {
                    if (detailList[j].OriginStationID == ToStation && detailList[j].DestinationStationID == FromStation)
                    {
                        if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                        {
                            ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                            break;
                        }
                    }
                }

                #endregion

                #region If not found by station , station find by Western, Eastern, ...

                if (!found)
                {
                    dcMaster = new MastersDataContext(connectionString);
                    int regionNEWS = 0;
                    regionNEWS = GetNEWS(FromStation);

                    if (regionNEWS != 0)
                    {
                        detailList =
                            dcMaster.StandardTariffDetails.Where(P => P.StandardTariffID == standardtariffList[standardtariffList.Count - 1].ID
                            && P.OriginStationID == regionNEWS && P.DestinationStationID == ToStation && P.StatusID == 1).ToList();

                        for (int j = 0; j < detailList.Count; j++)
                        {
                            if (detailList[j].OriginStationID == regionNEWS && detailList[j].DestinationStationID == ToStation)
                            {
                                if (Weight >= detailList[j].FromUnit && Weight <= detailList[j].ToUnit)
                                {
                                    ShipmentValue = GetShipValue(i, LoadTypeID, detailList, Weight, MWeight, j, ShipmentValue);
                                    break;
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            return ShipmentValue;
        }

        #endregion


        #region Pharma Changes

        public double GetTarifTemperatureAmount(int TemperatureId, int FromStation, int ToStation, int ClientID, DateTime dtDate)
        {
            double ShipmentValue = 0;
            dcMaster = new MastersDataContext(connectionString);
            dtDate = dtDate.Date;
            TariffTemperatureDetail tariffTemperatureDetail = dcMaster.TariffTemperatureDetails.Where
                (T => T.OriginStationID == FromStation && T.DestinationStationID == ToStation && T.StatusID == 1 &&
                      T.TariffTemperatureID == TemperatureId && T.ClientID == ClientID && Convert.ToDateTime(dtDate) >= T.FromDate
                        && Convert.ToDateTime(dtDate) <= T.ToDate)
                .FirstOrDefault();
            if (tariffTemperatureDetail != null)
            {
                ShipmentValue = tariffTemperatureDetail.Amount;
            }
            else
            {
                //Apply Standard Tariff 
                TariffTemperatureDetail tariffTemperatureDetailStandardtariff = dcMaster.TariffTemperatureDetails.Where
                (T => T.OriginStationID == FromStation && T.DestinationStationID == ToStation && T.StatusID == 1 &&
                      T.TariffTemperatureID == TemperatureId && T.ClientID == null).FirstOrDefault();
                if (tariffTemperatureDetailStandardtariff != null)
                {
                    ShipmentValue = tariffTemperatureDetailStandardtariff.Amount;
                }
            }

            return ShipmentValue;
        }

        public double GetTarifTemperatureAmountforKgs(int TemperatureId, int FromStation, int ToStation, int ClientID, DateTime dtDate, double weight)
        {
            double ShipmentValue = 0;
            dcMaster = new MastersDataContext(connectionString);
            dtDate = dtDate.Date;
            TariffTemperatureDetail tariffTemperatureDetailAllRoutes = dcMaster.TariffTemperatureDetails.Where
      (T => T.StatusID == 1 &&
            T.TariffTemperatureID == TemperatureId && T.ClientID == ClientID && Convert.ToDateTime(dtDate) >= T.FromDate
              && Convert.ToDateTime(dtDate) <= T.ToDate)
      .FirstOrDefault();

            if (tariffTemperatureDetailAllRoutes != null)
            {
                if (tariffTemperatureDetailAllRoutes.OriginStationID == -2 && tariffTemperatureDetailAllRoutes.DestinationStationID == -2)
                {
                    if (weight > tariffTemperatureDetailAllRoutes.MinWeight)
                    {
                        TariffTemperatureSlab tariffTemperatureslab = dcMaster.TariffTemperatureSlabs.Where
                (T => T.TariffTemperatureDetailID == tariffTemperatureDetailAllRoutes.ID && T.StatusID == 1 &&
                      weight >= T.FromWeight && weight <= T.ToWeight)
                .FirstOrDefault();

                        ShipmentValue = Convert.ToDouble(tariffTemperatureDetailAllRoutes.Amount) + (weight - (Convert.ToDouble(tariffTemperatureDetailAllRoutes.MinWeight))) * Convert.ToDouble(tariffTemperatureslab.Amount);
                    }
                    else
                    {
                        if (tariffTemperatureDetailAllRoutes != null)
                        {
                            ShipmentValue = tariffTemperatureDetailAllRoutes.Amount;
                        }
                    }
                }
                else
                {
                    TariffTemperatureDetail tariffTemperatureDetailOrgDest = dcMaster.TariffTemperatureDetails.Where
                (T => T.OriginStationID == FromStation && T.DestinationStationID == ToStation && T.StatusID == 1 &&
                      T.TariffTemperatureID == TemperatureId && T.ClientID == ClientID && Convert.ToDateTime(dtDate) >= T.FromDate
                        && Convert.ToDateTime(dtDate) <= T.ToDate)
                .FirstOrDefault();
                    if (tariffTemperatureDetailOrgDest != null)
                    {
                        if (weight > tariffTemperatureDetailOrgDest.MinWeight)
                        {
                            TariffTemperatureSlab tariffTemperatureslab = dcMaster.TariffTemperatureSlabs.Where
                    (T => T.TariffTemperatureDetailID == tariffTemperatureDetailOrgDest.ID && T.StatusID == 1 &&
                          weight >= T.FromWeight && weight <= T.ToWeight)
                    .FirstOrDefault();

                            ShipmentValue = Convert.ToDouble(tariffTemperatureDetailOrgDest.Amount) + (weight - (Convert.ToDouble(tariffTemperatureDetailOrgDest.MinWeight))) * Convert.ToDouble(tariffTemperatureslab.Amount);
                        }
                        else
                        {
                            if (tariffTemperatureDetailOrgDest != null)
                            {
                                ShipmentValue = tariffTemperatureDetailOrgDest.Amount;
                            }
                        }
                    }
                }

            }
            return ShipmentValue;
            #endregion
            #endregion
        }

        public double GetPharmaShipmentValue(int ClientID, int LoadTypeID, int SubLoadTypeID, DateTime date, int FromStation, int ToStation, double Weight, bool IsNeedFraction, int hwstarting
            , int ShipperID, int BillingTypeID, bool IsRTO, int TemperatureID)
        {
            dcMaster = new MastersDataContext();
            int ServiceTypeIDs = Convert.ToInt32(dcMaster.LoadTypes.First(C => C.ID == LoadTypeID).ServiceTypeID);
            //if (!dcMaster.ServiceTypes.First(P => P.ID == ServiceTypeIDs).HasFraction)
            //   Weight = System.Math.Round(Weight, 0);
            date = new DateTime(date.Year, date.Month, date.Day);
            HWStarting = hwstarting;
            FromExpress = false;
            FoundExpressSlab = false;
            FoundHWSlab = false;
            UseHW = false;

            if (LoadTypeID == 2)
                UseHW = true;

            find = false;
            AgreementID = null;
            AgreementRouteID = null;
            AgreementSlabID = null;
            StandardTariffID = null;
            StandardTariffDetailID = null;
            HeavyWeightValue = 0;
            ExpressValue = 0;
            //dic = new Dictionary<string, string>();

            double ShipmentValue = 0;
            ShipmentValue = GetPharmaShipmentValues(ClientID, LoadTypeID, SubLoadTypeID, date, FromStation, ToStation, Weight, IsNeedFraction, false, ShipperID, BillingTypeID, IsRTO, TemperatureID);
            IsNeedFraction = true;

            if (IsNeedFraction)
                return Math.Round(ShipmentValue, 2);
            else
                return Math.Round(ShipmentValue, 0, MidpointRounding.AwayFromZero);
        }
        private double GetPharmaShipmentValues(int ClientID, int LoadTypeID, int SubLoadTypeID, DateTime date, int FromStation, int ToStation, double Weight, bool IsNeedFraction, bool UseExpress
           , int ShipperID, int BillingTypeID, bool IsRTO, int TemperatureID)
        {
            double ShipmentValue = 0;
            FromAgreement = false;
            dcMaster = new MastersDataContext(connectionString);

            MastersDataContext dcAgreement = new MastersDataContext(connectionString);
            List<TariffTemperature> agreementList = new List<TariffTemperature>();

            //agreementList = dcAgreement.Agreements.Where(P => P.ClientID == ClientID
            //            && P.LoadTypeID == LoadTypeID
            //            && date >= P.FromDate
            //            && date <= P.ToDate
            //            && P.IsTerminated == false
            //            && P.IsCancelled == false
            //            && P.StatusID == 1).ToList();

            if (TemperatureID > 0)
            {
                agreementList = dcMaster.TariffTemperatures.Where(T => T.SubLoadTypeID == SubLoadTypeID && T.StatusID == 1 && T.ID == TemperatureID).ToList();
            }
            else
            {
                agreementList = dcMaster.TariffTemperatures.Where(T => T.SubLoadTypeID == SubLoadTypeID && T.StatusID == 1 && T.Temperature == "0").ToList();
            }

            //TariffTemperatureDetail tariffTemperatureDetail = dcMaster.TariffTemperatureDetails.Where
            //    (T => T.OriginStationID == FromStation && T.DestinationStationID == ToStation && T.StatusID == 1 &&
            //          T.TariffTemperatureID == TemperatureId && T.ClientID == ClientID && Convert.ToDateTime(dtDate) >= T.FromDate
            //            && Convert.ToDateTime(dtDate) <= T.ToDate)
            //    .FirstOrDefault();

            if (agreementList.Count > 0)
            {
                find = false;
                int FromNEWS = 0, ToNEWS = 0;
                FromNEWS = GetNEWS(FromStation);
                ToNEWS = GetNEWS(ToStation);
                List<int> AlreadyCheckedRouteList;

                for (int i = 0; i < agreementList.Count; i++)
                {
                    if (find)
                        break;

                    AlreadyCheckedRouteList = new List<int>();
                    List<TariffTemperatureDetail> routeList = dcAgreement.TariffTemperatureDetails.Where(P =>
                    P.TariffTemperatureID == agreementList[i].ID && P.ClientID == ClientID
                                   && date >= P.FromDate
                       && date <= P.ToDate && P.StatusID == 1).ToList();
                    bool HasFraction = false;
                    try
                    {
                        //if (dcMaster.ViwLoadTypes.First(P => P.ID == LoadTypeID).HasFraction.Value)
                        HasFraction = true;
                    }
                    catch { }
                    int? currentroutes = null;
                    currentroutes = GetPharmaAgreementRoute(routeList, FromStation, ToStation, FromNEWS, ToNEWS, AlreadyCheckedRouteList);
                    if (currentroutes.HasValue)
                    {
                        AlreadyCheckedRouteList.Add(currentroutes.Value);
                        ShipmentValue = GetPharmaShipment(Weight, routeList[currentroutes.Value], FromStation, ToStation, date, dcAgreement, LoadTypeID, agreementList[i], HasFraction);
                        if (ShipmentValue > 0)
                        {
                            find = true;
                            break;
                        }
                        else
                            continue;
                    }
                    //else
                    //    if (agreementList[i].StandardTariffID.HasValue)
                    //{
                    //    ShipmentValue = GetFromStandardTariff(agreementList[i].StandardTariffID.Value, FromStation, ToStation, Weight, 0, agreementList[i].LoadTypeID);
                    //    if (ShipmentValue > 0)
                    //    {
                    //        find = true;
                    //        AgreementID = agreementList[i].ID;
                    //        break;
                    //    }
                    //    else
                    //        continue;
                    //}
                    //else
                    //{
                    //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
                    //    if (ShipmentValue > 0)
                    //    {
                    //        find = true;
                    //        break;
                    //    }
                    //    else
                    //        continue;
                    //}
                }
            }
            //else
            //{
            //    if (LoadTypeID == HeavyWeight)
            //        ShipmentValue = GetShipmentValues(ClientID, Express, date, FromStation, ToStation, Weight, IsNeedFraction, true, ShipperID, BillingTypeID, IsRTO);
            //    else
            //        //if (LoadTypeID == Pallet || LoadTypeID == LTL)
            //        //{
            //        //    ShipmentValue = 0;
            //        //    find = true;
            //        //}
            //        if (LoadTypeID == Pallet)
            //    {
            //        ShipmentValue = 0;
            //        find = true;
            //    }
            //    //else
            //    //        if (LoadTypeID == LTL)
            //    //{
            //    //    ShipmentValue = GetFromStandardTariff(date, LTL, FromStation, ToStation, Weight, 0, null);
            //    //    //ShipmentValue = 0;
            //    //    find = true;
            //    //}
            //    //else
            //    //            if (!find)
            //    //{
            //    //    if (Weight >= HWStarting && LoadTypeID == Express && !UseExpress)
            //    //        ShipmentValue = GetShipmentValue(ClientID, HeavyWeight, date, FromStation, ToStation, Weight, IsNeedFraction, HWStarting, ShipperID, BillingTypeID, IsRTO);
            //    //    else
            //    //        ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
            //    //}
            //    //else
            //    //                if (find)
            //    //{
            //    //    if (Weight >= HWStarting && (LoadTypeID == Express) && ShipmentValue == 0 && !UseExpress)
            //    //        ShipmentValue = GetShipmentValue(ClientID, HeavyWeight, date, FromStation, ToStation, Weight, IsNeedFraction, HWStarting, ShipperID, BillingTypeID, IsRTO);
            //    //}
            //}

            //if (!find)
            //{
            //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null);
            //    if (ShipmentValue > 0)
            //        find = true;
            //}

            //if ((LoadTypeID == HeavyWeight || LoadTypeID == Express) && Weight >= HWStarting && !UseExpress)
            //{
            //    if (LoadTypeID == HeavyWeight)
            //        HeavyWeightValue = ShipmentValue;

            //    FromExpress = false;
            //    ExpressValue = GetShipmentValues(ClientID, Express, date, FromStation, ToStation, Weight, IsNeedFraction, true, ShipperID, BillingTypeID, IsRTO);

            //    if (FoundExpressSlab && !UseHW && !FoundHWSlab)
            //        ShipmentValue = ExpressValue;
            //    else
            //        if (HeavyWeightValue < ExpressValue && HeavyWeightValue > 0)
            //        ShipmentValue = HeavyWeightValue;
            //    else
            //            if (ExpressValue > 0 && FromExpress)
            //        ShipmentValue = ExpressValue;
            //}

            //if (ShipmentValue == 0)
            //{
            //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, null, HWStarting);
            //}

            return ShipmentValue;
        }
        private int? GetPharmaAgreementRoute(List<TariffTemperatureDetail> routeList, int FromStation, int ToStation, int FromNEWS, int ToNEWS, List<int> AvailbleRouteList)
        {
            int? route = null;

            #region Conditions

            #region From Specific Station to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == ToStation && routeList[j].DestinationStationID == FromStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Sub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == 0 && !IsStationHub(ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == 0 && !IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation)
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Hub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == -1 && IsStationHub(ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == -1 && IsStationHub(FromStation) && (routeList[j].OriginStationID == ToStation))
                {
                    if (AlreadyChecked(AvailbleRouteList, j))
                        continue;
                    else
                        return j;
                }

            #endregion

            #region From Specific Station to (Hub Station)

            if (IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                {
                    bool same = FromStation == ToStation;
                    if (!same)
                    {
                        if (routeList[j].OriginStationID != routeList[j].DestinationStationID)
                        {
                            if ((routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation)) ||
                            (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation))
                                return j;
                        }
                    }
                    else
                    {
                        if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation))
                            return j;
                    }
                }

            if (IsStationHub(FromStation))
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (find)
                        break;

                    bool same = FromStation == ToStation;
                    if (!same)
                    {
                        if (routeList[j].OriginStationID != routeList[j].DestinationStationID)
                        {
                            if ((routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsStationHub(ToStation)) ||
                            (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation))
                                return j;
                        }
                    }
                    else
                    {
                        if (routeList[j].DestinationStationID == ToStation && IsStationHub(FromStation) && routeList[j].OriginStationID == ToStation)
                            return j;
                    }
                }
            }

            #endregion

            #region From Specific Station to (Hub - Western , Eastern , Central)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToNEWS)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromNEWS && routeList[j].OriginStationID == ToStation)
                    return j;

            #endregion

            #region From Specific Station To (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == -2)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == ToStation)
                    return j;

            #endregion

            #region Fom (Hub) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -1 && IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && IsStationHub(ToStation) && routeList[j].OriginStationID == -1)
                    return j;

            #endregion

            #region Fom (Hub Station) to Specific Station

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                        return j;

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToStation && IsStationHub(ToStation))
                        return j;

            #endregion

            #region Fom (Hub - Western , Eastern , Central ) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == FromNEWS && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToNEWS)
                    return j;

            #endregion

            #region Fom (Sub) to Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == 0 && !IsStationHub(FromStation) && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == 0 && !IsStationHub(ToStation))
                    return j;

            #endregion

            #region From All Saudia Truck To Specific Station

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -2 && routeList[j].DestinationStationID == ToStation)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == -2)
                    return j;

            #endregion

            #region From (Hub) To (Hub)

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == -1)
                        return j;

            #endregion

            #region From (Hu Stationb) To (Hub Station)

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation)
                        return j;

            if (IsBothHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == ToStation && routeList[j].DestinationStationID == FromStation)
                        return j;

            #endregion

            #region From (Sub) To (Hub)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == 0 && !IsStationHub(FromStation) && (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA)))
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if (((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) || routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA)) && routeList[j].OriginStationID == 0))
                    return j;

            #endregion

            #region From (Hub) To (Sub)

            if (IsHubToSub(FromStation, ToStation) || IsHubToSub(ToStation, FromStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0 && IsHubToSub(FromStation, ToStation))
                        return j;

            if (IsHubToSub(FromStation, ToStation) || IsHubToSub(ToStation, FromStation))
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (find)
                        break;
                    if ((routeList[j].DestinationStationID == 0 && routeList[j].OriginStationID == -1 && IsHubToSub(ToStation, FromStation)))
                    {
                        bool f = false;
                        for (int n = 0; n < routeList.Count; n++)
                        {
                            if (routeList[n].DestinationStationID == 0 && routeList[n].OriginStationID == -1)
                            {
                                f = true;
                                break;
                            }
                        }
                        if (f) break;

                        return j;
                    }
                }
            }

            #endregion

            #region From (Sub) To (Hub)
            //notes
            if (IsSubToHub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == 0 && routeList[j].DestinationStationID == -1)
                        return j;

            //if (IsSubToHub(ToStation, FromStation))
            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == 0 && IsStationHub(ToStation) && routeList[j].OriginStationID == -1 && !IsStationHub(FromStation)))
                    return j;

            #endregion

            #region From (Sub) To (Sub)

            if (IsBothSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == 0 && routeList[j].DestinationStationID == 0)
                        return j;

            #endregion

            #region From (Hub) to (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].OriginStationID == -1 && IsStationHub(FromStation) && routeList[j].DestinationStationID == -2)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == -1 && IsStationHub(ToStation)))
                    return j;

            #endregion

            #region From (Hub Station) to (All Saudia Truck)

            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && IsStationHub(FromStation) && routeList[j].DestinationStationID == -2)
                        return j;


            if (IsStationHub(FromStation) || IsStationHub(ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if ((routeList[j].DestinationStationID == -2 && routeList[j].OriginStationID == ToStation && IsStationHub(ToStation)))
                        return j;

            #endregion

            #region From (Hub - Western , Eastern , Central) to (All Saudia Truck)

            for (int j = 0; j < routeList.Count; j++)
                if (routeList[j].DestinationStationID == FromNEWS && routeList[j].DestinationStationID == 0)
                    return j;

            for (int j = 0; j < routeList.Count; j++)
                if ((routeList[j].DestinationStationID == 0 && routeList[j].DestinationStationID == ToNEWS))
                    return j;

            #endregion



            #region From (Sub Station) To (Hub Station)

            if (IsSubToHub(FromStation, ToStation) || IsHubToSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if (routeList[j].OriginStationID == FromStation && routeList[j].DestinationStationID == ToStation && IsSubToHub(FromStation, ToStation))
                        return j;

            if (IsSubToHub(FromStation, ToStation) || IsHubToSub(FromStation, ToStation))
                for (int j = 0; j < routeList.Count; j++)
                    if ((routeList[j].DestinationStationID == FromStation && routeList[j].OriginStationID == ToStation && IsHubToSub(FromStation, ToStation)))
                        return j;

            #endregion

            #region From Sub to All Saudia Truck
            //if (!find)
            //{
            //    if (!IsStationHub(FromStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region From All Saudia Truck To Hub
            //if (!find)
            //{
            //    if (IsStationHub(ToStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            if (routeList[j].DestinationStationID == -1)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region From All Saudia Truck To Sub
            //if (!find)
            //{
            //    if (!IsStationHub(ToStation))
            //    {
            //        for (int j = 0; j < routeList.Count; j++)
            //        {
            //            if (find)
            //                break;
            //            //if (routeList[j].OriginStationID == -1 && routeList[j].DestinationStationID == 0)
            //            if (routeList[j].DestinationStationID == 0)
            //            {
            //                ShipmentValue = GetShipment(Weight, routeList[j], FromStation, ToStation, date, dcAgreement, i, j, LoadTypeID, agreementList[agreementList.Count - 1], HasFraction, ref AgreementID, ref RouteID, HeavyWeight);
            //                if (ShipmentValue > 0)
            //                    find = true;
            //                //else
            //                //{
            //                //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight);
            //                //    find = true;
            //                //}
            //                break;
            //            }
            //        }
            //    }
            //}
            #endregion

            #region Centeral , Western , Eastern

            //From Centeral To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Eastern To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Western To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //From Centeral To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //---------From Centeral To Eastern Vis Vers
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Easter To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //-----------From Easter To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Centeral To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //-----------From Centeral To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //From Western To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
                    return j;
            }

            //-----------From Western To Centeral
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //From Eastern To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            //----------From Eastern To Western
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //From Western To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
                    return j;
            }

            //-------From Western To Eastern
            for (int j = 0; j < routeList.Count; j++)
            {
                if (routeList[j].OriginStationID == Convert.ToInt32(RegionNEWS.JED) &&
                    routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
                    FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
                    return j;
            }

            #endregion

            //#region Centeral , Western , Eastern

            ////From Centeral To Centeral
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
            //        return j;
            //}

            ////From Eastern To Eastern
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Western To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Centeral To Eastern
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Eastern To Western
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) && ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}






            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) && ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.JED))
            //        return j;
            //}

            ////From Centeral To Eastern Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if ((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.RUH)))
            //        return j;
            //}

            ////From Centeral To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.RUH) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Centeral To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.RUH) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.RUH))
            //        return j;
            //}

            ////From Eastern To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if (routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.JED) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.DHA))
            //        return j;
            //}

            ////From Eastern To Western Vise Vers
            //for (int j = 0; j < routeList.Count; j++)
            //{
            //    if ((routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.DHA) &&
            //        FromNEWS == Convert.ToInt32(RegionNEWS.DHA) &&
            //        routeList[j].DestinationStationID == Convert.ToInt32(RegionNEWS.JED) &&
            //        ToNEWS == Convert.ToInt32(RegionNEWS.JED)))
            //        return j;
            //}

            //#endregion

            #region From All Saudia Truck To All Saudia Truck

            for (int w = 0; w < routeList.Count; w++)
            {
                for (int j = 0; j < routeList.Count; j++)
                {
                    if (routeList[j].OriginStationID == -2 && routeList[j].DestinationStationID == -2)
                    {
                        if (AlreadyChecked(AvailbleRouteList, j))
                            continue;
                        else
                            return j;
                    }
                }
            }

            #endregion

            #endregion

            return route;
        }
        private double GetPharmaShipment(double Weight, TariffTemperatureDetail CurrentRoute, int FromStation, int ToStation, DateTime date,
                                   MastersDataContext dcAgreement, int LoadTypeID, TariffTemperature CurrentAgreement, bool HasFraction)
        {
            double ShipmentValue = 0;
            find = false;

            if (LoadTypeID == AviationKG)
            {

                double _minWt = 0, _tariffAmount = 0, _phmShipmentValue = 0;
                if (CurrentRoute.MinWeight != null && CurrentRoute.MinWeight != 0)
                    _minWt = Convert.ToDouble(CurrentRoute.MinWeight);
                else
                {
                    _tariffAmount = CurrentRoute.Amount * Weight;
                    _phmShipmentValue = (_tariffAmount);
                }


                if (_minWt > 0)
                    if (Weight > _minWt)
                    {
                        List<TariffTemperatureSlab> slabList = dcAgreement.TariffTemperatureSlabs.Where(P => P.TariffTemperatureDetailID == CurrentRoute.ID && P.StatusID == 1 && Weight >= P.FromWeight && Weight <= P.ToWeight).ToList();

                        if (slabList.Count > 0)
                        {
                            if (slabList[0].CumulativeID == 1)
                            {
                                _tariffAmount = slabList[0].Amount;
                                _phmShipmentValue = _tariffAmount;
                            }
                        }
                        else
                        {
                            _tariffAmount = CurrentRoute.Amount;
                            _phmShipmentValue = (_tariffAmount);
                        }

                    }
                    else
                    {
                        _tariffAmount = CurrentRoute.Amount;
                        _phmShipmentValue = (_tariffAmount);
                    }

                return _phmShipmentValue;
            }



            //Get Value Form Current Route
            if (Weight <= CurrentRoute.MinWeight)
            {
                if (LoadTypeID != HeavyWeight && LoadTypeID != LTL)
                {
                    ShipmentValue = CurrentRoute.Amount;
                    find = true;
                    FromExpress = true;
                    AgreementID = CurrentAgreement.ID;
                    AgreementRouteID = CurrentRoute.ID;
                    //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                    return ShipmentValue;
                }
                else
                {
                    //if (CurrentAgreement.StandardTariffID != null)
                    //{
                    //    ShipmentValue = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                    //    double discount = 0;
                    //    discount = ShipmentValue * (CurrentRoute.MinCharge / 100);
                    //    ShipmentValue = ShipmentValue - discount;
                    //    find = true;
                    //    FromExpress = true;
                    //    AgreementID = CurrentAgreement.ID;
                    //    AgreementRouteID = CurrentRoute.ID;
                    //    //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                    //    return ShipmentValue;
                    //}
                    //else
                    //{
                    //    ShipmentValue = GetFromStandardTariff(date, LoadTypeID, FromStation, ToStation, Weight, 0, agreementid);// CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                    //    double discount = 0;
                    //    discount = ShipmentValue * (CurrentRoute.MinCharge / 100);
                    //    ShipmentValue = ShipmentValue - discount;
                    //    find = true;
                    //    FromExpress = true;
                    //    AgreementID = CurrentAgreement.ID;
                    //    AgreementRouteID = CurrentRoute.ID;
                    //    //AddData(SName.AgreementRoute, AgreementRouteID.Value, ShipmentValue);
                    //    return ShipmentValue;
                    //}
                }
            }
            else
            {
                // Else Find Value From Slabs
                List<TariffTemperatureSlab> slabList = dcAgreement.TariffTemperatureSlabs.Where(P => P.TariffTemperatureDetailID == CurrentRoute.ID && P.StatusID == 1).ToList();
                ShipmentValue = CheckInPharmaSlabs(Weight, slabList, ShipmentValue, CurrentRoute, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }
        private double CheckInPharmaSlabs(double Weight, List<TariffTemperatureSlab> slabList, double ShipmentValue,
                                   TariffTemperatureDetail CurrentRoute, TariffTemperature CurrentAgreement, MastersDataContext dcAgreement,
                                  int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double Results = ShipmentValue;
            find = false;
            for (int r = 0; r < slabList.Count; r++)
            {
                if (find)
                    break;

                //If Current Slab is suitable depend on weight > from weight and < to weight.
                if (Weight >= slabList[r].FromWeight && Weight <= slabList[r].ToWeight)
                {
                    if (LoadTypeID == 1)
                        FoundExpressSlab = true;
                    else
                        if (LoadTypeID == 2)
                        FoundHWSlab = true;
                    // If Current Slab Charge Type FOC ( Free Of Charge ).
                    if (slabList[r].ChargeTypeID == 4)
                    {
                        // If Current Slab Charge Type (FOC) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            Results = 0;
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            //AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (FOC) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            //List<AgreementSlab> slabListDetail = dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                            //            && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetPharmaFOCShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID.Value, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                        // If Current Slab Charge Type Fixed.
                        if (slabList[r].ChargeTypeID == 3)
                    {
                        // If Current Slab Charge Type (Fixed) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            Results = slabList[r].Amount;
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            //AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (Fixed) and Cumulative ( From Previous, From Beinning , Slab Option ).
                        {
                            //List<AgreementSlab> slabListDetail = dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                            //            && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetPharmaFixedShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID.Value, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                            // If Current Slab Charge Type %.
                            if (slabList[r].ChargeTypeID == 2)
                    {
                        // If Current Slab Charge Type (%) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            ////If Standard Taiff in the Top Specified
                            //if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                            //{
                            //    Results = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                            //    double val = Results * (slabList[r].Amount / 100);
                            //    Results = Results - val;
                            //    find = true;
                            //    FromExpress = true;
                            //    AgreementID = CurrentAgreement.ID;
                            //    AgreementRouteID = CurrentRoute.ID;
                            //    AgreementSlabID = slabList[r].ID;
                            //    break;
                            //}
                            //else
                            //{
                            //    //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                            //    standardtariffList = new List<StandardTariff>();
                            //    standardtariffList = dcMaster.StandardTariffs.Where(P => P.LoadTypeID == LoadTypeID
                            //                && CurrentAgreement.FromDate >= P.FromDate && CurrentAgreement.FromDate <= P.ToDate && P.StatusID == 1).ToList();
                            //    if (standardtariffList.Count > 0)
                            //        Results = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight, 0, LoadTypeID, CurrentRoute);
                            //    else
                            //        Results = GetFromStandardTariff(CurrentRoute, FromStation, ToStation, LoadTypeID, Weight, date, HWStarting);
                            //    double val = Results * (slabList[r].Amount / 100);
                            //    Results = Results - val;
                            //    find = true;
                            //    FromExpress = true;
                            //    AgreementID = CurrentAgreement.ID;
                            //    AgreementRouteID = CurrentRoute.ID;
                            //    AgreementSlabID = slabList[r].ID;
                            //    break;
                            //}
                        }
                        else
                        // If Current Slab Charge Type (%) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            //List<AgreementSlab> slabListDetail =
                            //            dcAgreement.AgreementSlabs.Where(P => P.AgreementRouteID == CurrentRoute.ID
                            //            && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetPharmaPercentageShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID.Value, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                    else
                                // If Current Slab Charge Type Kgs.
                                if (slabList[r].ChargeTypeID == 1)
                    {
                        // If Current Slab Charge Type (Kgs) and Cumulative ( No ).
                        if (slabList[r].CumulativeID == 1)
                        {
                            if (!HasFraction)
                                Results = Weight * slabList[r].Amount;
                            else
                            {
                                double val = ((slabList[r].FromWeight * 1000) - 1) / 1000;
                                Weight = Weight - val;
                                Results = Math.Truncate(Weight / slabList[r].ForEachFraction) * slabList[r].Amount;
                                if (Convert.ToDouble((Weight) % slabList[r].ForEachFraction) > 0)
                                    Results += slabList[r].Amount;
                            }

                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            ////AddData(SName.AgreementSlab, AgreementSlabID.Value, Results);
                            break;
                        }
                        else
                        // If Current Slab Charge Type (Kgs) and Cumulative ( From Previous or From Beginning or Slab Option ).
                        {
                            List<TariffTemperatureSlab> slabListDetail =
                                        dcAgreement.TariffTemperatureSlabs.Where(P => P.TariffTemperatureDetailID == CurrentRoute.ID
                                        && P.FromWeight <= Weight && P.StatusID == 1).ToList();

                            Results = GetPharmaKGShipment(Weight, CurrentRoute, slabList, r, slabList[r].CumulativeID.Value, Results, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                            find = true;
                            FromExpress = true;
                            AgreementID = CurrentAgreement.ID;
                            AgreementRouteID = CurrentRoute.ID;
                            AgreementSlabID = slabList[r].ID;
                            break;
                        }
                    }
                }
            }
            return Results;
        }
        private double GetPharmaKGShipment(double Weight, TariffTemperatureDetail CurrentRoute, List<TariffTemperatureSlab> slabList, int P,
                                  int CumulativeID, double PreviousShipmentValue, TariffTemperature CurrentAgreement, MastersDataContext dcAgreement,
                                  int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (Kgs) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                    if (!HasFraction)
                    {
                        ShipmentValue += Weight * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                    else
                    {
                        ShipmentValue += (Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                else
                    if (CurrentRoute.Amount > 0)
                {
                    ShipmentValue += CurrentRoute.Amount;
                    //AddData(SName.AgreementRoute, slabList[P].ID, ShipmentValue);
                }
                // else
                //  ShipmentValue += GetFromStandardTariff(CurrentRoute, FromStation, ToStation, LoadTypeID, CurrentRoute.MinWeight, date, HWStarting);
            }
            else
                // If Current Slab Charge Type (Kgs) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    if (!HasFraction)
                    {
                        ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                        //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    if (!HasFraction)
                    {
                        ShipmentValue += (Weight - CurrentRoute.MinWeight.Value) * slabList[P].Amount;
                        //AddData(SName.AgreementRoute, CurrentRoute.ID, ShipmentValue);
                    }
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (Kgs) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    if (!HasFraction)
                        ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    if (!HasFraction)
                        ShipmentValue += (Weight - CurrentRoute.MinWeight.Value) * slabList[P].Amount;
                    else
                    {
                        double val = ((slabList[P].FromWeight * 1000) - 1) / 1000;
                        Weight = Weight - val;
                        ShipmentValue += Math.Truncate(Weight / slabList[P].ForEachFraction) * slabList[P].Amount;
                        if (Convert.ToDouble((Weight) % slabList[P].ForEachFraction) > 0)
                            ShipmentValue += slabList[P].Amount;
                    }

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (Kgs) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
                ShipmentValue = GetSlabOptionValue(Weight, CurrentRoute, slabList);

            return ShipmentValue;
        }

        private double GetPharmaFixedShipment(double Weight, TariffTemperatureDetail CurrentRoute, List<TariffTemperatureSlab> slabList, int P,
                                   int CumulativeID, double PreviousShipmentValue, TariffTemperature CurrentAgreement, MastersDataContext dcAgreement,
                                   int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (Fixed) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                    if (!HasFraction)
                        ShipmentValue += slabList[P].Amount;
                    else
                        ShipmentValue += CurrentRoute.Amount;
                else
                    ShipmentValue += slabList[P].Amount;
            }
            else
                // If Current Slab Charge Type (Fixed) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    ShipmentValue += slabList[P].Amount;

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    // else
                    //If Comming Slab Charge Type is %
                    //if (slabList[P - 1].ChargeTypeID == 2)
                    //ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    //else
                    //If Comming Slab Charge Type is FOC
                    //if (slabList[P - 1].ChargeTypeID == 4)
                    // ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += slabList[P].Amount;
                    ShipmentValue += CurrentRoute.Amount;
                }
            }
            else
                    // If Current Slab Charge Type (Fixed) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    ShipmentValue += slabList[P].Amount;

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    //else
                    //    //If Comming Slab Charge Type is %
                    //    if (slabList[P - 1].ChargeTypeID == 2)
                    //    ShipmentValue = GetPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    //else
                    //            //If Comming Slab Charge Type is FOC
                    //            if (slabList[P - 1].ChargeTypeID == 4)
                    //    ShipmentValue = GetFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += slabList[P].Amount;
                    ShipmentValue += CurrentRoute.Amount;
                }
            }
            else
                        // If Current Slab Charge Type (Fixed) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.Amount;

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetPharmaKGShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                //else
                //    //If Comming Slab Charge Type is %
                //    if (slabList[P - 1].ChargeTypeID == 2)
                //    ShipmentValue = GetPercentageShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetPharmaFixedShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                //else
                //            //If Comming Slab Charge Type is FOC
                //            if (slabList[P - 1].ChargeTypeID == 4)
                //    ShipmentValue = GetFOCShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }

        private double GetPharmaPercentageShipment(double Weight, TariffTemperatureDetail CurrentRoute, List<TariffTemperatureSlab> slabList, int P,
                               int CumulativeID, double PreviousShipmentValue, TariffTemperature CurrentAgreement, MastersDataContext dcAgreement,
                               int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (%) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0 && LoadTypeID != 2 && LoadTypeID != 3)
                    ShipmentValue += 0;
                else
                        if (CurrentRoute.Amount > 0)
                    ShipmentValue += CurrentRoute.Amount;
            }
            else
                // If Current Slab Charge Type (%) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    //ShipmentValue += 0;

                    //If Standard Taiff in the Top Specified
                    //if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    //{
                    //    double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}
                    //else
                    //{
                    //    //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                    //    standardtariffList = new List<StandardTariff>();
                    //    standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                    //                && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                    //    double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);

                }
                else
                    if (P == 0)
                {
                    //ShipmentValue += 0;

                    //If Standard Taiff in the Top Specified
                    //if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    //{
                    //    double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}
                    //else
                    //{
                    //    //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                    //    standardtariffList = new List<StandardTariff>();
                    //    standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                    //                && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                    //    double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight, CurrentRoute.MinWeight, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (%) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    //ShipmentValue += (Weight - slabList[P - 1].ToWeight) * slabList[P].Amount;

                    //If Standard Taiff in the Top Specified
                    //if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    //{
                    //    double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}
                    //else
                    //{
                    //    //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                    //    standardtariffList = new List<StandardTariff>();
                    //    standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                    //                && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                    //    double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - slabList[P - 1].ToWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    //ShipmentValue += (Weight - CurrentRoute.MinWeight) * slabList[P].Amount;

                    //If Standard Taiff in the Top Specified
                    //if (CurrentAgreement.StandardTariffID != null && CurrentAgreement.StandardTariffID.Value > 0)
                    //{
                    //    double v = GetFromStandardTariff(CurrentAgreement.StandardTariffID.Value, FromStation, ToStation, Weight - CurrentRoute.MinWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}
                    //else
                    //{
                    //    //If Standard Taiff in the Top Not Specified then Get Last Standard Tariff
                    //    standardtariffList = new List<StandardTariff>();
                    //    standardtariffList = dcMaster.StandardTariffs.Where(c => c.LoadTypeID == LoadTypeID
                    //                && CurrentAgreement.FromDate >= c.FromDate && CurrentAgreement.FromDate <= c.ToDate && c.StatusID == 1).ToList();

                    //    double v = GetFromStandardTariff(standardtariffList[standardtariffList.Count - 1].ID, FromStation, ToStation, Weight - CurrentRoute.MinWeight, 0, LoadTypeID, CurrentRoute);
                    //    double val = v * (slabList[P].Amount / 100);
                    //    ShipmentValue += v - val;
                    //}

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (%) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.Amount;

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                    //If Comming Slab Charge Type is %
                    if (slabList[P - 1].ChargeTypeID == 2)
                    ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                            //If Comming Slab Charge Type is FOC
                            if (slabList[P - 1].ChargeTypeID == 4)
                    ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }
        private double GetPharmaFOCShipment(double Weight, TariffTemperatureDetail CurrentRoute, List<TariffTemperatureSlab> slabList, int P,
                               int CumulativeID, double PreviousShipmentValue, TariffTemperature CurrentAgreement, MastersDataContext dcAgreement,
                               int FromStation, int ToStation, int LoadTypeID, DateTime date, bool HasFraction)
        {
            double ShipmentValue = PreviousShipmentValue;

            // If Current Slab Charge Type (fOC) and Cumulative ( No ).
            if (CumulativeID == 1 || P == -1)
            {
                if (P == 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                }
                else
                {
                    ShipmentValue += CurrentRoute.Amount;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);
                }
            }
            else
                // If Current Slab Charge Type (fOC) and Cumulative ( From Previous ).
                if (CumulativeID == 2)
            {
                if (P > 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                    if (P == 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                    // If Current Slab Charge Type (fOC) and Cumulative ( From Beginning ).
                    if (CumulativeID == 3)
            {
                if (P > 0)
                {
                    ShipmentValue += 0;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P - 1].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P - 1].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P - 1].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P - 1].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
                else
                            if (P == 0)
                {
                    ShipmentValue += (Weight - CurrentRoute.Amount) * slabList[P].Amount;
                    //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                    //If Comming Slab Charge Type is KG
                    if (slabList[P].ChargeTypeID == 1)
                        ShipmentValue = GetPharmaKGShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                        //If Comming Slab Charge Type is %
                        if (slabList[P].ChargeTypeID == 2)
                        ShipmentValue = GetPharmaPercentageShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                            //If Comming Slab Charge Type is Fixed
                            if (slabList[P].ChargeTypeID == 3)
                        ShipmentValue = GetPharmaFixedShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                    else
                                //If Comming Slab Charge Type is FOC
                                if (slabList[P].ChargeTypeID == 4)
                        ShipmentValue = GetPharmaFOCShipment(slabList[P].ToWeight, CurrentRoute, slabList, P - 1, slabList[P].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                }
            }
            else
                        // If Current Slab Charge Type (fOC) and Cumulative ( Slabe Option ).
                        if (CumulativeID == 4)
            {
                ShipmentValue = CurrentRoute.Amount;
                //AddData(SName.AgreementSlab, slabList[P].ID, ShipmentValue);

                //If Comming Slab Charge Type is KG
                if (slabList[P - 1].ChargeTypeID == 1)
                    ShipmentValue = GetPharmaKGShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                    //If Comming Slab Charge Type is %
                    if (slabList[P - 1].ChargeTypeID == 2)
                    ShipmentValue = GetPharmaPercentageShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                        //If Comming Slab Charge Type is Fixed
                        if (slabList[P - 1].ChargeTypeID == 3)
                    ShipmentValue = GetPharmaFixedShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
                else
                            //If Comming Slab Charge Type is FOC
                            if (slabList[P - 1].ChargeTypeID == 4)
                    ShipmentValue = GetPharmaFOCShipment(Weight - slabList[P - 1].ToWeight, CurrentRoute, slabList, P - 1, slabList[P - 1].CumulativeID.Value, ShipmentValue, CurrentAgreement, dcAgreement, FromStation, ToStation, LoadTypeID, date, HasFraction);
            }

            return ShipmentValue;
        }
        private double GetSlabOptionValue(double Weight, TariffTemperatureDetail CurrentRoute, List<TariffTemperatureSlab> slabList)
        {
            double ShipmentValue = 0;

            bool f = false;
            double newWeight = Weight - CurrentRoute.MinWeight.Value;
            for (int i = 0; i < slabList.Count; i++)
            {
                if (newWeight >= slabList[i].FromWeight && newWeight <= slabList[i].ToWeight)
                {
                    ShipmentValue = newWeight * slabList[i].Amount;
                    f = true;
                    break;
                }
            }

            if (!f && Weight > CurrentRoute.MinWeight)
                ShipmentValue = newWeight * slabList[0].Amount;
            ShipmentValue += CurrentRoute.Amount;

            return ShipmentValue;
        }
    }
}