using Apps.XTM.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;
using XTMTests.Base;

namespace Tests.XTM
{
    [TestClass]
    public class DataHandler : TestBase
    {
        [TestMethod]
        public async Task CustomerHandler_IsSSuccess()
        {
            var handler = new CustomerDataHandler(InvocationContext);

            var result = await handler.GetDataAsync(new DataSourceContext { SearchString = "Webtoon" }, CancellationToken.None);

            foreach (var item in result)
            {
                Console.WriteLine($"{item.Value} - {item.Key}");
            }

            Assert.IsNotNull(result);
        }
    }
}
