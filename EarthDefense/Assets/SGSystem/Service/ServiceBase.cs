using Cysharp.Threading.Tasks;

namespace SG
{
    public class ServiceBase : IService
    {
        protected bool isPrepare;
        public bool IsPrepare => isPrepare;

        protected bool isRelease;
        public bool IsRelease => isRelease;
        public virtual async UniTask<bool> Init()
        {
            InitInternal();
            isRelease = false;
            await UniTask.CompletedTask;
            return true;
        }

        protected virtual void InitInternal()
        {
            
        }

        public virtual async UniTask<bool> Prepare()
        {
            PrepareInternal();
            isPrepare = true;
            await UniTask.CompletedTask;
            return true;
        }

        protected virtual void PrepareInternal()
        {
            
        }

        public virtual void Release()
        {
            isPrepare = false;
            isRelease = true;
        }
        public virtual void Destroy()
        {
            if( isRelease == false )
                Release();
        }
    }
}
