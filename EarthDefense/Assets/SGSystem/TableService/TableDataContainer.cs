using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SG.Data
{
    public abstract class TableDataContainer<T> : ScriptableObject where T : TableData
    {
    }
}
