using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_SeaMonsterIcon : ClientMonoBehaviour {
   #region Public Variables

   // Associated monster entity
   public SeaMonsterEntity seaMonster;

   // Area in which sea monster exists
   public Area currentArea;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (seaMonster == null || seaMonster.isDead()) {
         gameObject.SetActive(false);
         return;
      }

      // Keep the icon in the right position
      Vector3 relativePosition = seaMonster.transform.position - currentArea.transform.position;
      relativePosition *= 12f;
      relativePosition += new Vector3(-128f, 0f);
      Util.setLocalXY(this.transform, relativePosition);
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
