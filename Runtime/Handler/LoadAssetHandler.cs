using AceLand.TaskUtils.Models;
using AceLand.TaskUtils.PromiseAwaiter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AceLand.Resources.Handler
{
    public class LoadAssetHandler<T> : PromiseHandler<LoadAssetHandler<T>, T, ErrorMessage>
    {
        public AsyncOperationHandle<T> Handler { get; private set; }
        private readonly AssetReference _reference;

        private LoadAssetHandler(AssetReference reference)
        {
            if (reference == null) return;
            _reference = reference;
            LoadAsset();
        }

        public static LoadAssetHandlerBuilder Builder() => new();

        public class LoadAssetHandlerBuilder
        {
            internal LoadAssetHandlerBuilder() { }
            
            private AssetReference _reference = null;

            public LoadAssetHandler<T> Build() => new(_reference);

            public LoadAssetHandlerBuilder WithReference(AssetReference reference)
            {
                _reference = reference;
                return this;
            }
        }

        private void LoadAsset()
        {
            Handler = Addressables.LoadAssetAsync<T>(_reference);
            Handler.Completed += (handler) =>
            {
                var asset = handler.Result;
                if (asset == null)
                {
                    Debug.LogWarning($"Asset Not Found : {_reference}");
                    var error = ErrorMessage.Builder()
                        .WithMessage("error", "asset not found")
                        .Build();
                    OnError?.Invoke(error);
                }
                else
                {
                    OnSuccess?.Invoke(asset);
                }
                OnFinal?.Invoke();
            };
        }
    }
}
