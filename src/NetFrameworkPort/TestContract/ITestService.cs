using System.ServiceModel;
using CoreRpc.Networking.Rpc.ServiceAnnotations;

namespace TestContract
{
    [Service(port: 57001)]
    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        int SetTestData(TestData testData);

        [OperationContract]
        TestData GetTestData();
    }
}