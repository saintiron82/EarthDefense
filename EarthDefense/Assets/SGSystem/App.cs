using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SG.Audio;
using SG.Save;
using SG.UI;

namespace SG
{
    public class App : MonoBehaviour
    {
        // 백그라운드 실행 여부 체크 변수 
        [SerializeField] public bool RunInBackground = true;
        
        public static bool Nosave;
        public static App Instance { get; private set; }
        public ServiceHome ServiceHome { get; private set; }

        [SerializeField]
        private List<MonoServiceBase> _services;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            
            // 백그라운드 실행 설정
            Application.runInBackground = RunInBackground;
            
            DontDestroyOnLoad(this);
            CreateServiceHome().Forget();
        }

        private async UniTaskVoid CreateServiceHome()
        {
            ServiceHome = new ServiceHome();
            await ServiceHome.Init( InstallServices );
        }
        
        private void InstallServices( ServiceHome home )
        {
            home.AddNewService<SaveService>();
            home.AddNewService<ManagerMaster>();
            home.AddNewService<UIService>();

            foreach (var service in _services)
            {
                home.AddNewService(service);
            }
        }

        private void Update()
        {
            if( ServiceHome != null )
            {
                ServiceHome.Update();
            }
        }

        private void OnDestroy()
        {
            if( Nosave )
            {
                return;
            }
            if( ServiceHome != null )
            {
                ServiceHome.Release();
            }
        }

        public void Release()
        {
            if( ServiceHome != null )
            {
                ServiceHome.Release();
                ServiceHome = null;
            }
            
        }

        public void OnApplicationQuit()
        {
            if( Nosave )
            {
                return;
            }
            Release();
        }
    }
}
