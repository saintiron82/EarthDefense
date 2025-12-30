using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SG
{
    public abstract class MonoServiceBase : MonoBehaviour, IService
    {
        protected bool isPrepare;
        public bool IsPrepare => isPrepare;

        protected bool isRelease;
        public bool IsRelease => isRelease;

        public virtual async UniTask<bool> Init()
        {
            isRelease = false;
            DirectInit();
            await UniTask.CompletedTask;
            return true;
        }

        public virtual void DirectInit()
        {
        }

        public virtual async UniTask<bool> Prepare()
        {
            isPrepare = true;
            DirectPrepare();
            await UniTask.CompletedTask;
            return true;
        }

        public virtual void DirectPrepare()
        {
        }

        public virtual void Release()
        {
            isPrepare = false;
            isRelease = true;
        }

        public virtual void Destroy()
        {
            if (isRelease == false)
            {
                Release();
            }

            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
