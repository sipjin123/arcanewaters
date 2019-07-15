using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DebugButtons : MonoBehaviour {
    #region Public Variables

    #endregion

    #region Private Variables
    private void Update()
    {
        if(Input.GetKey(KeyCode.O))
        {
            Debug.LogError("Battle stat");
            Global.player.rpc.Cmd_StartNewBattle(1);
        }
    }
    #endregion
}

public static class DebugCustom
{
    public static string B = "[B0NTA] :: ";
    public static void Print(string wat)
    {
        Debug.LogError(B + wat);
    }
}
