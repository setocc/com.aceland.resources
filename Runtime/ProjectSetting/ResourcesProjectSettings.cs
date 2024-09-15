using AceLand.Library.ProjectSetting;
using UnityEngine.AddressableAssets;

namespace AceLand.Resources.ProjectSetting
{
    public sealed class ResourcesProjectSettings : ProjectSettings<ResourcesProjectSettings>
    {
        public bool remoteBundle = false;
        public AssetLabelReference[] preloadLabels;
    }
}
