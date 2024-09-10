using AceLand.TaskUtils.Models;
using AceLand.TaskUtils.PromiseAwaiter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AceLand.Resources.Handler
{
    public class InstantiateAssetHandler : PromiseHandler<InstantiateAssetHandler, GameObject, ErrorMessage>
    {
        public AsyncOperationHandle<GameObject> Handler { get; private set; }
        private readonly AssetReferenceGameObject reference;
        private readonly bool logging;

        private InstantiateAssetHandler(AssetReferenceGameObject reference, bool logging)
        {
            if (reference == null) return;
            this.reference = reference;
            this.logging = logging;
            LoadAsset();
        }

        public static InstantiateAssetHandlerBuilder Builder() => new();

        public class InstantiateAssetHandlerBuilder
        {
            internal InstantiateAssetHandlerBuilder() { }
            
            private AssetReferenceGameObject reference = null;
            private bool logging = false;

            public InstantiateAssetHandler Build() => new(reference, logging);

            public InstantiateAssetHandlerBuilder WithReference(AssetReferenceGameObject assetReference)
            {
                this.reference = assetReference;
                return this;
            }

            public InstantiateAssetHandlerBuilder WithLogging(bool logging)
            {
                this.logging = logging;
                return this;
            }
        }

        private void LoadAsset()
        {
            if (logging) Debug.Log($"Start Instantiate Asset : {reference}");
            Final(() => Addressables.Release(Handler));
            Handler = Addressables.LoadAssetAsync<GameObject>(reference);
            Handler.Completed += handle =>
            {
                var asset = handle.Result;
                if (asset == null)
                {
                    Debug.LogWarning($"Asset Not Found : {reference}");
                    var error = ErrorMessage.Builder()
                        .WithMessage("error", "asset not found")
                        .Build();
                    OnError?.Invoke(error);
                    OnFinal?.Invoke();
                }
                else
                {
                    Addressables.InstantiateAsync(reference).Completed += handle =>
                    {
                        var go = handle.Result;
                        if (go == null)
                        {
                            Debug.LogWarning($"Instantiate Asset Fail : {reference}");
                            var error = ErrorMessage.Builder()
                                .WithMessage("error", "asset not found")
                                .Build();
                            OnError?.Invoke(error);
                        }
                        else
                        {
                            if (logging) Debug.Log($"Instantiate Asset Success : {reference} {go.name}");
                            OnSuccess?.Invoke(go);
                        }
                        OnFinal?.Invoke();
                    };
                }
            };
        }
    }
}
