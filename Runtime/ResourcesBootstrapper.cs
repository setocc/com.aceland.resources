using AceLand.Resources.ProjectSetting;
using UnityEngine;

namespace AceLand.Resources
{
    internal static class ResourcesBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialization()
        {
            var settings = UnityEngine.Resources.Load<ResourcesProjectSettings>(nameof(ResourcesProjectSettings));
            
            Bundles.Initialization(settings);
        }
    }
}