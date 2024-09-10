using System;
using System.Collections.Generic;
using System.Threading;
using AceLand.Library.Extensions;
using AceLand.TaskUtils.Models;
using AceLand.TaskUtils.PromiseAwaiter;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AceLand.Resources.Handler
{
    public sealed class DownloadDependenciesHandler : ProgressPromiseHandler<DownloadDependenciesHandler, bool, ErrorMessage>
    {
        private Action<long> OnHasDownload { get; set; } = null;
        private readonly IEnumerable<AssetLabelReference> _categories = null;
        private AsyncOperationHandle<long> handler;
        private CancellationTokenSource tokenSource;

        private DownloadDependenciesHandler(IEnumerable<AssetLabelReference> categories)
        {
            if (categories == null) return;
            this._categories = categories;
            DownloadDependencies();
        }

        public static DownloadDependenciesHandlerBuilder Builder() => new();

        public class DownloadDependenciesHandlerBuilder
        {
            internal DownloadDependenciesHandlerBuilder() { }
            
            private IEnumerable<AssetLabelReference> categories = null;

            public DownloadDependenciesHandler Build() => new(categories);

            public DownloadDependenciesHandlerBuilder WithCategories(IEnumerable<AssetLabelReference> categories)
            {
                this.categories = categories;
                return this;
            }
        }

        public DownloadDependenciesHandler BeforeDownload(Action<long> beforeDownload)
        {
            OnHasDownload += beforeDownload;
            return this;
        }

        public override DownloadDependenciesHandler Progress(Action<ProgressData> inProgress)
        {
            base.Progress(inProgress);
            ProgressData = new(-1, 0);
            return this;
        }

        private void DownloadDependencies()
        {
            Debug.Log("Start Download Dependencies");
            Final(() => Addressables.Release(handler));
            handler = Addressables.GetDownloadSizeAsync(_categories);
            handler.Completed += (handle) =>
            {
                if (handle.Result > 0)
                {
                    var totalSize = handler.Result.SizeSuffix(2);
                    Debug.Log($"Dependence download start : {totalSize}");
                    OnHasDownload?.Invoke(handler.Result);
                    var downloadHandler = Addressables.DownloadDependenciesAsync(_categories, Addressables.MergeMode.None, false);
                    Final(() => Addressables.Release(downloadHandler));
                    OnDownload(downloadHandler);
                    downloadHandler.Completed += (handle) =>
                    {
                        StopDownloadCorou();
                        if (handle.Status is AsyncOperationStatus.Succeeded)
                        {
                            Debug.Log($"Dependence download completed");
                            OnSuccess?.Invoke(true);
                        }
                        else
                        {
                            Debug.LogError($"Dependence download failed");
                            var error = ErrorMessage.Builder()
                                .WithMessage("dependence", "download failed")
                                .Build();
                            OnError?.Invoke(error);
                        }
                        OnFinal?.Invoke();
                    };
                }
                else
                {
                    Debug.Log("No Dependence download");
                    OnSuccess?.Invoke(false);
                    OnFinal?.Invoke();
                }
            };
        }

        private void StopDownloadCorou()
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
        }

        private void OnDownload(AsyncOperationHandle handle)
        {
            if (InProgress == null) return;
            tokenSource = new();
            DownloadUiHandler(handle, tokenSource.Token);
        }

        private async void DownloadUiHandler(AsyncOperationHandle handle, CancellationToken token)
        {
            while (!token.IsCancellationRequested && handle.IsValid() && handle.Status is AsyncOperationStatus.None)
            {
                var totalBytes = handle.GetDownloadStatus().TotalBytes;
                var downloadedBytes = handle.GetDownloadStatus().DownloadedBytes;
                if (totalBytes < 0)
                {
                    try { await UniTask.Yield(token, true); }
                    catch { break; }
                }

                ProgressData.TotalValue = totalBytes;
                ProgressData.CurrentValue = downloadedBytes;
                ProgressData.IsDone = handle.IsDone;
                InProgress.Invoke(ProgressData);
                if (handle.IsDone || token.IsCancellationRequested) break;
                await UniTask.Yield();
            }
        }
    }
}