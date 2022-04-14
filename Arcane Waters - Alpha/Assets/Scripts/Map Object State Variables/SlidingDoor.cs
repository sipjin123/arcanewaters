using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SlidingDoor : VaryingStateObject
{
   #region Public Variables

   // The door we lift
   public Transform door;

   #endregion

   private void Update () {
      door.transform.localPosition = new Vector3(
         0,
         Mathf.MoveTowards(door.transform.localPosition.y, state.Equals("down") ? 0 : 2f, Time.deltaTime * 2f),
         0);
   }

   #region Private Variables

   #endregion
}
