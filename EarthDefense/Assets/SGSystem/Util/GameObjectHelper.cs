using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameObjectHelper
{
    public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

    private static readonly Queue<Transform> _traversalQueue = new();
    private static readonly Dictionary<string, MonoBehaviour> _cachedComponentDic = new();

    private static T GetComponentWithTag<T>(string tag) where T : MonoBehaviour
    {
        T FindWithTag(string tag)
        {
            var objects = UnityEngine.GameObject.FindGameObjectsWithTag(tag);
            if (objects.FirstOrDefault(t => t.name == typeof(T).Name) is var obj && obj != default)
            {
                return obj.GetComponent<T>();
            }

            return default;
        }

        var key = typeof(T).Name;
        if (_cachedComponentDic.TryGetValue(key, out var component))
        {
            if (component != default)
            {
                return (T)component;
            }

            if (FindWithTag(tag) is var findComponent && findComponent != default)
            {
                _cachedComponentDic[key] = findComponent;
                return findComponent;
            }
        }
        else
        {
            if (FindWithTag(tag) is var findComponent && findComponent != default)
            {
                _cachedComponentDic[key] = findComponent;
                return findComponent;
            }
        }

        return default;
    }

    public static IEnumerable<T> FindAll<T>(this Transform self) where T : Component
    {
        return self.gameObject.GetComponentsInChildren<T>(true);
    }

    public static IEnumerable<T> FindAll<T>(this Transform self, string name) where T : Component
    {
        return self.FindAll<T>().Where(t => t.name == name);
    }

    public static GameObject CTFind(this GameObject go, string name)
    {
        if (go == null)
            throw new System.ArgumentNullException(nameof(go));

        return go.transform.CTFind(name).gameObject;
    }

    public static Transform CTFind(this Transform transform, string name)
    {
        if (transform == null)
            throw new System.ArgumentNullException(nameof(transform));

        if (name == null)
            throw new System.ArgumentNullException(nameof(name));

        return deepSearch(transform, name);
    }


    private static Transform deepSearch(Transform parent, string name)
    {
        Transform tf = parent.Find(name);

        if (tf != null)
            return tf;

        foreach (Transform child in parent)
        {
            tf = deepSearch(child, name);
            if (tf != null)
                return tf;
        }

        return null;
    }

    public static T Find<T>(this Transform self, string name) where T : Component
    {
        _traversalQueue.Clear();
        _traversalQueue.Enqueue(self);
        name = name.Trim();
        while (_traversalQueue.Count > 0)
        {
            var x = _traversalQueue.Dequeue();
            for (var i = 0; i < x.childCount; i++)
            {
                var child = x.GetChild(i);
                if (child.name == name && child.GetComponent<T>() != null)
                {
                    return child.GetComponent<T>();
                }

                if (child.childCount > 0)
                {
                    _traversalQueue.Enqueue(child);
                }
            }
        }

        return null;
    }

    public static void DontDestroyOnLoad2(this UnityEngine.GameObject self)
    {
        if (self.transform.parent != null)
        {
            self.transform.SetParent(null, true);
        }

        Object.DontDestroyOnLoad(self);
    }

    public static void MakeDestroyOnLoad(this UnityEngine.GameObject self)
    {
        SceneManager.MoveGameObjectToScene(self, SceneManager.GetActiveScene());
    }

    public static void ChangeLayerRecursive(this UnityEngine.GameObject go, string name)
    {
        var layer = LayerMask.NameToLayer(name);
        ChangeLayerRecursive(go, layer);
    }

    public static void ChangeLayerRecursive(this UnityEngine.GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            ChangeLayerRecursive(child.gameObject, layer);
        }
    }

    public static T Assert<T>(this T target) where T : Object
    {
        Debug.Assert(target != default);
        return target;
    }

    public static void SetActive(this MonoBehaviour behaviour, bool active)
    {
        behaviour?.gameObject?.SetActive(active);
    }

    public static bool ActiveSelf(this MonoBehaviour behaviour)
    {
        if(behaviour != null)
        {
            return behaviour.gameObject.activeSelf;
        }

        return false;
    }

    public static bool ActiveInHierarchy(this MonoBehaviour behaviour)
    {
        if(behaviour != null)
        {
            return behaviour.gameObject.activeInHierarchy;
        }

        return false;
    }

    public static void SetEnabled<T>(this IEnumerable<UnityEngine.GameObject> gameObjects, bool active) where T : MonoBehaviour
    {
        foreach (var target in gameObjects.SelectMany(t => t.transform.FindAll<T>()))
        {
            target.enabled = active;
        }
    }

    public static void Do<T>(this UnityEngine.GameObject gameObject, Action<T> action) where T : MonoBehaviour
    {
        foreach (var target in gameObject.transform.FindAll<T>())
        {
            action?.Invoke(target);
        }
    }

    public static void Do<T>(this IEnumerable<UnityEngine.GameObject> gameObjects, Action<T> action) where T : MonoBehaviour
    {
        foreach (var target in gameObjects.SelectMany(t => t.transform.FindAll<T>()))
        {
            action?.Invoke(target);
        }
    }

    public static T GetOrAddComponent<T>(this Transform self) where T : Component
    {
        var component = self.gameObject.GetComponent<T>();
        if (component == null)
        {
            component = self.gameObject.AddComponent<T>();
        }

        return component;
    }

    public static bool IsNull(this Object obj)
    {
        return ReferenceEquals(obj, null);
    }

    // UnityObject 는 Destroy 후 명시적으로 null 해주지 않으면 가비지 컬렉터 수집 전에는 값이 남아있음.
    public static bool IsFakeNull(this Object obj)
    {
        return !ReferenceEquals(obj, null) && obj;
    }

    public static bool IsAssigned(this Object obj)
    {
        return obj;
    }

    public static T GetManager<T>() where T : MonoBehaviour
    {
        return GetComponentWithTag<T>("Manager");
    }

    public static T GetOrAddComponent<T>(this UnityEngine.GameObject child) where T : Component
    {
        var result = child.GetComponent<T>();
        if (result == null)
        {
            result = child.AddComponent<T>();
        }

        return result;
    }

    public static bool HasTag(this RaycastHit rayCastHit, string[] tagList)
    {
        for (var index = 0; index < tagList.Length; ++index)
        {
            if (rayCastHit.collider.CompareTag(tagList[index]))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetSize(this RectTransform rectTransform, float x, float y)
    {
        rectTransform.sizeDelta = new Vector2(x, y);
    }

    public static void SetSize(this RectTransform rectTransform, RectTransform targetRectTransform)
    {
        rectTransform.sizeDelta = targetRectTransform.sizeDelta;
    }

    public static void SetWidthSize(this RectTransform rectTransform, float width)
    {
        rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
    }

    public static void SetHeightSize(this RectTransform rectTransform, float height)
    {
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
    }

    public static float GetHeight(this RectTransform rectTransform)
    {
        return rectTransform.rect.height;
    }

    public static float GetWidth(this RectTransform rectTransform)
    {
        return rectTransform.rect.width;
    }
}