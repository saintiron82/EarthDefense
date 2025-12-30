using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public class ServiceHome
    {
        public Dictionary<Type, IService> Services = new Dictionary<Type, IService>();
        public Dictionary<Type, IDoUpdate> UpdateServices = new Dictionary<Type, IDoUpdate>();

        private GameObject _serviceRoot; // MonoService용 루트
        private Action<ServiceHome> _installer;

        public void Register( Action<ServiceHome> installer )
        {
            _installer = installer;
        }

        public async UniTask Init()
        {
            // MonoService용 루트 생성
            _serviceRoot = new GameObject("[ServiceRoot]");
            UnityEngine.Object.DontDestroyOnLoad(_serviceRoot);

            await UniTask.CompletedTask;
            
            if( _installer != null )
            {
                _installer( this );
            }
        }
        
        public async UniTask Init( Action<ServiceHome> installer )
        {
            Register( installer );
            await Init();
        }

        public async UniTask Prepare()
        {
            await UniTask.CompletedTask;
        }

        public async UniTask InitService()
        {
            foreach (var service in Services)
            {
                await service.Value.Init();
            }
        }

        public async UniTask PrepareService()
        {
            foreach (var service in Services)
            {
                await service.Value.Prepare();
            }
        }

        public IService AddNewService( Type type )
        {
            var nowService = GetService( type );
            if( nowService != null )
            {
                Debug.LogError( $"ServiceHome.AddNewService: {type} already exists" );
                return nowService;
            }

            var service = Activator.CreateInstance(type) as IService;
            if (service == null)
            {
                Debug.LogError($"ServiceHome.AddNewService: {type} is not a IService");
                return null;
            }
            Services.Add(type, service);

            if (service is IDoUpdate updateService )
            {
                UpdateServices.Add(type, updateService);
            }
            return service;
        }

        public T AddNewService<T>() where T : class, IService, new()
        {
            var type = typeof(T);
            return AddNewService(type) as T;
        }

        public IService AddNewService( IService service )
        {
            var type = service.GetType();
            var nowService = GetService(type);
            if( nowService != null )
            {
                Debug.LogError($"ServiceHome.AddNewService: {type} already exists");
                return nowService;
            }
            Services.Add(type, service);

            if (service is IDoUpdate updateService )
            {
                UpdateServices.Add(type, updateService);
            }
            return service;
        }

        public void ReleaseService( IService service )
        {
            var type = service.GetType();
            ReleaseService(type);
        }

        public void ReleaseService<T>() where T : IService
        {
            var type = typeof(T);
            ReleaseService(type);
        }

        public void ReleaseService( Type type )
        {
            if (Services.ContainsKey(type))
            {
                Services.Remove(type);
            }

            if (UpdateServices.ContainsKey(type))
            {
                UpdateServices.Remove(type);
            }
        }

        public IService GetService(Type type)
        {
            if( !Services.ContainsKey( type ) )
            {
                Debug.LogWarning( $"ServiceHome.GetService: {type} not found" );
                return default;
            }
            return Services[type];
        }

        public T GetService<T>() where T : IService
        {
            var type = typeof(T);
            if (!Services.ContainsKey(type))
            {
                Debug.LogWarning( $"ServiceHome.GetService: {type} not found");
                return default;
            }
            return (T)Services[type];
        }

        public void Update()
        {
            foreach (var updateService in UpdateServices)
            {
                updateService.Value.DoUpdate();
            }
        }

        /// <summary>
        /// MonoServiceBase 기반 서비스 추가
        /// </summary>
        public T AddMonoService<T>() where T : MonoServiceBase
        {
            var type = typeof(T);
            
            var nowService = GetService(type);
            if (nowService != null)
            {
                Debug.LogError($"ServiceHome.AddMonoService: {type} already exists");
                return nowService as T;
            }

            if (_serviceRoot == null)
            {
                Debug.LogError("ServiceHome.AddMonoService: _serviceRoot is null. Call Init() first.");
                return null;
            }

            // GameObject 생성 후 컴포넌트 추가
            var serviceObj = new GameObject($"[{typeof(T).Name}]");
            serviceObj.transform.SetParent(_serviceRoot.transform);
            
            var service = serviceObj.AddComponent<T>();
            Services.Add(type, service);

            if (service is IDoUpdate updateService)
            {
                UpdateServices.Add(type, updateService);
            }

            return service;
        }

        public void Release()
        {
            foreach (var service in Services)
            {
                service.Value.Release();
            }
            Services.Clear();
            UpdateServices.Clear();

            // 루트 오브젝트 파괴
            if (_serviceRoot != null)
            {
                UnityEngine.Object.Destroy(_serviceRoot);
                _serviceRoot = null;
            }
        }
    }
}