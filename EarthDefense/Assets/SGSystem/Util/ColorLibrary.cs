using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ColorData
{
    public string name;
    public Color color;
}
[CreateAssetMenu( fileName = "ColorLibrary", menuName = "CatLibrary/Color Library", order = 2 )]
public class ColorLibrary : ScriptableObject
{
    public List<ColorData> colorList = new List<ColorData>();

    public Color GetColorByName(string name)
    {
        foreach (var colorData in colorList)
        {
            if (colorData.name == name)
            {
                return colorData.color;
            }
        }
        Debug.LogWarning($"Color '{name}' not found in ColorLibrary.");
        return Color.white; // Default color if not found
    }
}
