using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_PlayerIcon : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static MM_PlayerIcon self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      // Keep the icon in the right position
      Area currentArea = AreaManager.self.getArea(Global.player.areaKey);
      if (currentArea != null) {
         Vector3 relativePosition = Global.player.transform.position - currentArea.transform.position;
         relativePosition *= 12f;
         relativePosition += new Vector3(-64f, -64f);
         Util.setLocalXY(this.transform, relativePosition);

         // Rotate the player arrow based on our facing direction
         _image.transform.rotation = Quaternion.Euler(0, 0, getArrowRotation());
      }
   }

   protected int getArrowRotation () {
      switch (Global.player.facing) {
         case Direction.North:
            return 0;
         case Direction.NorthEast:
         case Direction.East:
         case Direction.SouthEast:
            return -90;
         case Direction.South:
            return -180;
         case Direction.SouthWest:
         case Direction.West:
         case Direction.NorthWest:
            return -270;
      }

      return 0;
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
