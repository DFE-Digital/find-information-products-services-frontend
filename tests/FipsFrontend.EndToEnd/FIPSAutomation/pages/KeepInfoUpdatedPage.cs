using Microsoft.Playwright;

namespace FiPSAutomation.Pages
{
    public class KeepInfoUpdatedPage
    {
        private readonly IPage page;

        public KeepInfoUpdatedPage(IPage page)
        {
            this.page = page;
        }


        //public async Task VerifyServiceEmailAsync()
        //{
        //    await Assertions.Expect(ServiceEmailDesc).ToBeVisibleAsync();
        //}
    }
}
