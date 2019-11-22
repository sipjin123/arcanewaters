using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ConfinerAssigner : ClientMonoBehaviour {
   #region Public Variables

   // Our confiner
   public Cinemachine.CinemachineConfiner confiner;

   // The Area we want our camera to be bounded by
   public string areaKey;

   #endregion

   private void Update () {
      if (confiner.m_BoundingShape2D == null) {
         Area area = AreaManager.self.getArea(areaKey);

         if (area != null) {
            confiner.m_BoundingShape2D = area.cameraBounds;
         }
      }
   }

   #region Private Variables

   #endregion
}
