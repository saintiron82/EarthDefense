using UnityEngine;

namespace ShapeDefense.Scripts.Weapons
{
    /// <summary>
    /// 폭발 이펙트 (간단한 원형 확산 애니메이션)
    /// </summary>
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private float maxScale = 2f;
        
        private float _startTime;
        private Vector3 _initialScale;
        private Color _initialColor;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                CreateCircleSprite();
            }

            _initialScale = transform.localScale;
            _initialColor = explosionColor;
        }

        private void OnEnable()
        {
            _startTime = Time.time;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _initialColor;
            }
        }

        private void Update()
        {
            float elapsed = Time.time - _startTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            // 스케일 애니메이션
            float scaleValue = scaleCurve.Evaluate(t) * maxScale;
            transform.localScale = _initialScale * scaleValue;

            // 알파 애니메이션
            if (spriteRenderer != null)
            {
                float alphaValue = alphaCurve.Evaluate(t);
                Color color = _initialColor;
                color.a = alphaValue;
                spriteRenderer.color = color;
            }
        }

        private void CreateCircleSprite()
        {
            // 간단한 원형 스프라이트 생성
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);
            
            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    // 부드러운 경계를 위한 그라디언트
                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    alpha = Mathf.Pow(alpha, 2f); // 더 부드러운 페이드
                    
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution / 2f
            );
            
            spriteRenderer.sprite = sprite;
        }
    }
}

