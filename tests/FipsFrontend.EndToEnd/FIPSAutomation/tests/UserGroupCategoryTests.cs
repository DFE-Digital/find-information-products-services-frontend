using AventStack.ExtentReports;
using DocumentFormat.OpenXml.Spreadsheet;
using find_information_products_services_tests.utilities;
using FiPSAutomation.Pages;
using Microsoft.Playwright;
using static find_information_products_services_tests.utilities.ExcelReader;

namespace FiPSAutomation
{
    [TestFixture, Order(7)]
    [Category("Functional")]
    public class UserGroupCategoryTests : BaseTest
    {
        private CategoryDetailPage categoryDetailPage = null!;

        [OneTimeSetUp]
        public void InitPages()
        {
            categoryDetailPage = new CategoryDetailPage(Page);
        }

        [Test, Order(1)]
        public async Task VerifyUserGroupCategoryListUS30AC()
        {
            await NavigateToAsync("categories/user-group");
            await categoryDetailPage.VerifyDescriptionAsync("The users of the product or service.");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Adult learner (18+)");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(1)).ToHaveTextAsync("4 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Careers adviser or work coach");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(2)).ToHaveTextAsync("3 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Child or young person");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(3)).ToHaveTextAsync("5 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Department for Education workforce");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(4)).ToHaveTextAsync("4 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Education provider and early years workforce");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(5)).ToHaveTextAsync("7 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Employer");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(6)).ToHaveTextAsync("3 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Local authority workforce");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(7)).ToHaveTextAsync("5 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("NEET or career seeker");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(8)).ToHaveTextAsync("5 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Parent or carer");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(9)).ToHaveTextAsync("8 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Professional external user of DfE data");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(10)).ToHaveTextAsync("6 sub-categories");

            await categoryDetailPage.VerifySubcategoryLinkAsync("Social care workforce");
            await Assertions.Expect(categoryDetailPage.GetSubcategoryDescription(11)).ToHaveTextAsync("8 sub-categories");

            ExtentTest?.Log(Status.Pass, "VerifyUserGroupCategoryListUS30AC passed");
        }

        [Test, Order(2)]
        public async Task VerifyUGAdultLearner18SubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_AdultLearner18_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Adult learner (18+)");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 3; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGAdultLearner18SubcategoryListUS30AC passed");
            }
        }

        [Test, Order(3)]
        public async Task VerifyUGCareersAdviserOrWorkCoachSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_CareersAdviser_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Careers adviser or work coach");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGCareersAdviserOrWorkCoachSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(4)]
        public async Task VerifyUGChildOrYoungPersonSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_ChildOrYoungPers_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Child or young person");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGChildOrYoungPersonSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(5)]
        public async Task VerifyUGDfEWorkforceSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_DfEWorkforce_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Department for Education workforce");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGDfEWorkforceSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(6)]
        public async Task VerifyUGEPAndEYWorkforceSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_EPandEYWorkforce_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Education provider and early years workforce");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGEPAndEYWorkforceSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(7)]
        public async Task VerifyUGEmployerSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_Employer_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Employer");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGEmployerSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(8)]
        public async Task VerifyUGLocalAuthorityWorkforceSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_LAWorkforce_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Local authority workforce");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGLocalAuthorityWorkforceSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(9)]
        public async Task VerifyUGNEETOrCareerSeekerSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_NEETOrCareerSeek_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "NEET or career seeker");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGNEETOrCareerSeekerSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(10)]
        public async Task VerifyUGParentOrCarerSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_ParentOrCarer_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Parent or carer");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGParentOrCarerSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(11)]
        public async Task VerifyUGProfExternalUserofDfEDataSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_ProfExtUserofDfE_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Professional external user of DfE data");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGProfExternalUserofDfEDataSubcategoryListUS30AC passed");
            }
        }

        [Test, Order(12)]
        public async Task VerifyUGSocialCareWorkforceSubcategoryListUS30AC()
        {
            List<SheetRow> dataRows = ExcelReader.getCategoryRowsFromExcelFileBySheetName("testdata.xlsx", "usergroup_SocialCWorkforce_list");
            if (dataRows.Count > 1)
            {
                await NavigateToAsync(dataRows[0].Col1);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[0].Col2, "Social care workforce");
                await categoryDetailPage.VerifyDescriptionAsync(dataRows[0].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[1].Col2, dataRows[1].Col3);
                await categoryDetailPage.VerifySubcategoryDescriptionAsync(dataRows[2].Col2, dataRows[2].Col3);

                for (int i = 1; i < dataRows.Count; i++)
                {
                    if (dataRows[i].Col1 != "")
                    {
                        await categoryDetailPage.VerifySubcategoryLinkAsync(dataRows[i].Col1);
                    }
                }
                ExtentTest?.Log(Status.Pass, "VerifyUGSocialCareWorkforceSubcategoryListUS30AC passed");
            }
        }
    }
}
