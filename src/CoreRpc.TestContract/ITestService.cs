using System.Threading.Tasks;
using CoreRpc.Networking.Rpc.ServiceAnnotations;

namespace CoreRpc.TestContract
{
    [Service(port: 57001)]
    public interface ITestService
    {
        int SetTestData(TestData testData);

        TestData GetTestData();

        Task<TestData> GetTestDataAsync();

        Task TestAsync();
    }
}