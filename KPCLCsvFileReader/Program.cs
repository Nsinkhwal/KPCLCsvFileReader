using KPCLCsvFileReader;
using Microsoft.Data.SqlClient;

var reservoirMapping = new Dictionary<string, int>
{
    { "SUPA DAM", 11 },
    { "MANI DAM", 171 },
    { "KADRA DAM", 195 },
    { "GERUSOPPA DAM", 198 },
    { "KODASALLI DAM", 199 },
    { "LINGAN MAKKI DAM", 8 },
    { "TALKALALE DAM", 55 },
    { "TATTIHALLA DAM", 123 },
    { "BP DAM", 111 }
};

string csvDirectory = "../../../../csvfiles";
var allData = new List<ReservoirData>();
var dbData = new List<ReservoirDataDB>();

var csvFiles = Directory.GetFiles(csvDirectory, "*.csv");

if (csvFiles.Length == 0)
{
    Console.WriteLine("No CSV files found in the directory.");
    return;
}

foreach (string csvFile in csvFiles)
{
    Console.WriteLine($"Reading file: {Path.GetFileName(csvFile)}");
    
    var lines = File.ReadAllLines(csvFile);
    DateTime fileDate = DateTime.MinValue;
    
    if (lines.Length > 3)
    {
        var dateLine = lines[0].Split(',');
        if (dateLine.Length > 1 && DateTime.TryParse(dateLine[1], out fileDate))
        {
            for (int i = 3; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');
                if (columns.Length >= 27 && !string.IsNullOrWhiteSpace(columns[0]))
                {
                    int csvId = ParseInt(columns[0]) ?? 0;
                    if (csvId < 1 || csvId > 9) continue;
                    
                    string reservoirName = columns[1].ToUpper().Trim();
                    int? realReservoirId = reservoirMapping.ContainsKey(reservoirName) ? reservoirMapping[reservoirName] : null;
                    
                    if (!realReservoirId.HasValue) continue;
                    
                    var csvData = new ReservoirData
                    {
                        Date = fileDate,
                        Id = ParseInt(columns[0]) ?? 0,
                        Reservoir = columns[1],
                        GrossCap = ParseDecimal(columns[2]),
                        LiveCap = ParseDecimal(columns[3]),
                        FRL = ConvertToFeet(columns[4]),
                        MDDL = ConvertToFeet(columns[5]),
                        PresentLevel = columns[6],
                        PresentCapMCft = ParseDecimal(columns[7]),
                        PresentCapPercent = ParseDecimal(columns[8]),
                        PresentEqEnergy = ParseDecimal(columns[9]),
                        LastYearLevel = columns[10],
                        LastYearCapMCft = ParseDecimal(columns[11]),
                        LastYearCapPercent = ParseDecimal(columns[12]),
                        LastYearEqEnergy = ParseDecimal(columns[13]),
                        PresentInflowDay = ParseDecimal(columns[14]),
                        PresentInflowMonth = ParseDecimal(columns[15]),
                        PresentInflowWY = ParseDecimal(columns[16]),
                        LastYearInflowDay = ParseDecimal(columns[17]),
                        LastYearInflowMonth = ParseDecimal(columns[18]),
                        LastYearInflowWY = ParseDecimal(columns[19]),
                        PresentDischargeDay = ParseDecimal(columns[20]),
                        PresentDischargeMonth = ParseDecimal(columns[21]),
                        PresentDischargeWY = ParseDecimal(columns[22]),
                        LastYearDischargeDay = ParseDecimal(columns[23]),
                        LastYearDischargeMonth = ParseDecimal(columns[24]),
                        LastYearDischargeWY = ParseDecimal(columns[25]),
                        InflowCusecs = ParseDecimal(columns[26]),
                        LastYearInflowCusecs = columns.Length > 27 ? ParseDecimal(columns[27]) : null
                    };
                    allData.Add(csvData);
                    
                    var dbRecord = new ReservoirDataDB
                    {
                        ReservoirID = realReservoirId,
                        Date = csvData.Date,
                        FRL_As_Per_Design = ExtractNumericValue(csvData.FRL),
                        FRL = ExtractNumericValue(csvData.FRL),
                        MDDL = ExtractNumericValue(csvData.MDDL),
                        Reservior_Level = ExtractNumericValue(csvData.PresentLevel) ,
                        StorageCapacity_AsPerDesign = csvData.GrossCap / 1000,
                        TMC_GrossCapacity = csvData.PresentCapMCft / 1000,
                        TMC_Live_Above_Cill = csvData.LiveCap/1000,
                        Flow_Inflow = csvData.PresentInflowDay * 11.574074074074073m,
                        Flow_OutFlow = csvData.PresentDischargeDay * 11.574074074074073m,
                        Cum_TMC_Inflow = csvData.PresentInflowWY,
                        Cum_TMC_OutFlow = csvData.PresentDischargeWY,
                        Storage_Per = csvData.PresentCapPercent,
                        Eq_Energy_MU = csvData.PresentEqEnergy
                    };
                    dbData.Add(dbRecord);
                }
            }
        }
    }
}

