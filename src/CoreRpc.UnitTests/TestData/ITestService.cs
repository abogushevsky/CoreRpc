using System.Diagnostics.CodeAnalysis;
using CoreRpc.Networking.Rpc.ServiceAnnotations;

namespace CoreRpc.UnitTests.TestData
{
    [Service(TestService.ServicePort)]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Using in ExpressionTree")]
    public interface ITestService
    {
        int GetHashCodeOfMe(SerializableObject me);

        SerializableObject ConstructObject(int id, string name, double age);

        void VoidMethod(string someString);

	    (int count, SerializableObject[] objects) GetObjects(int offset, int count);
    }
}