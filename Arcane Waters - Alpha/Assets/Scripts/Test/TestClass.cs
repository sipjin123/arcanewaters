using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TestClass : NetworkBehaviour {
	#region Public Variables

	#endregion

   public static void someFunction () {
      // Some comment
   }
   
   public static int someIntFunction () {
      return 5;
   }

   [Command]
   public void Cmd_someIntCommand () {
      int test = 5;
      test += 1;
      test += 3;
      Debug.Log("Test: " + test);
   }

   [ServerOnly]
   private int someServerCode (int testParam) {
      testParam += 3;

      // Some comment
      if (Util.isBatch()) {
         return -1;
      }

      return testParam + 4;
   }

   [Server]
   protected static Armor someObjectFunction () {
      return new Armor();
   }

   [Client]
   public static int someClientFunction (int param) {
      Debug.Log("Some client function called: " + param);

      return param + 3;
   }

   [Server]
   public static int someServerFunction (int param) {
      Debug.Log("Some server function called: " + param);

      return param + 3;
   }

   #region Private Variables

   #endregion
}

