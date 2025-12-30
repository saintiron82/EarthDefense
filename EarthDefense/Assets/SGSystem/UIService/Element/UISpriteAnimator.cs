using UnityEngine;
using UnityEngine.UI;

namespace SG.UI
{
    /// <summary>
    /// UISpriteAnimator is a simple sprite animator for Unity UI Image components.
    /// It cycles through an array of sprites at a specified frame rate.
    /// </summary>
    [RequireComponent( typeof( Image ) )]
    public class UISpriteAnimator : MonoBehaviour
    {
        public Image targetImage;
        public Sprite[] frames;
        public float frameRate = 0.1f;

        private int currentFrame = 0;
        private float timer = 0f;

        void Update()
        {
            if( frames.Length == 0 ) return;

            timer += Time.deltaTime;
            if( timer >= frameRate )
            {
                timer = 0f;
                currentFrame = (currentFrame + 1) % frames.Length;
                targetImage.sprite = frames[currentFrame];
            }
        }
    }
}