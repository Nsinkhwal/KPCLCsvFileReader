using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text.RegularExpressions;
using System.Globalization;

namespace KPCLCsvFileReader
{
    public class PdfProcessor
    {
        private readonly string pdfDirectory;

        public PdfProcessor()
        {
            pdfDirectory = "../../../../pdffiles";
        }

        public async Task ProcessPdfFiles()
        {
            var pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf");

            if (pdfFiles.Length == 0)
            {
                Console.WriteLine("No PDF files found in the directory.");
                return;
            }

            foreach (string pdfFile in pdfFiles)
            {
                Console.WriteLine($"Processing PDF file: {Path.GetFileName(pdfFile)}");
                await ProcessSinglePdf(pdfFile);
            }
        }

        private async Task ProcessSinglePdf(string pdfFilePath)
        {
            try
            {
                Console.WriteLine($"Extracting data from: {Path.GetFileName(pdfFilePath)}");
                
                var extractedData = ExtractWaterQualityData(pdfFilePath);
                
                if (extractedData.Count > 0)
                {
                    await PostWaterQualityData(extractedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing PDF {Path.GetFileName(pdfFilePath)}: {ex.Message}");
            }
        }

        private List<WaterQualityData> ExtractWaterQualityData(string pdfFilePath)
        {
            var waterQualityData = new List<WaterQualityData>();
            
            try
            {
                using (var document = PdfDocument.Open(pdfFilePath))
                {
                    Console.WriteLine($"PDF has {document.NumberOfPages} pages");
                    
                    foreach (var page in document.GetPages())
                    {
                        var text = page.Text;
                        Console.WriteLine($"\nPage {page.Number} content:");
                        Console.WriteLine(text);
                        Console.WriteLine("\n" + new string('=', 50));
                        
                        var pageData = ParseWaterQualityFromText(text, Path.GetFileName(pdfFilePath));
                        waterQualityData.AddRange(pageData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PDF: {ex.Message}");
            }
            
            return waterQualityData;
        }
        
        private List<WaterQualityData> ParseWaterQualityFromText(string text, string fileName)
        {
            var data = new List<WaterQualityData>();
            var date = ExtractDateFromFileName(fileName) ?? DateTime.Now;
            
            var stationPattern = @"(\d{4})Jul";
            var stationMatches = Regex.Split(text, stationPattern);
            
            for (int i = 1; i < stationMatches.Length; i += 2)
            {
                if (i + 1 < stationMatches.Length)
                {
                    var stationId = stationMatches[i];
                    var stationData = stationMatches[i + 1];
                    var fullRow = stationId + "Jul" + stationData;
                    
                    var record = ParseDataRowFromContinuousText(fullRow, date);
                    if (record != null)
                    {
                        data.Add(record);
                        Console.WriteLine($"Extracted record for Station {record.StationId}: {record.StationName}");
                    }
                }
            }
            
            return data;
        }
        
        private WaterQualityData? ParseDataRowFromContinuousText(string rowText, DateTime date)
        {
            try
            {
                var stationMatch = Regex.Match(rowText, @"^(\d{4})");
                if (!stationMatch.Success) return null;
                
                var stationId = int.Parse(stationMatch.Groups[1].Value);
                
                var nameMatch = Regex.Match(rowText, @"Jul([^DE]+?)([DE])\s");
                var stationName = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : "";
                var classification = nameMatch.Success ? nameMatch.Groups[2].Value : "";
                
                // Use word boundaries to properly separate numeric values
                var numericPattern = @"\b(\d+(?:\.\d+)?|BDL)\b";
                var numericMatches = Regex.Matches(rowText, numericPattern);
                
                Console.WriteLine($"Found {numericMatches.Count} numeric values in: {rowText.Substring(0, Math.Min(100, rowText.Length))}...");
                for (int j = 0; j < Math.Min(10, numericMatches.Count); j++)
                {
                    Console.WriteLine($"  Value {j}: {numericMatches[j].Value}");
                }
                
                if (numericMatches.Count < 10) return null;
                
                var record = new WaterQualityData
                {
                    Date = date,
                    StationId = stationId,
                    SamplingMonth = "Jul",
                    StationName = stationName,
                    Classification = classification
                };
                
                int valueIndex = 1; // Skip station ID which is first match
                if (valueIndex < numericMatches.Count) record.Temperature = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.DissolvedOxygen = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.pH = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Conductivity = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.BOD = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Nitrates = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Nitrites = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.FecalColiform = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.TotalColiform = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Carbonate = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Bicarbonate = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Turbidity = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.PhenolphthaleinAlkalinity = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Alkalinity = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Chlorides = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.COD = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.TotalNitrogen = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Ammonia = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Hardness = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.CalciumAsCaCO3 = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Calcium = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.MagnesiumAsCaCO3 = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Magnesium = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Sulphates = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Sodium = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.TotalDissolvedSolids = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.TotalSuspendedSolids = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Phosphates = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Boron = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Potassium = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.Fluorides = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.SodiumPercentage = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.SAR = ParseDecimal(numericMatches[valueIndex++].Value);
                if (valueIndex < numericMatches.Count) record.OrthoPhosphate = ParseDecimal(numericMatches[valueIndex++].Value);
                
                return record;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing continuous text row: {ex.Message}");
                return null;
            }
        }
        
        private int? ParseInt(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "BDL" ? null : int.TryParse(value, out int result) ? result : null;
        }
        
        private decimal? ParseDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "BDL" ? null : decimal.TryParse(value, out decimal result) ? result : null;
        }
        
        private DateTime? ExtractDateFromFileName(string fileName)
        {
            var match = Regex.Match(fileName, @"(\w{3})-(\d{4})");
            if (match.Success)
            {
                var monthStr = match.Groups[1].Value;
                var year = int.Parse(match.Groups[2].Value);
                
                if (DateTime.TryParseExact($"{monthStr} {year}", "MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
            }
            return null;
        }

        private async Task PostWaterQualityData(List<WaterQualityData> data)
        {
            var startTime = DateTime.Now;
            var postUrl = "http://localhost:60005/api/data/PostWaterQualityData";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(4);

            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            Console.WriteLine($"Posting {data.Count} water quality records to API...");

            var response = await client.PostAsync(postUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            var endTime = DateTime.Now;
            var totalTimeTaken = endTime - startTime;
            int milliseconds = (int)totalTimeTaken.TotalMilliseconds;

            int logId = 36;
            int rowno = AuditLog.SaveAuditLog(logId, DateTime.Now, data.Count, result, (int)response.StatusCode, json, null, null, milliseconds);
            Console.WriteLine($"Water quality audit log saved with ID: {rowno}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode} - Unable to post water quality data");
                return;
            }

            Console.WriteLine($"Successfully posted {data.Count} water quality records to API.");
        }
    }

    public class WaterQualityData
    {
        public int? StationId { get; set; }
        public DateTime Date { get; set; }
        public string? SamplingMonth { get; set; }
        public string? StationName { get; set; }
        public string? Classification { get; set; }
        
        public decimal? Temperature { get; set; }
        public decimal? DissolvedOxygen { get; set; }
        public decimal? pH { get; set; }
        public decimal? Conductivity { get; set; }
        public decimal? BOD { get; set; }
        public decimal? Nitrates { get; set; }
        public decimal? Nitrites { get; set; }
        public decimal? FecalColiform { get; set; }
        public decimal? TotalColiform { get; set; }
        public decimal? Carbonate { get; set; }
        public decimal? Bicarbonate { get; set; }
        public decimal? Turbidity { get; set; }
        public decimal? PhenolphthaleinAlkalinity { get; set; }
        public decimal? Alkalinity { get; set; }
        public decimal? Chlorides { get; set; }
        public decimal? COD { get; set; }
        public decimal? TotalNitrogen { get; set; }
        public decimal? Ammonia { get; set; }
        public decimal? Hardness { get; set; }
        public decimal? CalciumAsCaCO3 { get; set; }
        public decimal? Calcium { get; set; }
        public decimal? MagnesiumAsCaCO3 { get; set; }
        public decimal? Magnesium { get; set; }
        public decimal? Sulphates { get; set; }
        public decimal? Sodium { get; set; }
        public decimal? TotalDissolvedSolids { get; set; }
        public decimal? TotalSuspendedSolids { get; set; }
        public decimal? Phosphates { get; set; }
        public decimal? Boron { get; set; }
        public decimal? Potassium { get; set; }
        public decimal? Fluorides { get; set; }
        public decimal? SodiumPercentage { get; set; }
        public decimal? SAR { get; set; }
        public decimal? OrthoPhosphate { get; set; }
        public string? Remarks { get; set; }
    }
}