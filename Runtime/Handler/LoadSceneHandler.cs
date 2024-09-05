using System;
using System.Threading;
using AceLand.Library.Extensions;
using AceLand.TasksUtils.Models;
using AceLand.TasksUtils.Promise;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace AceLand.Resources.Handler
{
    public sealed class LoadSceneHandler : ProgressPromiseHandler<LoadSceneHandler, SceneInstance, ErrorMessage>
    {
        public SceneInstance SceneInstance { get; private set; }

        private Action<long> OnHasDownload { get; set; } = null;
        private readonly string assetKey;
        private readonly LoadSceneMode mode;
        private AsyncOperationHandle<SceneInstance> handler;
        private CancellationTokenSource tokenSource;

        private LoadSceneHandler(string assetKey, LoadSceneMode mode)
        {
            if (assetKey.IsNullOrEmptyOrWhiteSpace()) return;
            this.assetKey = assetKey;
            this.mode = mode;
            LoadScene();
        }

        public static LoadSceneHandlerBuilder Builder() => new();

        public class LoadSceneHandlerBuilder
        {
            internal LoadSceneHandlerBuilder() { }
            
            private string assetKey = string.Empty;
            private LoadSceneMode mode = LoadSceneMode.Single;

            public LoadSceneHandler Build() => new(assetKey, mode);

            public LoadSceneHandlerBuilder WithAssetKey(string assetKey)
            {
                this.assetKey = assetKey;
                return this;
            }

            public LoadSceneHandlerBuilder WithMode(LoadSceneMode sceneMode)
            {
                this.mode = sceneMode;
                return this;
            }
        }

        public LoadSceneHandler BeforeDownload(Action<long> beforeDownload)
        {
            OnHasDownload += beforeDownload;
            return this;
        }

        public override LoadSceneHandler Progress(Action<ProgressData> inProgress)
        {
            base.Progress(inProgress);
            ProgressData = new(-1, 0);
            return this;
        }

        private void LoadScene()
        {
            Debug.Log($"Start Load Scene : {assetKey} in {mode}");
            Final(() => Addressables.Release(handler));
            handler = Addressables.LoadSceneAsync(assetKey, mode);
            OnHasDownload?.Invoke(handler.GetDownloadStatus().TotalBytes);
            OnDownload(handler);
            handler.Completed += (handle) =>
            {
                StopDownloadCoroutine();
                if (handle.Status is AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"Load Scene Success : {assetKey}");
                    SceneInstance = handle.Result;
                    OnSuccess?.Invoke(SceneInstance);
                }
                else
                {
                    Debug.LogError($"Load Scene Fail: {assetKey} - {handle.Status}");
                    var error = ErrorMessage.Builder()
                        .WithMessage("error", handle.Status.ToString())
                        .Build();
                    OnError?.Invoke(error);
                }
                OnFinal?.Invoke();
            };
        }

        private void StopDownloadCoroutine()
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
        }

        private void OnDownload(AsyncOperationHandle<SceneInstance> handle)
        {
            if (InProgress == null) return;
            tokenSource = new();
            DownloadUiHandler(handle, tokenSource.Token);
        }

        private async void DownloadUiHandler(AsyncOperationHandle<SceneInstance> handle, CancellationToken token)
        {
            while (!token.IsCancellationRequested && handle.Status is AsyncOperationStatus.None)
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
                if (handler.IsDone) break;
                await UniTask.Yield(token);
            }
        }
    }
}
