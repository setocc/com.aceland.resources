using AceLand.TaskUtils.Models;
using AceLand.TaskUtils.PromiseAwaiter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace AceLand.Resources.Handler
{
    public class UnloadSceneHandler : PromiseHandler<UnloadSceneHandler, ErrorMessage>
    {
        private readonly SceneInstance scene;
        private AsyncOperationHandle<SceneInstance> handler;

        private UnloadSceneHandler(SceneInstance scene)
        {
            this.scene = scene;
            UnloadScene();
        }

        public static UnloadSceneHandlerBuilder Builder() => new();

        public class UnloadSceneHandlerBuilder
        {
            internal UnloadSceneHandlerBuilder() { }
            
            private SceneInstance scene;

            public UnloadSceneHandler Build() => new(scene);

            public UnloadSceneHandlerBuilder WithScene(SceneInstance scene)
            {
                this.scene = scene;
                return this;
            }
        }

        private void UnloadScene()
        {
            Debug.Log($"Start Unload Scene : {scene.Scene.name}");
            Final(() => Addressables.Release(handler));
            handler = Addressables.UnloadSceneAsync(scene);
            handler.Completed += (handle) =>
            {
                if (handle.Status is AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"Unload Scene Success : {scene.Scene.name}");
                    OnSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"Unload Scene Fail: {scene.Scene.name} - {handle.Status}");
                    var error = ErrorMessage.Builder()
                        .WithMessage("error", handle.Status.ToString())
                        .Build();
                    OnError?.Invoke(error);
                }
                OnFinal?.Invoke();
            };
        }
    }
}
