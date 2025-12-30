using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class EXRawImage : RawImage, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private bool _isRotationEnabled = true;
    [SerializeField]
    private float _rotationSpeed = 0.2f;
    [SerializeField]
    private Transform _rotationTarget;
    [SerializeField]
    private bool _rotationXAxis = true;
    [SerializeField]
    private bool _rotationYAxis = true;
    [SerializeField]
    private float _renderCameraOrthographicSize = 0.45f;

    private bool _isPointerDown;
    private bool _isDragging;
    private UnityAction<Vector2> _onDragCallback;

    public UnityAction<Vector2> OnDragEvent
    {
        get => _onDragCallback;
        set => _onDragCallback = value;
    }

    public bool IsRotationEnabled
    {
        get => _isRotationEnabled;
        set
        {
            _isRotationEnabled = value;
            if (_rotationTarget != null)
            {
                _rotationTarget.localRotation = Quaternion.identity;
            }
        }
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = value;
    }

    public bool RotationXAxis
    {
        get => _rotationXAxis;
        set => _rotationXAxis = value;
    }

    public bool RotationYAxis
    {
        get => _rotationYAxis;
        set => _rotationYAxis = value;
    }

    public Transform RotationTarget
    {
        get => _rotationTarget;
        set => _rotationTarget = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isPointerDown)
        {
            _isDragging = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isPointerDown && _isDragging)
        {
            Vector2 delta = eventData.delta;
            RotateTarget(delta);
            _onDragCallback?.Invoke(delta);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        _isDragging = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    private void RotateTarget(Vector2 rotateValue)
    {
        if (_rotationTarget != null && _isRotationEnabled)
        {
            float xRotation = _rotationXAxis ? rotateValue.y * _rotationSpeed : 0f;
            float yRotation = _rotationYAxis ? -rotateValue.x * _rotationSpeed : 0f;
            _rotationTarget.localRotation *= Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }
}

