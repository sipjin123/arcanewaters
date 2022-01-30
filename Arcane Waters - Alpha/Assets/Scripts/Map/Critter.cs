using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Critter : ClientMonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _simpleAnim = GetComponent<SimpleAnimation>();
   }

   void Update () {
      // If we just started, play the gopher sound
      if (_simpleAnim.getIndex() == 1 && _previousIndex == 0) {
         int num = Random.Range(1, 4);
         //SoundManager.create3dSound("gopher_" + num, this.transform.position);
      }

      // Keep track for next frame
      _previousIndex = _simpleAnim.getIndex();
   }

   #region Private Variables

   // Our animation
   protected SimpleAnimation _simpleAnim;

   // The index in the previous frame
   protected int _previousIndex;

   #endregion
}
