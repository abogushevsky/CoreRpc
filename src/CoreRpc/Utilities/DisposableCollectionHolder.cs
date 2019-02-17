using System;

namespace CoreRpc.Utilities
{
	public sealed class DisposableCollectionHolder : IDisposable
	{
		public DisposableCollectionHolder(params IDisposable[] disposableObjects)
		{
			_disposableObjects = disposableObjects;
		}

		public void Dispose()
		{
			_disposableObjects.ForEach(disposable => disposable.Dispose());
		}

		private readonly IDisposable[] _disposableObjects;
	}
}