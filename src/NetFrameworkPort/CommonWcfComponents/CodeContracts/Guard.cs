using System;

namespace CommonWcfComponents.CodeContracts
{
    public static class Guard
    {
        public static void CheckNotNull<T>(T item, string name) where T : class
        {
            if (item == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}