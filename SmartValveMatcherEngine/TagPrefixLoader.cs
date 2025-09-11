using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace SmartValveMatcherEngine.Services
{
    public static class TagPrefixLoader
    {
        public static List<string> LoadPrefixesFromExcel(string filePath)
        {
            var prefixes = new List<string>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Prefix Excel file not found.", filePath);

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.First(); // Assume first sheet
                foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
                {
                    var prefix = row.Cell(1).GetString().Trim().ToUpper();
                    if (!string.IsNullOrWhiteSpace(prefix))
                        prefixes.Add(prefix);
                }
            }

            return prefixes;
        }
    }
}
