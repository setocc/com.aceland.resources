using AceLand.Resources.ProjectSetting;
using UnityEditor;
using UnityEngine.UIElements;

namespace AceLand.Resources.Editor.ProjectSetting
{
    public class ResourcesProjectSettingsProvider : SettingsProvider
    {
        public const string SETTINGS_NAME = "Project/AceLand Resources";

        private SerializedObject _settings;
        
        private ResourcesProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.User) 
            : base(path, scope) { }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = ResourcesProjectSettings.GetSerializedSettings();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new ResourcesProjectSettingsProvider(SETTINGS_NAME, SettingsScope.Project);
            
            return provider;
        }

        public override void OnGUI(string searchContext)
        {
            SerializedProperty(out var remoteBundle, out var preloadLabels);
            
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Addressables Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(remoteBundle);
            EditorGUILayout.PropertyField(preloadLabels);
            
            _settings.ApplyModifiedPropertiesWithoutUndo();
        }
        
        private void SerializedProperty(
            out SerializedProperty remoteBundle, out SerializedProperty preloadLabels)
        {
            remoteBundle = _settings.FindProperty("remoteBundle");
            preloadLabels = _settings.FindProperty("preloadLabels");
        }
    }
}