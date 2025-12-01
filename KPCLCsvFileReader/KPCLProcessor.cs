using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KPCLCsvFileReader
{
    public class KPCLProcessor
    {
        private readonly Dictionary<string, int> reservoirMapping = new()
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

        public async Task ProcessReservoirData()
        {
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
                    string[] acceptedFormats = new[]
                    {
                        "yyyy-MM-dd", "dd-MM-yyyy", "yyyy/MM/dd", "dd/MM/yyyy",
                        "MM/dd/yyyy", "M/d/yyyy", "d-M-yyyy", "yyyyMMdd"
                    };

                    var dateLine = lines[0].Split(',');
                    if (dateLine.Length > 1 && DateTime.TryParseExact(dateLine[1], acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out fileDate))
                    {
                        for (int i = 3; i < lines.Length; i++)
                        {
                            var columns = lines[i].Split(',');
                            if (columns.Length < 27 || string.IsNullOrWhiteSpace(columns[0]))
                            {
                                Console.WriteLine($"Skipping row {i}: Invalid column count ({columns.Length}) or empty ID");
                                continue;
                            }
                            
                            int csvId = ParseInt(columns[0]) ?? 0;
                            if (csvId < 1 || csvId > 9) continue;
                            
                            string reservoirName = columns[1].ToUpper().Trim();
                            int? realReservoirId = reservoirMapping.ContainsKey(reservoirName) ? reservoirMapping[reservoirName] : null;
                            
                            if (!realReservoirId.HasValue) continue;
                            
                            try
                            {
                                var csvData = new ReservoirData
                                {
                                    Date = fileDate,
                                    Id = csvId,
                                    Reservoir = GetColumn(columns, 1),
                                    GrossCap = ParseDecimal(GetColumn(columns, 2)),
                                    LiveCap = ParseDecimal(GetColumn(columns, 3)),
                                    FRL = ConvertToFeet(GetColumn(columns, 4)),
                                    MDDL = ConvertToFeet(GetColumn(columns, 5)),
                                    PresentLevel = GetColumn(columns, 6),
                                    PresentCapMCft = ParseDecimal(GetColumn(columns, 7)),
                                    PresentCapPercent = ParseDecimal(GetColumn(columns, 8)),
                                    PresentEqEnergy = ParseDecimal(GetColumn(columns, 9)),
                                    LastYearLevel = GetColumn(columns, 10),
                                    LastYearCapMCft = ParseDecimal(GetColumn(columns, 11)),
                                    LastYearCapPercent = ParseDecimal(GetColumn(columns, 12)),
                                    LastYearEqEnergy = ParseDecimal(GetColumn(columns, 13)),
                                    PresentInflowDay = ParseDecimal(GetColumn(columns, 14)),
                                    PresentInflowMonth = ParseDecimal(GetColumn(columns, 15)),
                                    PresentInflowWY = ParseDecimal(GetColumn(columns, 16)),
                                    LastYearInflowDay = ParseDecimal(GetColumn(columns, 17)),
                                    LastYearInflowMonth = ParseDecimal(GetColumn(columns, 18)),
                                    LastYearInflowWY = ParseDecimal(GetColumn(columns, 19)),
                                    PresentDischargeDay = ParseDecimal(GetColumn(columns, 20)),
                                    PresentDischargeMonth = ParseDecimal(GetColumn(columns, 21)),
                                    PresentDischargeWY = ParseDecimal(GetColumn(columns, 22)),
                                    LastYearDischargeDay = ParseDecimal(GetColumn(columns, 23)),
                                    LastYearDischargeMonth = ParseDecimal(GetColumn(columns, 24)),
                                    LastYearDischargeWY = ParseDecimal(GetColumn(columns, 25)),
                                    InflowCusecs = ParseDecimal(GetColumn(columns, 26)),
                                    LastYearInflowCusecs = ParseDecimal(GetColumn(columns, 27))
                                };
                                allData.Add(csvData);
                                
                                var dbRecord = new ReservoirDataDB
                                {
                                    ReservoirID = realReservoirId,
                                    Date = csvData.Date,
                                    FRL_As_Per_Design = ExtractNumericValue(csvData.FRL),
                                    FRL = ExtractNumericValue(csvData.FRL),
                                    MDDL = ExtractNumericValue(csvData.MDDL),
                                    Reservior_Level = ExtractNumericValue(csvData.PresentLevel),
                                    StorageCapacity_AsPerDesign = csvData.GrossCap / 1000,
                                    TMC_GrossCapacity = csvData.PresentCapMCft / 1000,
                                    TMC_Live_Above_Cill = ((csvData.PresentCapMCft) - (csvData.GrossCap-csvData.LiveCap))/1000,
                                    Flow_Inflow = csvData.PresentInflowDay * 11.574074074074073m,
                                    Flow_OutFlow = csvData.PresentDischargeDay * 11.574074074074073m,
                                    Cum_TMC_Inflow = csvData.PresentInflowWY,
                                    Cum_TMC_OutFlow = csvData.PresentDischargeWY,
                                    Storage_Per = csvData.PresentCapPercent,
                                    Eq_Energy_MU = csvData.PresentEqEnergy
                                };
                                dbData.Add(dbRecord);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing row {i}: {ex.Message}");
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
        }

        private static string GetColumn(string[] columns, int index) => index < columns.Length ? columns[index] : string.Empty;
        private static int? ParseInt(string value) => string.IsNullOrWhiteSpace(value) ? null : int.TryParse(value, out int result) ? result : null;
        private static decimal? ParseDecimal(string value) => string.IsNullOrWhiteSpace(value) ? null : decimal.TryParse(value, out decimal result) ? result : null;
        
        private static string ConvertToFeet(string value)
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

        private static decimal? ExtractNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var numericPart = value.Replace("ft", "").Replace("m", "").Trim();
            return decimal.TryParse(numericPart, out decimal result) ? result : null;
        }

        private static async Task PostToAPI(List<ReservoirDataDB> data)
        {
            var startTime = DateTime.Now;
            var postUrl = "http://localhost:60005/api/data/Post_ReservoirDataBulk";

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

            int logId = 35;
            int rowno = AuditLog.SaveAuditLog(logId, DateTime.Now, data.Count, result, (int)response.StatusCode, json, null, null, milliseconds);
            Console.WriteLine($"Audit log saved with ID: {rowno}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode} - Unable to post data");
                return;
            }

            Console.WriteLine($"API Response: {result}");
            Console.WriteLine($"Successfully posted {data.Count} records to API.");
        }
    }
}