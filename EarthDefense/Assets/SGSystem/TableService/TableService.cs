using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Data
{
    public class TableService : ServiceBase
    {
        private static TableService _instance;
        public static TableService Instance => _instance;
        public Dictionary<string, ITable> Tables = new Dictionary<string, ITable>();
        public override async UniTask<bool> Init()
        {
            await base.Init();
            _instance = this;
            AutoAddTableInFolder( "TableContainer" );
            return true;
        }

        public override async UniTask<bool> Prepare()
        {
            await base.Prepare();
            return true;
        }

        public void AutoAddTableInFolder( string path )
        {
            var containerList = Resources.LoadAll<ScriptableObject>( path );
            foreach( var container in containerList )
            {
                var tableContainer = container as ITableContainer;
                var talbName = tableContainer.TableName == string.Empty ? container.name : tableContainer.TableName;
                if( Tables.ContainsKey( talbName ) == false )
                {
                    var type = Type.GetType( "DC.Data." + talbName );
                    if( type == null )
                        continue;
                    var table = Activator.CreateInstance( type ) as ITable;
                    if( table == null )
                        continue;

                    table.InitTable( tableContainer );
                    AddNewTable( table );
                }
                else
                {
                    Tables[tableContainer.TableName].AddTableData( tableContainer );
                }
            }
        }

        public void AddNewTable( ITable table )
        {
            if( Tables.ContainsKey( table.TableName ) == false )
            {
                Tables.Add( table.TableName, table );
            }
            else
            {
                Debug.Log( $"Already Exist {table.TableName} Table" );
            }
        }

        public T GetTable<T>( string tableName ) where T : class, ITable
        {
            if( Tables.ContainsKey( tableName ) )
            {
                return Tables[tableName] as T;
            }
            else
            {
                Debug.Log( $"Not Exist {tableName} Table" );
                return null;
            }
        }

        public T GetTable<T>() where T : class, ITable
        {
            var tableName = typeof( T ).Name;
            if( Tables.ContainsKey( tableName ) )
            {
                return Tables[tableName] as T;
            }
            else
            {
                Debug.Log( $"Not Find <<{tableName}>> Table" );
                return null;
            }
        }

        public ITable GetTable( string tableName )
        {
            if( Tables.ContainsKey( tableName ) )
            {
                return Tables[tableName];
            }
            else
            {
                Debug.Log( $"Not Find <<{tableName}>> Table" );
                return null;
            }
        }
        public override void Release()
        {

        }

        public override void Destroy()
        {

        }
    }

}
