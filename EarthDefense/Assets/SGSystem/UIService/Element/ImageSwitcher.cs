using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SG.UI
{
    public class ImageSwitcher : MonoBehaviour
    {
        public Image targetImage;
        [Serializable]
        public class ImageData
        {
            public string Name;
            public Sprite Image;
        }
        public List<ImageData> ImageList = new List<ImageData>();

        public void SwitchImage(string name)
        {
            for (int i = 0; i < ImageList.Count; i++)
            {
                if (ImageList[i].Name == name)
                {
                    var targetImage = ImageList[i].Image;
                    if (targetImage == null)
                    {
                        Debug.LogError("ImageSwitch: " + name + " not found");
                        return;
                    }
                    SetImage(targetImage);
                    break;
                }
            }
        }

        public void SwitchImage( int index)
        {
            if (index < 0 || index >= ImageList.Count)
            {
                Debug.LogError("ImageSwitch: " + index + " out of range");
                return;
            }
            var targetImage = ImageList[index].Image;
            if (targetImage == null)
            {
                Debug.LogError("ImageSwitch: " + index + " not found");
                return;
            }
            SetImage(targetImage);
        }

        public void UpdateImage(string name, Sprite image)
        {
            var findTarget = false;
            for (int i = 0; i < ImageList.Count; i++)
            {
                if (ImageList[i].Name == name)
                {
                    ImageList[i].Image = image;
                    findTarget = true;
                    break;
                }
            }

            if (!findTarget)
            {
                var newImageData = new ImageData();
                newImageData.Name = name;
                newImageData.Image = image;
                ImageList.Add(newImageData);
            }
        }

        public void SetImage(Sprite image)
        {
            if( targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            targetImage.sprite = image;
        }
    }
}