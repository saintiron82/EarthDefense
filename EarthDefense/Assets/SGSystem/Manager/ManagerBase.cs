using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG
{
    public abstract class ManagerBase : IManager
    {
        protected bool isPrepare;
        public bool IsPrepare => isPrepare;

        protected bool isRelease;
        public bool IsRelease => isRelease;
        public virtual async UniTask<bool> Init()
        {
            isRelease = false;
            await UniTask.CompletedTask;
            return true;
        }

        public virtual async UniTask<bool> Prepare()
        {
            isPrepare = true;
            await UniTask.CompletedTask;
            return true;
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