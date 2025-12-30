using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace SG.UI
{
    public interface IMultiUIContainer 
    {
    }
    public class MultiUIContainer<T> : IMultiUIContainer where T : class, IUIPresenter 
    {

        // GUID 또는 string ID로 Presenter 인스턴스 추적
        private readonly Dictionary<string, T> _instances = new();
        private readonly Stack<T> _pool = new();
        private Func<UniTask<T>> _Factory;

        // ✅ 추가: 열려있는 순서 기억용
        private readonly List<string> _order = new();

        public void Init( Func<UniTask<T>> factory )
        {
            _Factory = factory;
        }

        public void Add( string id, IUIPresenter presenter )
        {
            if( _instances.ContainsKey( id ) )
            {
                Debug.LogWarning( $"Presenter already exists: {id}" );
                return;
            }

            _instances[id] = presenter as T;
            _order.Add( id );
        }

        public IUIPresenter Get( string id )
        {
            if( _instances.TryGetValue( id, out var presenter ) )
                return presenter;
            Debug.LogWarning( $"Presenter not found: {id}" );
            return null;
        }

        public UniTask<T> GetNewPresenter()
        {
                if( _pool != null && _pool.Count > 0 )
            {
                // Wrap the popped presenter in a UniTask to match the expected return type  
                return UniTask.FromResult( _pool.Pop() );
            }
            else
            {
                if( _Factory != null )
                {
                    return _Factory();
                }
            }
            // Return a default UniTask if no presenter is available  
            return UniTask.FromResult<T>( default(T) );
        }

        public void Remove( string id )
        {
            if( _instances.TryGetValue( id, out var presenter ) )
            {
                presenter.Close();
                _instances.Remove( id );
                _order.Remove( id );
            }
        }

        public void CloseAll()
        {
            foreach( var id in _order )
            {
                if( _instances.TryGetValue( id, out var presenter ) )
                {
                    presenter.Close();
                }
            }
            _instances.Clear();
            _order.Clear();
        }


        public void ReturnPool( T returnPresenter )
        {
            if(returnPresenter != null && _pool != null )
            {
                _pool.Push(returnPresenter );
            }
        }

        public IEnumerable<string> Keys => _order;

        public int Count => _instances.Count;
    }
}
