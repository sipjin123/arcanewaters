using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class LineCreator : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating our line
   public TrailingLine trailPrefab;

   #endregion

   private void Start () {
      InvokeRepeating("makeTrail", 0f, .01f);
   }

   protected void makeTrail () {
      TrailingLine trail = Instantiate(trailPrefab);
      trail.transform.position = this.transform.position;
      Util.setZ(trail.transform, 95f);
   }

   #region Private Variables

   #endregion
}
