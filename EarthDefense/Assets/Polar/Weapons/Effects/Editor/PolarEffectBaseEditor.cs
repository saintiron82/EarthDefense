using UnityEngine;
using UnityEditor;

namespace Polar.Weapons.Effects.Editor
{
    /// <summary>
    /// PolarEffectBase용 Custom Inspector - JSON 미리보기 및 편집
    /// </summary>
    [CustomEditor(typeof(PolarEffectBase), true)]
    public class PolarEffectBaseEditor : UnityEditor.Editor
    {
        private bool showJson = false;
        private string jsonText = "";
        private Vector2 scrollPos;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("JSON 데이터", EditorStyles.boldLabel);

            showJson = EditorGUILayout.Foldout(showJson, "JSON 보기/편집", true);

            if (showJson)
            {
                var effect = target as PolarEffectBase;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // JSON 내보내기
                if (GUILayout.Button("현재 데이터 → JSON 생성"))
                {
                    jsonText = effect.ToJson(true);
                    GUI.FocusControl(null);
                }

                EditorGUILayout.Space(5);

                // JSON 텍스트 에리어
                EditorGUILayout.LabelField("JSON:", EditorStyles.miniBoldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();

                // JSON 가져오기
                if (GUILayout.Button("JSON → 데이터 적용", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("데이터 덮어쓰기",
                        "현재 설정을 JSON으로 덮어씁니다. 계속하시겠습니까?",
                        "적용", "취소"))
                    {
                        Undo.RecordObject(effect, "Apply JSON to Effect");
                        try
                        {
                            effect.FromJson(jsonText);
                            EditorUtility.SetDirty(effect);
                            Debug.Log($"[EffectEditor] JSON 적용 완료: {effect.name}");
                        }
                        catch (System.Exception e)
                        {
                            EditorUtility.DisplayDialog("JSON 파싱 에러", e.Message, "OK");
                        }
                    }
                }

                // 클립보드 복사
                if (GUILayout.Button("클립보드 복사", GUILayout.Height(25)))
                {
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        jsonText = effect.ToJson(true);
                    }
                    EditorGUIUtility.systemCopyBuffer = jsonText;
                    Debug.Log("[EffectEditor] JSON을 클립보드에 복사했습니다");
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 빠른 내보내기
                if (GUILayout.Button("파일로 내보내기", GUILayout.Height(25)))
                {
                    string path = EditorUtility.SaveFilePanel(
                        "Export Effect JSON",
                        "Assets/Polar/Data/Effects/Exported/",
                        $"{effect.EffectId}_{effect.name}.json",
                        "json");

                    if (!string.IsNullOrEmpty(path))
                    {
                        string json = effect.ToJson(true);
                        System.IO.File.WriteAllText(path, json, System.Text.Encoding.UTF8);
                        AssetDatabase.Refresh();
                        Debug.Log($"[EffectEditor] 내보내기 완료: {path}");
                        EditorUtility.DisplayDialog("내보내기 완료", $"저장됨:\n{path}", "OK");
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
            
            // 빠른 정보
            var effectBase = target as PolarEffectBase;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Effect 정보", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ID:", effectBase.EffectId);
            EditorGUILayout.LabelField("Name:", effectBase.EffectName);
            EditorGUILayout.LabelField("Type:", effectBase.GetType().Name);
            EditorGUILayout.EndVertical();
        }
    }
}

