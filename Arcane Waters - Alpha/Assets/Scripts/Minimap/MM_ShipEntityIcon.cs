using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_ShipEntityIcon : MonoBehaviour {
   #region Public Variables

   // Associated ship entity
   public ShipEntity shipEntity;

   // Area in which ship should be (otherwise hide)
   public Area currentArea;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (shipEntity == null || shipEntity.isDead() || shipEntity.areaKey != currentArea.areaKey) {
         Destroy(this.gameObject);
         return;
      }

      // Set correct ship entity icon position in minimap
      Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(shipEntity.transform, currentArea));
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
