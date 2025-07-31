using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KPCLCsvFileReader
{
    public class ReservoirData
    {
        public int Id { get; set; }
        public string Reservoir { get; set; }
        public decimal? GrossCap { get; set; }
        public decimal? LiveCap { get; set; }
        public string FRL { get; set; }
        public string MDDL { get; set; }
        public string PresentLevel { get; set; }
        public decimal? PresentCapMCft { get; set; }
        public decimal? PresentCapPercent { get; set; }
        public decimal? PresentEqEnergy { get; set; }
        public string LastYearLevel { get; set; }
        public decimal? LastYearCapMCft { get; set; }
        public decimal? LastYearCapPercent { get; set; }
        public decimal? LastYearEqEnergy { get; set; }
        public decimal? PresentInflowDay { get; set; }
        public decimal? PresentInflowMonth { get; set; }
        public decimal? PresentInflowWY { get; set; }
        public decimal? LastYearInflowDay { get; set; }
        public decimal? LastYearInflowMonth { get; set; }
        public decimal? LastYearInflowWY { get; set; }
        public decimal? PresentDischargeDay { get; set; }
        public decimal? PresentDischargeMonth { get; set; }
        public decimal? PresentDischargeWY { get; set; }
        public decimal? LastYearDischargeDay { get; set; }
        public decimal? LastYearDischargeMonth { get; set; }
        public decimal? LastYearDischargeWY { get; set; }
        public decimal? InflowCusecs { get; set; }
        public decimal? LastYearInflowCusecs { get; set; }
        public DateTime Date { get; set; }
    }
    public class ReservoirDataDB
    {
        public int? ReservoirID { get; set; }
        public decimal? FRL_As_Per_Design { get; set; }
        public decimal? FRL { get; set; }
        public decimal? MDDL { get; set; }
        public decimal? Cill_Level { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Reservior_Level { get; set; }
        public decimal? StorageCapacity_AsPerDesign { get; set; }
        public decimal? TMC_GrossCapacity { get; set; }
        public decimal? TMC_Live_Above_Cill { get; set; }
        public decimal? TMC_Above_Cill { get; set; }
        public decimal? Flow_Inflow { get; set; }
        public decimal? Flow_OutFlow { get; set; }
        public decimal? Flow_Withdrawal { get; set; }
        public decimal? Evaporation { get; set; }
        public decimal? Cum_TMC_Inflow { get; set; }
        public decimal? Cum_TMC_OutFlow { get; set; }
        public decimal? Cum_TMC_Withdrawl { get; set; }
        public decimal? Cum_Evaporation { get; set; }
        public decimal? River_Spillway { get; set; }
        public decimal? River_PowerHouse { get; set; }
        public decimal? River_Sluice { get; set; }
        public decimal? Other_Abstractions { get; set; }
        public decimal? Foreshore_LIS { get; set; }
        public decimal? Drinking_Domestic { get; set; }
        public decimal? Industries { get; set; }
        public decimal? Storage_Per { get; set; }
        public decimal? GrossCapacity { get; set; }
        public decimal? LiveCapacity { get; set; }
        public decimal? Storage_Above_Cill { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? Approvedby { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? SourceID { get; set; }
        public decimal? Eq_Energy_MU { get; set; }
        public decimal? Discharge { get; set; }
        public decimal? Discharge_Cum { get; set; }
    }

}
