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
      if (shipEntity == null || shipEntity.isDead() || shipEntity.areaType != currentArea.areaType) {
         Destroy(this.gameObject);
         return;
      }

      // Keep the icon in the right position
      Vector3 relativePosition = shipEntity.transform.position - currentArea.transform.position;
      relativePosition *= 12f;
      relativePosition += new Vector3(-64f, 64f);
      Util.setLocalXY(this.transform, relativePosition);
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
