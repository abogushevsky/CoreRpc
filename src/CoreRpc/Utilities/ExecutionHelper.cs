// using System;
//
// namespace CoreRpc.Utilities
// {
//     internal static class ExecutionHelper
//     {
//         public static void WithLock(object locker, Action action)
//         {
//             WithLock(locker, () =>
//             {
//                 action();
//                 return 0;
//             });
//         }
//         
//         public static T WithLock<T>(object locker, Func<T> function)
//         {
//             lock (locker)
//             {
//                 return function();
//             }
//         }
//     }
// }