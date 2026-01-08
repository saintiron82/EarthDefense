using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Polar.Input
{
    /// <summary>
    /// Polar 전용 New Input System 런타임 바인딩 (ShapeDefense 독립)
    /// 코드만으로 동작하는 최소 기능 제공
    /// 
    /// 제공 액션:
    /// - Player/Attack (Button) - 좌클릭, Enter, Gamepad
    /// - Player/Look (Vector2) - 마우스 위치
    /// </summary>
    public sealed class PolarInputActionsRuntime : IDisposable
    {
        public InputActionAsset Asset { get; }
        public PlayerActions Player { get; }

        public PolarInputActionsRuntime()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();

            var map = new InputActionMap("Player");

            // Attack (발사)
            var attack = map.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");
            attack.AddBinding("<Touchscreen>/primaryTouch/tap");
            attack.AddBinding("<Keyboard>/enter");
            attack.AddBinding("<Keyboard>/space");  // Space 추가

            // Look (마우스 위치)
            var look = map.AddAction("Look", InputActionType.Value, "Vector2");
            look.AddBinding("<Pointer>/position");

            Asset.AddActionMap(map);
            Player = new PlayerActions(map, attack, look);
        }

        public void Enable() => Asset.Enable();
        public void Disable() => Asset.Disable();

        public void Dispose()
        {
            try { Disable(); } catch { /* ignore */ }
            if (Asset != null)
            {
                UnityEngine.Object.Destroy(Asset);
            }
        }

        public readonly struct PlayerActions
        {
            private readonly InputActionMap _map;
            public InputAction Attack { get; }
            public InputAction Look { get; }

            public PlayerActions(InputActionMap map, InputAction attack, InputAction look)
            {
                _map = map;
                Attack = attack;
                Look = look;
            }

            public void Enable() => _map.Enable();
            public void Disable() => _map.Disable();
        }
    }
}