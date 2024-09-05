using System;
using System.Collections.Generic;
using AceLand.Resources.Handler;
using AceLand.Resources.ProjectSetting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace AceLand.Resources
{
    public static class Bundles
    {
        private static ResourcesProjectSettings _settings;
        private static readonly List<string> _updateCatalogs = new();
        private static readonly List<string> _assetKeys = new();

        public static bool Initialized;
        
        internal static void Initialization(ResourcesProjectSettings settings)
        {
            _settings = settings;
            Addressables.InitializeAsync().Completed += HandleInitializeResult;
        }

        private static void HandleInitializeResult(AsyncOperationHandle<IResourceLocator> handler)
        {
            if (handler.Status is AsyncOperationStatus.Succeeded)
            {
                var locator = handler.Result;
                foreach (var key in locator.Keys)
                {
                    if (_assetKeys.Contains(key.ToString())) continue;
                    _assetKeys.Add(key as string);
                }
                Debug.Log($"Addressables is initialized. {_assetKeys.Count} Assets Keys arranged.");

                CheckCatalog(
                    onFinal: () =>
                    {
                        if (!_settings.remoteBundle || _settings.preloadLabels.Count == 0)
                        {
                            Initialized = true;
                            return;
                        }
                        DownloadDependencies(_settings.preloadLabels)
                            .Final(() => Initialized = true);
                    }
                );
            }
            else
            {
                Debug.LogError($"Addressables Initialize Error: {handler.Status}");
            }
        }

        private static void CheckCatalog(Action onFinal = null)
        {
            Debug.Log("Check Update Catalogs");
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            checkHandle.Completed += (handler) =>
            {
                HandleCheckCatalogResult(handler, onFinal);
                Addressables.Release(checkHandle);
            };
        }

        private static void HandleCheckCatalogResult(AsyncOperationHandle<List<string>> handler, Action onFinal)
        {
            if (handler.Status is AsyncOperationStatus.Succeeded && handler.Result.Count > 0)
            {
                UpdateCatalogs(handler, onFinal);
            }
            else
            {
                Debug.Log("no Catalog update");
                onFinal?.Invoke();
            }
        }

        private static void UpdateCatalogs(AsyncOperationHandle<List<string>> handler, Action onFinal)
        {
            var updateHandle = Addressables.UpdateCatalogs(handler.Result, false);
            updateHandle.Completed += (handle) =>
            {
                foreach (var item in handle.Result)
                {
                    foreach (var key in item.Keys)
                    {
                        if (_updateCatalogs.Contains(key.ToString())) continue;
                        _updateCatalogs.Add(key.ToString());
                    }
                }
                Debug.Log("Catalog update completed");
                Addressables.Release(updateHandle);
                onFinal?.Invoke();
            };
        }

        public static DownloadDependenciesHandler DownloadDependencies(IEnumerable<AssetLabelReference> categories)
        {
            return DownloadDependenciesHandler.Builder()
                .WithCategories(categories)
                .Build();
        }

        public static LoadSceneHandler LoadScene(string assetKey, LoadSceneMode mode)
        {
            return LoadSceneHandler.Builder()
                .WithAssetKey(assetKey)
                .WithMode(mode)
                .Build();
        }

        public static UnloadSceneHandler UnLoadScene(SceneInstance scene)
        {
            return UnloadSceneHandler.Builder()
                .WithScene(scene)
                .Build();
        }

        public static LoadAssetHandler<T> LoadAsset<T>(AssetReference assetKey)
        {
            return LoadAssetHandler<T>.Builder()
                .WithReference(assetKey)
                .Build();
        }

        public static InstantiateAssetHandler InstantiateGameObject(AssetReferenceGameObject reference, bool logging = false)
        {
            return InstantiateAssetHandler.Builder()
                .WithReference(reference)
                .WithLogging(logging)
                .Build();
        }

        public static void ReleaseAsset<T>(T asset)
        {
            Addressables.Release(asset);
        }
    }
}
