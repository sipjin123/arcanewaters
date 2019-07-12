using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Chimney : ClientMonoBehaviour {
   #region Public Variables

   // The chimney smoke prefab
   public ChimneySmoke chimneySmokePrefab;

   #endregion

   private void Start () {
      // Keep creating smoke puffs
      InvokeRepeating("createSmoke", 0f, 1.4f);
   }

   protected void createSmoke () {
      // Create an instance of the chimney smoke
      ChimneySmoke smoke = Instantiate(chimneySmokePrefab, this.transform.position + new Vector3(0f, .04f), Quaternion.identity);
      smoke.transform.SetParent(this.transform, true);
   }

   #region Private Variables

   #endregion
}
