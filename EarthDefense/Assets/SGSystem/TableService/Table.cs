using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SG.Data
{
    public interface ITableContainer
    {
        public string TableName { get; }
    }

    public interface ITableData
    {
        int ID { get; }
    }
    public interface ITable
    {
        public string TableName { get; }
        public void InitTable( ITableContainer container );

        public void AddTableData( ITableContainer container );
        public void OrganizeData();
    }


    [Serializable]
    public class TableData : ITableData
    {
        public virtual int ID { get; set; }
    }

    public class Table<D> : ITable where D : TableData
    {
        public Dictionary<int, D> DataList = new Dictionary<int, D>();
        public ITableContainer Container;
        public List<D> Datas;
        protected string tableName;
        public string TableName => tableName;
        public virtual void BuildDataList( List<D> dataList )
        {
            foreach( var data in dataList )
            {
                if( DataList.ContainsKey( data.ID ) == false )
                {
                    DataList.Add( data.ID, data );
                }
                else
                {
                    dataList[data.ID] = data;
                }
            }
        }

        protected void CreateDatas<T>( T container )
        {

        }


        public virtual void AddTableData( ITableContainer container )
        {

        }

        public virtual void InitTable( ITableContainer container )
        {
            tableName = container.TableName;
            Container = container;
        }

        public virtual void OrganizeData()
        {

        }

        public D GetData( int id )
        {
            if( DataList.ContainsKey( id ) )
            {
                return DataList[id];
            }
            return default;
        }

        public D GetRandomData()
        {
            var random = new System.Random();
            var index = random.Next( 0, DataList.Count );
            var data = DataList.ElementAt( index ).Value;
            return data;
        }
    }
}
