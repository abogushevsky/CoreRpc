using System.Diagnostics.CodeAnalysis;
using CoreRpc.Networking.Rpc.ServiceAnnotations;

namespace CoreRpc.UnitTests.TestData
{
    [Service(port: 57001)]
    public interface ITestService
    {
        int GetHashCodeOfMe(SerializableObject me);

        SerializableObject ConstructObject(int id, string name, double age);

        void VoidMethod(string someString);

	    (int count, SerializableObject[] objects) GetObjects(int offset, int count);
    }
}