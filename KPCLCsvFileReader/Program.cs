using KPCLCsvFileReader;

Console.WriteLine("=== Data Processing Application ===");

// Process KPCL Reservoir Data
Console.WriteLine("\n--- Processing KPCL Reservoir Data ---");

var kpclProcessor = new KPCLProcessor();
await kpclProcessor.ProcessReservoirData();

// Process PDF files for water quality data
Console.WriteLine("\n--- Processing PDF Files for Water Quality Data ---");
var pdfProcessor = new PdfProcessor();
//await pdfProcessor.ProcessPdfFiles();

Console.WriteLine("\n=== Processing Complete ===");
