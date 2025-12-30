using Cysharp.Threading.Tasks;

namespace SG
{
   public interface IService
    {
        bool IsPrepare { get; }
        bool IsRelease { get; }
        UniTask<bool> Init();
        UniTask<bool> Prepare();
        void Release();
        void Destroy();
    }
    public interface IDoUpdate
    {
        void DoUpdate();
    }
}