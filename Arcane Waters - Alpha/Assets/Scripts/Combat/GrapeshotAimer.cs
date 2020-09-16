using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GrapeshotAimer : ClientMonoBehaviour {
   #region Public Variables

   #endregion
   private void Start () {
      _renderer = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      // Check if the right mouse is being held down for the grapeshot attack
      bool rightMouseDown = Input.GetMouseButton(1);
      // Can't do anything until we have a player
      if (Global.player == null || !(Global.player is SeaEntity)) {
         return;
      }
      _renderer.enabled = (rightMouseDown && SeaManager.getAttackType() == Attack.Type.Air);

      // Only show when we've selected grapeshot
      if (SeaManager.getAttackType() != Attack.Type.Air) {
         return;
      }

      // Center on the player's ship
      Util.setXY(this.transform, Global.player.transform.position);

      // Rotate according to the direction towards the mouse
      Vector3 targetDir = Util.getMousePos() - this.transform.position;
      float angle = Util.angle(targetDir);
      this.transform.rotation = Quaternion.Euler(0f, 0f, 315f - angle);
   }

   #region Private Variables

   // Our Sprite Renderer
   protected SpriteRenderer _renderer;

   #endregion
}
