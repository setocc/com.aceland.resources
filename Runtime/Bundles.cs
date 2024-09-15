using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AceLand.Library.Extensions;
using AceLand.Resources.ProjectSetting;
using AceLand.TaskUtils;
using AceLand.TaskUtils.PromiseAwaiter;
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
        private static readonly List<string> RequireUpdateCatalogs = new();
        private static readonly List<string> AssetKeys = new();

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
                    if (AssetKeys.Contains(key.ToString())) continue;
                    AssetKeys.Add(key as string);
                }
                Debug.Log($"Addressable Assets are initialized. {AssetKeys.Count} Assets Keys arranged.");

                CheckCatalog(
                    onFinal: () =>
                    {
                        if (!_settings.remoteBundle || _settings.preloadLabels.Length == 0)
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
                Debug.LogError($"Addressable Assets Initialize Error: {handler.Status}");
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
                        if (RequireUpdateCatalogs.Contains(key.ToString())) continue;
                        RequireUpdateCatalogs.Add(key.ToString());
                    }
                }
                Debug.Log("Catalog update completed");
                Addressables.Release(updateHandle);
                onFinal?.Invoke();
            };
        }

        private static Promise DownloadDependencies(AssetLabelReference[] categories, Action<ProgressData> processAction = null, CancellationTokenSource tokenSource = null)
        {
            var handler = Addressables.GetDownloadSizeAsync(categories);
            AsyncOperationHandle downloadHandler = new();

            return Task.Run(async () =>
                {
                    Debug.Log("Start Download Dependencies");

                    while (!handler.IsDone)
                        Thread.Yield();

                    if (handler.Result <= 0)
                        throw new Exception("No Dependence download");

                    var totalSize = handler.Result.SizeSuffix();
                    Debug.Log($"Dependence download start : {totalSize}");
                    downloadHandler = Addressables.DownloadDependenciesAsync(
                        categories, Addressables.MergeMode.None
                    );

                    if (processAction is not null && tokenSource is not null)
                        await DownloadProgressHandler(downloadHandler, processAction, tokenSource.Token);

                    while (!downloadHandler.IsDone)
                        Thread.Yield();

                    if (tokenSource is not null && tokenSource.IsCancellationRequested)
                        throw new Exception("Dependencies download canceled");

                    if (downloadHandler.Status is not AsyncOperationStatus.Succeeded)
                        throw new Exception("Dependencies download failed");

                    Debug.Log($"Dependence download completed");
                },
                TaskHandler.ApplicationAliveToken
            ).Final(() =>
                {
                    Addressables.Release(handler);
                    Addressables.Release(downloadHandler);
                }
            );
        }

        public static Promise<SceneInstance> LoadScene(string assetKey, LoadSceneMode mode, Action<ProgressData> processAction = null, CancellationTokenSource tokenSource = null)
        {
            var handler = Addressables.LoadSceneAsync(assetKey, mode);
            
            return Task.Run(async () =>
            {
                if (processAction is not null && tokenSource is not null)
                    await DownloadProgressHandler(handler, processAction, tokenSource.Token);
                
                while (!handler.IsDone)
                    Thread.Yield();

                if (handler.Status is not AsyncOperationStatus.Succeeded)
                    throw new Exception($"Load Scene Fail: {assetKey} - {handler.Status}");

                Debug.Log($"Load Scene Success : {assetKey}");
                return handler.Result;
            }).Final(() =>
                Addressables.Release(handler)
            );
        }

        public static Promise UnLoadScene(SceneInstance scene)
        {
            var handler = Addressables.UnloadSceneAsync(scene);
            
            return Task.Run(() =>
            {
                while (!handler.IsDone)
                    Thread.Yield();
                
                if (handler.Status is not AsyncOperationStatus.Succeeded)
                    throw new Exception($"Unload Scene Fail: {scene.Scene.name} - {handler.Status}");
                
                Debug.LogError($"Unload Scene Fail: {scene.Scene.name} - {handler.Status}");
            }).Final(() =>
                Addressables.Release(handler)
            );
        }

        public static Promise<T> LoadAsset<T>(AssetReference assetKey)
        {
            var handler = Addressables.LoadAssetAsync<T>(assetKey);
            
            return Task.Run(() =>
            {
                while (!handler.IsDone)
                    Thread.Yield();

                var asset = handler.Result;
                if (asset is null)
                    throw new Exception($"Asset Not Found : ({typeof(T).Name}) {assetKey}");

                return asset;
            }).Final(() =>
                Addressables.Release(handler)
            );
        }

        public static Promise<GameObject> InstantiateGameObject(AssetReferenceGameObject reference, bool logging = false)
        {
            var handler = Addressables.InstantiateAsync(reference);
            
            return Task.Run(() =>
            {
                while (!handler.IsDone)
                    Thread.Yield();
                
                var go = handler.Result;
                if (go is null)
                    throw new Exception($"Instantiate Asset Fail : {reference}");

                return go;
            }).Final(() =>
                Addressables.Release(handler)
            );
        }

        public static void ReleaseAsset<T>(T asset)
        {
            Addressables.Release(asset);
        }
        
        private static Task DownloadProgressHandler(AsyncOperationHandle handle, Action<ProgressData> processAction, CancellationToken token)
        {
            while (handle.GetDownloadStatus().TotalBytes < 0)
            {
                if (token.IsCancellationRequested) return null;
                Thread.Yield();
            }

            if (!handle.IsValid() || handle.IsDone) return null;
            
            var processData = new ProgressData
            {
                TotalValue = handle.GetDownloadStatus().TotalBytes
            };

            while (handle.Status is AsyncOperationStatus.None)
            {
                if (token.IsCancellationRequested) break;
                
                processData.CurrentValue = handle.GetDownloadStatus().DownloadedBytes;
                processData.IsDone = handle.IsDone;
                processAction.Invoke(processData);

                if (handle.IsDone) break;
                Thread.Yield();
            }

            return null;
        }
    }
}
