using System.Collections.Generic;
using System.Threading.Tasks;
using CoreRpc.Networking.Rpc.ServiceAnnotations;

namespace CoreRpc.UnitTests.TestData
{
    [Service(port: 57001)]
    public interface ITestService
    {
        /// <summary>
        /// Operation code 0
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        int GetHashCodeOfMe(SerializableObject me);
        
        /// <summary>
        /// Operation code 1
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        SerializableObject ConstructObject(int id, string name, double age);

        /// <summary>
        /// Operation code 2
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        void VoidMethod(string someString);

        /// <summary>
        /// Operation code 3
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
	    (int count, SerializableObject[] objects) GetObjects(int offset, int count);
        
        /// <summary>
        /// Operation code 4
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        Task<int> GetHashCodeOfMeAsync(SerializableObject me);

        /// <summary>
        /// Operation code 5
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        Task<SerializableObject> ConstructObjectAsync(int id, string name, double age);
        
        /// <summary>
        /// Operation code 6
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        Task<(int count, SerializableObject[] objects)> GetObjectsAsync(int offset, int count);

        /// <summary>
        /// Operation code 7
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        Task VoidMethodAsync(string someString);

        Task<List<SerializableObject>> ConstructObjectsListAsync(int[] ids, string[] names, double[] ages);
    }
}