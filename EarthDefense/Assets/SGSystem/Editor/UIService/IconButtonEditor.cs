using UnityEditor;
using UnityEditor.UI;
using SG.UI;


[CustomEditor(typeof(IconButton))]
[CanEditMultipleObjects]
public class IconButtonEditor : EXButtonEditor
{
    SerializedProperty Icon;
    SerializedProperty Text;

    protected override void OnEnable()
    {
        base.OnEnable();
        Icon = serializedObject.FindProperty("Icon");
        Text = serializedObject.FindProperty("Text");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("IconButton Fields", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Icon);
        EditorGUILayout.PropertyField(Text);

        serializedObject.ApplyModifiedProperties();
    }
}