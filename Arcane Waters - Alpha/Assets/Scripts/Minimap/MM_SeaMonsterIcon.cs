﻿using UnityEngine;
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

      // Set correct sea monster icon position in minimap
      Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(seaMonster.transform, currentArea));
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
