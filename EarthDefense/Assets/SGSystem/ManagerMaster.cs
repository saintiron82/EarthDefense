using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public class ManagerMaster : ServiceBase, IDoUpdate
    {
        public static ManagerMaster Instance { get; private set; }
        public Dictionary<Type, IManager> Managers = new Dictionary<Type, IManager>();

        public override async UniTask<bool> Init()
        {
            if (await base.Init() == false)
            {
                return false;
            }
            Instance = this;
            return true;
        }

        public virtual void RegistedManagers()
        {

        }

        //모든 Manager는 PrePare 이후에 추가되어야 한다.
        public override async UniTask<bool> Prepare()
        {
            if (await base.Prepare() == false)
            {
                return false;
            }
            return true;
        }

        public async UniTask<bool> InitManager()
        {
            foreach (var manager in Managers)
            {
                await manager.Value.Init();
            }
            return true;
        }

        public async UniTask PrepareManager()
        {
            foreach (var manager in Managers)
            {
                await manager.Value.Prepare();
            }
        }

        public T AddNewManager<T>( IManager newManager ) where T : class, IManager
        {
            var type = typeof( T );
            if (Managers.ContainsKey(type))
            {
                Debug.LogError($"ManagerMaster.AddNewManager: {type} already exists");
                return null;
            }
            Managers.Add(type, newManager);
            return newManager as T;
        }

        public async UniTask<T> AddNewManager<T>() where T : class, IManager, new()
        {
            var type = typeof( T );
            var newManager = new T();

            if (AddNewManager<T>(newManager) == null)
            {
                await UniTask.CompletedTask;
                return null;
            }
            await UniTask.CompletedTask;
            return newManager;
        }

        public void RemoveManager<T>() where T : class, IManager
        {
            var type = typeof( T );
            var targetManager = Get<T>();
            if( targetManager == null )
            {
                Debug.LogError( $"ManagerMaster.RemoveManager: {type} not found" );
                return;
            }
            targetManager.Destroy();
            Managers.Remove( type );
        }

        public static T Get<T>() where T : class , IManager
        {
            var type = typeof( T );
            return Instance.Get(type) as T;
        }

        public IManager Get( Type type )
        {
            if( Managers.ContainsKey( type ) == false )
            {
                Debug.LogError( $"ManagerMaster.GetManager: {type} not found" );
                return default;
            }
            return Managers[type];
        }

        public void DoUpdate()
        {
            foreach( var manager in Managers )
            {
                var mgr = manager.Value;
                if( mgr == null ) continue;
                var mgrTypeName = mgr.GetType().Name;
                //Debug.Log( $"ManagerMaster.DoUpdate: updating {mgrTypeName}" );
 
                if( mgr is IDoUpdate updatable )
                {
                    try
                    {
                        updatable.DoUpdate();
                    }
                    catch( Exception ex )
                    {
                        Debug.LogError( $"ManagerMaster.DoUpdate: {mgrTypeName} threw {ex.GetType().Name}: {ex.Message}" );
                        Debug.LogException( ex ); // 한 매니저가 터져도 나머지는 계속 업데이트
                    }
                }
            }
        }

        //ManagerMaster에서 Release는 보유하고 있는 모든 Manager를 Destroy한다.
        //Manager는 sevice입장에선 Relese 단계에 소거되어야 하기 때문이다. 
        public override void Release()
        {
            base.Release();
            foreach( var manager in Managers )
            {
                manager.Value.Release();
            }
            
        }

        public override void Destroy()
        {
            if( IsRelease == false )
            {
                Release();
            }
            Managers.Clear();
            foreach( var manager in Managers )
            {
                manager.Value.Destroy();
            }

            base.Destroy();
            Instance = null;
        }
    }
}