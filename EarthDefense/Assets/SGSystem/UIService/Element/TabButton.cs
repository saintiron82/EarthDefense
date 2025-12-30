using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.UI
{
    public class TabButton : EXButton
    {
        public void SetEnable()
        {
            UIColorGuide.ChangeColorEnable();
        }

        public void SetDisable()
        {
            UIColorGuide.ChangeColorDisable();
        }

        public void SetLock()
        {
            UIColorGuide.ChangeColorLock();
        }
    }
}
