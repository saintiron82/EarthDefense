using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// New Input System 런타임 바인딩(코드만으로 동작).
    /// 프로젝트의 .inputactions(Assets/InputSystem_Actions.inputactions)와 이름/구조를 맞춰두되,
    /// 생성된 C# 래퍼 클래스가 없을 때도 PlayerShooter가 동작하도록 최소 기능만 제공합니다.
    /// 
    /// 제공 액션:
    /// - Player/Attack (Button)
    /// - Player/Look (Vector2) : Pointer/position을 사용해 '화면 좌표'를 제공합니다.
    /// </summary>
    public sealed class PlayerInputActionsRuntime : IDisposable
    {
        public InputActionAsset Asset { get; }

        public PlayerActions Player { get; }

        public PlayerInputActionsRuntime()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();

            var map = new InputActionMap("Player");

            // Attack
            var attack = map.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");
            attack.AddBinding("<Touchscreen>/primaryTouch/tap");
            attack.AddBinding("<Keyboard>/enter");

            // Look (screen position)
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
