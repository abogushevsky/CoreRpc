## CoreRpc
A .NET Core library for rpc-style inter-process communication with async methods support.

## Description
The main point of this project is to try to implement some kind of analog of WCF for .NET Core. It allows to declare interface that will be used as service contract and use it on client side in same way if it was in-process object. All you need to do is to mark interface with special attribute, provide its implementation on server side, launch server application with service instance created and generate client and launch client application.
In opposite to WCF where developer had to create client implementation by himself or implement some kind of code-generation, CoreRpc can generate client automatically. It uses reflection and Roslyn API to generate class implementing service interface.

## Example
Declare data contracts. It can be domain model classes as well as DTO, the only requirement is that they have to be serializable:
```C#
[Serializable]
public class SerializableObject
{
  public string StringProperty { get; set; }

  public int IntProperty { get; set; }

  public NestedSerializableObject NestedObject { get; set; }

  public static SerializableObject GetTestInstance() =>
    new SerializableObject
    {
      StringProperty = TestString,
      IntProperty = TestInt,
      NestedObject = new NestedSerializableObject
      {
        DoubleProperty = TestDouble
      }
    };

  public const string TestString = "Test string";
  public const int TestInt = 7;
  public const double TestDouble = Math.PI;
}

[Serializable]
public class NestedSerializableObject 
{
  public double DoubleProperty { get; set; }
}

[Serializable]
public class ImmutableSerializableObject
{
  public ImmutableSerializableObject(string stringProperty, int intProperty, NestedSerializableObject nestedObject)
  {
    StringProperty = stringProperty;
    IntProperty = intProperty;
    NestedObject = nestedObject;
  }

  public string StringProperty { get; }

  public int IntProperty { get; }

  public NestedSerializableObject NestedObject { get; }

  public static ImmutableSerializableObject GetTestInstance() =>
    new ImmutableSerializableObject(
      SerializableObject.TestString, 
      SerializableObject.TestInt, 
      new NestedSerializableObject()
      {
        DoubleProperty = SerializableObject.TestDouble
      });
}
```

Declare interface:
```C#
[Service(port: 57001)]
public interface ITestService
{
    int GetSomeInt(SerializableObject me);

    SerializableObject ConstructObject(int id, string name, double age);

    void VoidMethod(string someString);

    (int count, SerializableObject[] objects) GetTuple(int someInt);
}
```

Declare its implementation on server side:
```C#
public class TestService : ITestService
{
  public int GetSomeInt(SerializableObject me) => me.IntProperty;

  public SerializableObject ConstructObject(int id, string name, double age) =>
    new SerializableObject
    {
      IntProperty = id,
      StringProperty = name,
      NestedObject = new NestedSerializableObject
      {
        DoubleProperty = age
      }
    };

  public void VoidMethod(string someString)
  {
    Console.WriteLine($"Called with string: {someString}");
  }

  public (int count, SerializableObject[] objects) GetTuple(int someInt)
  {
    return (someInt + count, new[] { SerializableObject.GetTestInstance() });
  }
}
```

Then we have to create RpcTcpServicePublisher and publish our service:
```C#
var logger = // provide some implementation of CoreRpc.Logging.ILogger;
var serializerFactory = new MessagePackSerializerFactory(); // or any other implementation of ISerializerFactory
var servicePublisher = new RpcTcpServicePublisher(serializerFactory, logger);
var serviceInstance = new TestService();

var publishedService = servicePublisher.PublishUnsecured(testService, serviceShutdownTimeout: TimeSpan.FromMinutes(1)); // use PublishSecured() for SSL commnications.
// hold reference while application is working and dispose before shutdown
publishedService.Dispose();
```

Generate client (note that implementation of ISerializerFactory on client and server side should be same):
```C#
using (var serviceClient = ServiceClientFactory.CreateServiceClient<ITestService>("<service address>", logger, serializerFactory)) // use CreateSecuredServiceClient for SSL communications.
{
    // use serviceClient.ServiceInstance
}
```

## Releases
Unfortunately, project is not production ready. There are many things to be done.