Console.WriteLine($"Total CSV records loaded: {allData.Count}");
Console.WriteLine($"Total DB records created: {dbData.Count}");

if (dbData.Count > 0)
{
    await InsertToDatabase(dbData);
}

foreach (var item in allData.Take(5))
{
    Console.WriteLine($"{item.Date:yyyy-MM-dd} - {item.Reservoir} - Level: {item.PresentLevel}");
}

static int? ParseInt(string value) => string.IsNullOrWhiteSpace(value) ? null : int.TryParse(value, out int result) ? result : null;
static decimal? ParseDecimal(string value) => string.IsNullOrWhiteSpace(value) ? null : decimal.TryParse(value, out decimal result) ? result : null;
static DateTime? ParseDate(string value) => string.IsNullOrWhiteSpace(value) ? null : DateTime.TryParse(value, out DateTime result) ? result : null;
static DateTime? ParseDateTime(string value) => string.IsNullOrWhiteSpace(value) ? null : DateTime.TryParse(value, out DateTime result) ? result : null;
static string ConvertToFeet(string value)
{
    if (string.IsNullOrWhiteSpace(value) || !value.Contains("m")) return value;
    var numericPart = value.Replace("m", "").Trim();
    if (decimal.TryParse(numericPart, out decimal meters))
    {
        decimal feet = meters * 3.28084m;
        return $"{feet:F2} ft";
    }
    return value;
}
static decimal? ExtractNumericValue(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return null;
    var numericPart = value.Replace("ft", "").Replace("m", "").Trim();
    return decimal.TryParse(numericPart, out decimal result) ? result : null;
}

static async Task InsertToDatabase(List<ReservoirDataDB> data)
{
    string connectionString = "Server=103.171.96.233,5022;User ID=kwrisuser;Password=S_Admin@Kwr!$@2025;Database=ACIWRM_Lang;MultipleActiveResultSets=True;TrustServerCertificate=true;";
    
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    
    foreach (var record in data)
    {
        string sql = @"
            INSERT INTO ACIWRM_Lang.dbo.tbl_Reservoir_WL_KPCL 
            (ReservoirID, FRL, MDDL, [Date], Reservior_Level, StorageCapacity_AsPerDesign, 
             TMC_GrossCapacity, TMC_Live_Above_Cill, TMC_Above_Cill, Flow_Inflow, Flow_OutFlow, 
             Cum_TMC_Inflow, Cum_TMC_OutFlow, Storage_Per, GrossCapacity, LiveCapacity, 
             Eq_Energy_MU, Discharge, CreatedBy, CreatedOn)
            VALUES 
            (@ReservoirID, @FRL, @MDDL, @Date, @Reservior_Level, @StorageCapacity_AsPerDesign, 
             @TMC_GrossCapacity, @TMC_Live_Above_Cill, @TMC_Above_Cill, @Flow_Inflow, @Flow_OutFlow, 
             @Cum_TMC_Inflow, @Cum_TMC_OutFlow, @Storage_Per, @GrossCapacity, @LiveCapacity, 
             @Eq_Energy_MU, @Discharge, @CreatedBy, @CreatedOn)";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ReservoirID", record.ReservoirID ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FRL", record.FRL ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MDDL", record.MDDL ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Date", record.Date ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Reservior_Level", record.Reservior_Level ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@StorageCapacity_AsPerDesign", record.StorageCapacity_AsPerDesign ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TMC_GrossCapacity", record.TMC_GrossCapacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TMC_Live_Above_Cill", record.TMC_Live_Above_Cill ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TMC_Above_Cill", record.TMC_Above_Cill ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Flow_Inflow", record.Flow_Inflow ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Flow_OutFlow", record.Flow_OutFlow ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cum_TMC_Inflow", record.Cum_TMC_Inflow ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cum_TMC_OutFlow", record.Cum_TMC_OutFlow ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Storage_Per", record.Storage_Per ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@GrossCapacity", record.GrossCapacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LiveCapacity", record.LiveCapacity ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Eq_Energy_MU", record.Eq_Energy_MU ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Discharge", record.Discharge ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreatedBy", "CSV_Import");
        command.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
        
        await command.ExecuteNonQueryAsync();
    }
    
    Console.WriteLine($"Successfully inserted {data.Count} records into database.");
}
