using AceLand.Library.ProjectSetting;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace AceLand.Resources.ProjectSetting
{
    public sealed class ResourcesProjectSettings : ProjectSettings<ResourcesProjectSettings>
    {
        public bool remoteBundle = false;
        public List<AssetLabelReference> preloadLabels;
    }
}
