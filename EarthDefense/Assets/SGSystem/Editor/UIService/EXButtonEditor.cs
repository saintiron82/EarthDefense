using UnityEditor;
using UnityEditor.UI;
using SG.UI;

[CustomEditor(typeof(EXButton))]
[CanEditMultipleObjects]
public class EXButtonEditor : ButtonEditor
{
    SerializedProperty _onPointerDown;
    SerializedProperty _onPointerUp;
    SerializedProperty _onPointerExit;
    SerializedProperty _onEnter;
    SerializedProperty _targetImage;
    SerializedProperty _targetText;
    SerializedProperty _uIColorGuide;
    SerializedProperty _imageSwitcher;

    protected override void OnEnable()
    {
        base.OnEnable();
        _onPointerDown = serializedObject.FindProperty("_onPointerDown");
        _onPointerUp = serializedObject.FindProperty("_onPointerUp");
        _onPointerExit = serializedObject.FindProperty("_onPointerExit");
        _onEnter = serializedObject.FindProperty("_onEnter");
        _targetImage = serializedObject.FindProperty("_targetImage");
        _targetText = serializedObject.FindProperty("_targetText");
        _uIColorGuide = serializedObject.FindProperty("_uIColorGuide");
        _imageSwitcher = serializedObject.FindProperty("_imageSwitcher");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("EXButton Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_onPointerDown);
        EditorGUILayout.PropertyField(_onPointerUp);
        EditorGUILayout.PropertyField(_onPointerExit);
        EditorGUILayout.PropertyField(_onEnter);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("EXButton References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_targetImage);
        EditorGUILayout.PropertyField(_targetText);
        EditorGUILayout.PropertyField(_uIColorGuide);
        EditorGUILayout.PropertyField(_imageSwitcher);

        serializedObject.ApplyModifiedProperties();
    }
}