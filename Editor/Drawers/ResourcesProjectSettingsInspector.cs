using AceLand.Resources.ProjectSetting;
using AceLand.Library.Editor;
using UnityEditor;

namespace AceLand.Resources.Editor.Drawers
{
    [CustomEditor(typeof(ResourcesProjectSettings))]
    public class ResourcesProjectSettingsInspector : UnityEditor.Editor
    {        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorHelper.DrawAllPropertiesAsDisabled(serializedObject);
        }
    }
}