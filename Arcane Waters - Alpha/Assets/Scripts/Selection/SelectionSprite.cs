using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SelectionSprite : MonoBehaviour {
   #region Public Variables

   // The four sprites that we display
   public SpriteRenderer north;
   public SpriteRenderer east;
   public SpriteRenderer south;
   public SpriteRenderer west;

   #endregion

   void Start () {
      // Add our arrows to a list for easier reference
      _arrows = new List<SpriteRenderer>() { north, east, south, west };
   }

   void Update () {
      NetEntity selectedEntity = SelectionManager.self.selectedEntity;

      // Start out hidden if we don't have a selected entity
      if (selectedEntity == null) {
         hide();
      }

      // For now, we only support selections while at sea
      if (!(Global.player is PlayerShipEntity)) {
         selectedEntity = null;
         return;
      }

      // Do some extra stuff if we have an entity selected
      if (selectedEntity != null) {
         // Keep it positioned under the selected ship
         Vector3 targetPosition = selectedEntity.transform.position;
         targetPosition.z -= .001f;
         this.transform.position = targetPosition + getOffset(selectedEntity);

         // Move the arrows around based on the orientation of our target
         setDistances(selectedEntity);

         // Set the color of the arrows
         setColorsForTarget(selectedEntity);

         // Make sure we're visible
         show();
      }
   }

   public void show () {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.enabled = true;
      }
   }

   public void hide () {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.enabled = false;
      }
   }

   protected void setDistances (NetEntity selectedEntity) {
      // Default distances
      float horizontalDistance = .20f;
      float verticalDistance = .20f;

      // A small offset applied to make the arrows bounce
      float offset = (Time.time % 1 > .5f) ? .01f : -.01f;

      north.transform.localPosition = new Vector3(0f, verticalDistance + offset, north.transform.localPosition.z);
      south.transform.localPosition = new Vector3(0f, -verticalDistance - offset, south.transform.localPosition.z);
      east.transform.localPosition = new Vector3(horizontalDistance + offset, 0f, east.transform.localPosition.z);
      west.transform.localPosition = new Vector3(-horizontalDistance - offset, 0f, west.transform.localPosition.z);
   }

   protected void setColorsForTarget (NetEntity selectedEntity) {
      PlayerShipEntity ourShip = (PlayerShipEntity) Global.player;

      // Default color
      setColors(Color.gray);

      // If the target is dead, then we're done
      if (selectedEntity is SeaEntity && ((SeaEntity) selectedEntity).isDead()) {
         return;
      }

      // Green if it's our own ship
      if (selectedEntity.userId == Global.player.userId) {
         setColors(Util.getColor(90, 255, 90));
      } else {
         // Calculate the time since the last shot was fired
         float timeSinceFired = Time.time - ourShip.getLastAttackTime();

         // If we've recently fired, or are ready to fire again, show red
         if (ourShip.hasReloaded() || timeSinceFired < 1f) {
            setColors(Util.getColor(255, 90, 90));
         }
      }
   }

   protected void setColors (Color color) {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.color = color;
      }
   }

   protected Vector3 getOffset (NetEntity selectedEntity) {
      // Default for Ships
      return new Vector2(0f, 0f);
   }

   #region Private Variables

   // A convenient list collection of our arrows
   protected List<SpriteRenderer> _arrows = new List<SpriteRenderer>();

   #endregion
}
