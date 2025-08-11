using KPCLCsvFileReader;
using System.Text;
using System.Text.Json;

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
    await PostToAPI(dbData);
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

static async Task PostToAPI(List<ReservoirDataDB> data)
{
    var startTime = DateTime.Now;
    var postUrl = "http://localhost:60005/api/data/Post_ReservoirDataBulk";
    //var postUrl = "http://kwris.aciwrm.org/api/data/Post_ReservoirDataBulk";

    using var client = new HttpClient();
    client.Timeout = TimeSpan.FromMinutes(4);

    var json = JsonSerializer.Serialize(data);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    Console.WriteLine($"Posting {data.Count} records to API...");

    var response = await client.PostAsync(postUrl, content);
    var result = await response.Content.ReadAsStringAsync();

    var endTime = DateTime.Now;
    var totalTimeTaken = endTime - startTime;
    int milliseconds = (int)totalTimeTaken.TotalMilliseconds;

    var lastUpdatedLog = DateTime.Now;
    int noOfRecords = data.Count;
    int logId = 35; // Change if needed
    string remark = result;

    // Save the audit log 
    int rowno = AuditLog.SaveAuditLog(logId, lastUpdatedLog, noOfRecords, remark, (int)response.StatusCode, json, startTime.ToString(), endTime.ToString(), milliseconds);
    Console.WriteLine($"Audit log saved with ID: {rowno}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Error: {response.StatusCode} - Unable to post data");
        Console.WriteLine($"Time taken: {milliseconds}ms, Records: {noOfRecords}");
        return;
    }
    
    Console.WriteLine($"API Response: {result}");
    Console.WriteLine($"Successfully posted {data.Count} records to API.");
}
