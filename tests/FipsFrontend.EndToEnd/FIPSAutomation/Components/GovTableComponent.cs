using Microsoft.Playwright;

namespace FiPSAutomation.Components
{
    public static class GovTableComponent
    {
        public static async Task AssertTableColumnValuesAsync(
            IPage page,
            List<Dictionary<string, string>> expectedRows,
            string govTableLocator)
        {
            var tableLocator = page.Locator(govTableLocator);
            await tableLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            var headersLocator = tableLocator.Locator(".govuk-table__head th");
            await headersLocator.First.WaitForAsync();

            var headers = await headersLocator.AllTextContentsAsync();
            var cleanedHeaders = headers.Select(h => h.Trim()).ToList();

            var rowsLocator = tableLocator.Locator(".govuk-table__body .govuk-table__row");
            await rowsLocator.First.WaitForAsync();

            int actualRowCount = await rowsLocator.CountAsync();
            Assert.That(actualRowCount, Is.EqualTo(expectedRows.Count),
                $"Expected {expectedRows.Count} rows in the table, but found {actualRowCount} rows.");

            for (int rowIndex = 0; rowIndex < actualRowCount; rowIndex++)
            {
                var expectedRowData = expectedRows[rowIndex];
                var dataCellsLocator = rowsLocator.Nth(rowIndex).Locator(".govuk-table__cell");

                Assert.That(expectedRowData.Count, Is.EqualTo(cleanedHeaders.Count),
                    $"Row {rowIndex + 1}: Expected {cleanedHeaders.Count} columns, but the expected data dictionary has {expectedRowData.Count} keys.");

                foreach (var expectedKvp in expectedRowData)
                {
                    string headerName = expectedKvp.Key.Trim();
                    string expectedValue = expectedKvp.Value.Trim();

                    int columnIndex = cleanedHeaders.FindIndex(h => h.Contains(headerName));
                    Assert.That(columnIndex, Is.Not.EqualTo(-1),
                        $"Header '{headerName}' not found in the table headers for row {rowIndex + 1}.");

                    var cellLocator = dataCellsLocator.Nth(columnIndex);
                    string actualValue = (await cellLocator.TextContentAsync())?.Trim() ?? string.Empty;

                    Assert.That(actualValue.Trim(), Is.EqualTo(expectedValue),
                        $"Row {rowIndex + 1}, Column '{headerName}': Expected value '{expectedValue}', but found '{actualValue}'.");
                }
            }
        }
    }
}
