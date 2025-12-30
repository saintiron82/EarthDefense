using System;

namespace SG
{
    public interface IUIItemData {

    }
    public class UIItemData<T> where T : class
    {
        public virtual int DataIndex { get; protected set; } = -1;

        public virtual T GameData { get; protected set; } = default( T );

        public virtual bool Selected { get; set; } = false;

        public Action OnSlotDataRefreshUIAction { get; set; } = null;

        public UIItemData( int index, T gameData ) {
            DataIndex = index;
            Selected = false;
            GameData = gameData;
        }

        public virtual void SetSlotData( T gameData ) {
            GameData = gameData;
            RefreshSlotDataUI();
        }

        public virtual void RefreshSlotDataUI() {
            if( OnSlotDataRefreshUIAction != null )
                OnSlotDataRefreshUIAction.Invoke();
        }
    }
}
