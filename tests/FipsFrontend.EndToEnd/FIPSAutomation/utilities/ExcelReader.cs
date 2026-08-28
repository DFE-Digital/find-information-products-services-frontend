using ClosedXML.Excel;

namespace find_information_products_services_tests.utilities
{
    internal class ExcelReader
    {
        public class FipsSheetRow
        {
            public string Product_Locator { get; set; }
            public string Filter_Tag { get; set; }
            public string Message { get; set; }
            public string Heading { get; set; }
            public string Filter_Text_Locator { get; set; }
            public string Checkbox_Locator { get; set; }
        }

        public class FipsSheetRowUG
        {
            public string Product_Locator { get; set; }
            public string Filter_Tag { get; set; }
            public string Message { get; set; }
            public string Heading { get; set; }
            public string Filter_Text_Locator { get; set; }
            public string Checkbox_Locator { get; set; }
            public string Selected_UserTypes { get; set; }
            public string Selected_UserTypes_Locator { get; set; }
        }

        public class SheetRow
        {
            public string Col1 { get; set; }
            public string Col2 { get; set; }
            public string Col3 { get; set; }
        }

        public static List<FipsSheetRow> getRowsFromExcelFileBySheetName(string fileName, string sheetName)
        {
            //string testDirectory = TestContext.CurrentContext.TestDirectory;

            string excelFilePath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName 
                +"//" +fileName;

            if (!File.Exists(excelFilePath))
            {
                Assert.Fail($"Excel file {fileName} not found at: {excelFilePath}");
            }

            // List to hold the data from the Excel file
            var dataList = new List<FipsSheetRow>();

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(sheetName);
                if (worksheet == null)
                {
                    Assert.Fail($"Sheet {sheetName} not found in the Excel file.");
                }

                var firstRowUsed = worksheet?.FirstRowUsed();
                if (firstRowUsed == null)
                {
                    Assert.Pass("No data found in the Excel sheet.");
                }

                var dataRows = worksheet.RowsUsed().Skip(1);

                foreach (var row in dataRows)
                {
                    var userData = new FipsSheetRow
                    {
                        Product_Locator = row.Cell(1).Value.ToString(),
                        Filter_Tag = row.Cell(2).Value.ToString(),
                        Message = row.Cell(3).Value.ToString(),
                        Heading = row.Cell(4).Value.ToString(),
                        Filter_Text_Locator = row.Cell(5).Value.ToString(),
                        Checkbox_Locator = row.Cell(6).Value.ToString()
                    };
                    dataList.Add(userData);
                }
            }
            return dataList;
        }

        public static List<FipsSheetRowUG> getRowsFromExcelForSelectedUserType(string fileName, string sheetName)
        {
            string excelFilePath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName
                + "//" + fileName;

            if (!File.Exists(excelFilePath))
            {
                Assert.Fail($"Excel file {fileName} not found at: {excelFilePath}");
            }

            // List to hold the data from the Excel file
            var dataList = new List<FipsSheetRowUG>();

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(sheetName);
                if (worksheet == null)
                {
                    Assert.Fail($"Sheet {sheetName} not found in the Excel file.");
                }

                var firstRowUsed = worksheet?.FirstRowUsed();
                if (firstRowUsed == null)
                {
                    Assert.Pass("No data found in the Excel sheet.");
                }

                var dataRows = worksheet.RowsUsed().Skip(1);

                foreach (var row in dataRows)
                {
                    var userData = new FipsSheetRowUG
                    {
                        //Product_Locator Filter_Tag  Message Heading Filter_Text_Locator Checkbox_Locator
                        Product_Locator = row.Cell(1).Value.ToString(),
                        Filter_Tag = row.Cell(2).Value.ToString(),
                        Message = row.Cell(3).Value.ToString(),
                        Heading = row.Cell(4).Value.ToString(),
                        Filter_Text_Locator = row.Cell(5).Value.ToString(),
                       //Checkbox_Locator = row.Cell(6).Value.ToString(),
                        Selected_UserTypes_Locator = row.Cell(6).Value.ToString(),
                        Selected_UserTypes = row.Cell(7).Value.ToString()
                    };
                    dataList.Add(userData);
                }
            }
            return dataList;
        }

        public static List<SheetRow> getCategoryRowsFromExcelFileBySheetName(string fileName, string sheetName)
        {
            string excelFilePath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName
                + "//" + fileName;

            if (!File.Exists(excelFilePath))
            {
                Assert.Fail($"Excel file {fileName} not found at: {excelFilePath}");
            }

            // List to hold the data from the Excel file
            var dataList = new List<SheetRow>();

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var worksheet = workbook.Worksheet(sheetName);
                if (worksheet == null)
                {
                    Assert.Fail($"Sheet {sheetName} not found in the Excel file.");
                }

                var firstRowUsed = worksheet?.FirstRowUsed();
                if (firstRowUsed == null)
                {
                    Assert.Pass("No data found in the Excel sheet.");
                }

                var dataRows = worksheet.RowsUsed().Skip(1);

                foreach (var row in dataRows)
                {
                    var userData = new SheetRow
                    {
                        //Col1, Col2, Col3
                        Col1 = row.Cell(1).Value.ToString(),
                        Col2 = row.Cell(2).Value.ToString(),
                        Col3 = row.Cell(3).Value.ToString(),

                    };
                    dataList.Add(userData);
                }
            }
            return dataList;
        }
    }
}
