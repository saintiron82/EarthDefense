using UnityEngine;
using UnityEngine.UI;
using SG;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace SG.UI
{
    /// <summary>
    /// UI Image의 텍스처에 흐름 효과를 적용하는 컴포넌트
    /// </summary>
    public class EXFlowImage : Image
    {
        [Header("Flow Settings")]
        [SerializeField] private Vector2 _flowDirection = Vector2.right; // DESC :: 흐름 방향 벡터
        [SerializeField] private float _flowSpeed = 1.0f; // DESC :: 흐름 속도
        [SerializeField] private bool _autoStart = true; // DESC :: 자동 시작 여부
        [SerializeField] private bool _useUnscaledTime = true; // DESC :: Time.unscaledTime 사용 여부

        [Header("Texture Settings")]
        [SerializeField] private Vector2 _textureScale = Vector2.one; // DESC :: 텍스처 스케일
        [SerializeField] private bool _wrapTexture = true; // DESC :: 텍스처 래핑 여부

        private Material _materialInstance;
        private Vector2 _currentOffset;
        private bool _isFlowing = false;
        private bool _materialDirty = false; // DESC :: IL2CPP에서 머티리얼 업데이트 상태 추적
        private bool _pendingMaterialUpdate = false; // DESC :: 머티리얼 업데이트 대기 상태
        private CancellationTokenSource _flowCancellationTokenSource; // DESC :: Flow 효과 취소를 위한 내부 토큰 소스

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        protected override void Start()
        {
            base.Start();
            if (_autoStart)
            {
                StartFlow();
            }
        }

        protected override void OnDestroy()
        {
            DisposeCancellationTokenSource();
            CleanupMaterial();
            base.OnDestroy();
        }

        private void Update()
        {
            // DESC :: 안전한 토큰 상태 확인
            bool isTokenValid = _flowCancellationTokenSource != null && !_flowCancellationTokenSource.IsCancellationRequested;
            
            if (_isFlowing && isTokenValid)
            {
                UpdateTextureFlow();
            }
            else if (_isFlowing && !isTokenValid)
            {
                // DESC :: 토큰이 취소되었거나 없으면 흐름 정지
                _isFlowing = false;
            }
        }

        /// <summary>
        /// 마스킹과 흐름 효과를 모두 지원하기 위한 머티리얼 수정
        /// </summary>
        /// <param name="baseMaterial">기본 머티리얼</param>
        /// <returns>수정된 머티리얼</returns>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            // DESC :: IL2CPP에서 안전한 머티리얼 처리
            if (_materialInstance == null)
            {
                CreateMaterialInstance();
            }

            // DESC :: 흐름 효과가 적용된 머티리얼을 사용하되, 마스킹도 적용
            Material materialToModify = _materialInstance != null ? _materialInstance : baseMaterial;
            
            // DESC :: 부모 클래스의 GetModifiedMaterial을 호출하여 마스킹 처리
            Material result = base.GetModifiedMaterial(materialToModify);
            
            // DESC :: IL2CPP에서 현재 오프셋 재적용
            if (result != null && _isFlowing)
            {
                result.SetTextureOffset(ConstrantStrings.MainTex, _currentOffset);
                result.SetTextureScale(ConstrantStrings.MainTex, _textureScale);
            }
            
            return result;
        }

        /// <summary>
        /// 머티리얼 업데이트 시 흐름 효과도 함께 업데이트
        /// </summary>
        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            
            // DESC :: 머티리얼이 변경되었을 때 흐름 효과 재적용 (순환 참조 방지)
            if (_isFlowing && _materialInstance != null && !_pendingMaterialUpdate)
            {
                _materialDirty = true;
                // DESC :: SetMaterialDirty 호출하지 않고 다음 Update에서 처리
            }
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void Init()
        {
            CreateMaterialInstance();
        }

        /// <summary>
        /// 머티리얼 인스턴스 생성
        /// </summary>
        private void CreateMaterialInstance()
        {
            // DESC :: 기존 머티리얼 정리
            CleanupMaterial();
            
            Material originalMaterial = material;
            
            if (originalMaterial != null)
            {
                _materialInstance = new Material(originalMaterial);
            }
            else
            {
                // DESC :: 기본 UI 머티리얼 사용
                var defaultShader = Shader.Find(ConstrantStrings.UIDefaultShader);
                if (defaultShader != null)
                {
                    _materialInstance = new Material(defaultShader);
                }
                else
                {
                    Debug.LogError("EXFlowImage: UI/Default shader not found!");
                    return;
                }
            }

            // DESC :: 초기 텍스처 스케일 설정
            if (_materialInstance != null)
            {
                _materialInstance.SetTextureScale(ConstrantStrings.MainTex, _textureScale);
                // DESC :: 초기화 시에는 즉시 업데이트
                ScheduleMaterialUpdate();
            }
        }

        /// <summary>
        /// 텍스처 흐름 업데이트
        /// </summary>
        private void UpdateTextureFlow()
        {
            if (_materialInstance == null)
            {
                CreateMaterialInstance();
                if (_materialInstance == null) return;
            }

            float deltaTime = _useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : Time.deltaTime;
            
            // DESC :: 방향 벡터와 속도를 이용한 오프셋 계산
            Vector2 flowOffset = _flowDirection.normalized * _flowSpeed * deltaTime;
            _currentOffset += flowOffset;

            // DESC :: 텍스처 래핑 처리
            if (_wrapTexture)
            {
                _currentOffset.x = _currentOffset.x % 1.0f;
                _currentOffset.y = _currentOffset.y % 1.0f;
            }

            // DESC :: 머티리얼에 오프셋 적용
            _materialInstance.SetTextureOffset(ConstrantStrings.MainTex, _currentOffset);
            
            // DESC :: 그래픽 재빌드 루프 외부에서 머티리얼 업데이트 예약
            if (_materialDirty && !_pendingMaterialUpdate)
            {
                ScheduleMaterialUpdate();
                _materialDirty = false;
            }
        }

        /// <summary>
        /// 안전한 머티리얼 업데이트 예약
        /// </summary>
        private void ScheduleMaterialUpdate()
        {
            if (!_pendingMaterialUpdate)
            {
                _pendingMaterialUpdate = true;
                DelayedMaterialUpdate().Forget();
            }
        }

        /// <summary>
        /// 다음 프레임에 머티리얼 업데이트 실행
        /// </summary>
        private async UniTaskVoid DelayedMaterialUpdate()
        {
            try
            {
                // DESC :: 유효한 토큰이 있을 때만 토큰 사용
                var token = _flowCancellationTokenSource?.Token ?? default;
                await UniTask.NextFrame(cancellationToken: token); // DESC :: 한 프레임 대기
                
                if (this != null && gameObject.activeInHierarchy && 
                    (_flowCancellationTokenSource?.IsCancellationRequested == false))
                {
                    SetMaterialDirty();
                }
            }
            catch (System.OperationCanceledException)
            {
                // DESC :: 취소된 경우 무시
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"EXFlowImage DelayedMaterialUpdate error: {ex.Message}");
            }
            finally
            {
                _pendingMaterialUpdate = false;
            }
        }

        public override void RecalculateMasking()
        {
            base.RecalculateMasking();
            // DESC :: 마스킹 재계산 후 머티리얼 상태 갱신 (안전한 방식으로)
            _materialDirty = true;
        }

        /// <summary>
        /// 흐름 시작
        /// </summary>
        public void StartFlow()
        {
            // DESC :: 기존 토큰 소스 정리
            DisposeCancellationTokenSource();
            
            // DESC :: 상태 초기화
            _pendingMaterialUpdate = false;
            _materialDirty = false;
            
            // DESC :: 새로운 토큰 소스 생성
            _flowCancellationTokenSource = new CancellationTokenSource();
            
            _isFlowing = true;
            
            // DESC :: 흐름 시작 시 머티리얼 초기화 확인
            if (_materialInstance == null)
            {
                CreateMaterialInstance();
            }
            
            // DESC :: 즉시 머티리얼 업데이트 트리거
            _materialDirty = true;
            ScheduleMaterialUpdate();
        }

        /// <summary>
        /// 흐름 정지
        /// </summary>
        public void StopFlow()
        {
            _isFlowing = false;
            
            // DESC :: 토큰 소스 취소 및 정리
            DisposeCancellationTokenSource();
            
            // DESC :: 상태 리셋
            _pendingMaterialUpdate = false;
            _materialDirty = false;
        }

        /// <summary>
        /// CancellationTokenSource 정리
        /// </summary>
        private void DisposeCancellationTokenSource()
        {
            if (_flowCancellationTokenSource != null)
            {
                try
                {
                    if (!_flowCancellationTokenSource.IsCancellationRequested)
                    {
                        _flowCancellationTokenSource.Cancel();
                    }
                }
                catch (System.ObjectDisposedException)
                {
                    // DESC :: 이미 해제된 경우 무시
                }
                finally
                {
                    _flowCancellationTokenSource.Dispose();
                    _flowCancellationTokenSource = null;
                }
            }
        }

        /// <summary>
        /// 흐름 방향 설정
        /// </summary>
        /// <param name="direction">흐름 방향 벡터</param>
        public void SetFlowDirection(Vector2 direction)
        {
            _flowDirection = direction;
        }

        /// <summary>
        /// 흐름 속도 설정
        /// </summary>
        /// <param name="speed">흐름 속도</param>
        public void SetFlowSpeed(float speed)
        {
            _flowSpeed = speed;
        }

        /// <summary>
        /// 텍스처 스케일 설정
        /// </summary>
        /// <param name="scale">텍스처 스케일</param>
        public void SetTextureScale(Vector2 scale)
        {
            _textureScale = scale;
            if (_materialInstance != null)
            {
                _materialInstance.SetTextureScale(ConstrantStrings.MainTex, _textureScale);
                _materialDirty = true;
                ScheduleMaterialUpdate();
            }
        }

        /// <summary>
        /// 현재 오프셋 리셋
        /// </summary>
        public void ResetOffset()
        {
            _currentOffset = Vector2.zero;
            if (_materialInstance != null)
            {
                _materialInstance.SetTextureOffset(ConstrantStrings.MainTex, _currentOffset);
                _materialDirty = true;
                ScheduleMaterialUpdate();
            }
        }

        /// <summary>
        /// 특정 오프셋으로 설정
        /// </summary>
        /// <param name="offset">설정할 오프셋</param>
        public void SetOffset(Vector2 offset)
        {
            _currentOffset = offset;
            if (_materialInstance != null)
            {
                _materialInstance.SetTextureOffset(ConstrantStrings.MainTex, _currentOffset);
                _materialDirty = true;
                ScheduleMaterialUpdate();
            }
        }

        /// <summary>
        /// 머티리얼 정리
        /// </summary>
        private void CleanupMaterial()
        {
            if (_materialInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_materialInstance);
                }
                else
                {
                    DestroyImmediate(_materialInstance);
                }
                _materialInstance = null;
            }
        }

        /// <summary>
        /// 현재 흐름 상태 반환
        /// </summary>
        public bool IsFlowing => _isFlowing;

        /// <summary>
        /// 현재 흐름 방향 반환
        /// </summary>
        public Vector2 FlowDirection => _flowDirection;

        /// <summary>
        /// 현재 흐름 속도 반환
        /// </summary>
        public float FlowSpeed => _flowSpeed;

        /// <summary>
        /// 현재 오프셋 반환
        /// </summary>
        public Vector2 CurrentOffset => _currentOffset;
    }
}
