using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameFramework.GameData
{
    public class GameDataRuntimeBase
    {
        protected void LoadTable(ITable table)
        {
            table.Load();
        }

        protected void UnloadTable(ITable table)
        {
            table.Unload();
        }
    }
}
