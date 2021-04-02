using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class AttackRangeCircle : GenericGameManager {
   #region Public Variables

   // The mesh renderer component
   public MeshRenderer meshRenderer;

   // The animator component
   public Animator animator;

   // The alpha multiplier, set by the animator
   public float alphaMultiplier = 1f;

   // The radius multiplier, set by the animator
   public float radiusMultiplier = 1f;

   #endregion

   protected override void Awake () {
      base.Awake();
      // Get the material
      _circleMaterial = meshRenderer.material;

      // Store the base color
      _circleColor = _circleMaterial.GetColor("_Color");
   }

   public void update() {
      // Get the current area
      Area area = AreaManager.self.getArea(Global.player.areaKey);

      // Set the z position, above the sea and below the land
      if (area != null) {
         Util.setZ(transform, area.waterZ - 0.01f);
      }

      // Move to the player position
      Util.setXY(transform, Global.player.transform.position);

      // Update the position in the shader
      _circleMaterial.SetVector("_Position", transform.position);

      // Update the radius
      PlayerShipEntity playerShipEntity = (PlayerShipEntity) Global.player;
      _circleMaterial.SetFloat("_Radius", playerShipEntity.getAttackRange() * radiusMultiplier);

      // Update the color
      Color c = _circleColor;
      c.a = alphaMultiplier;
      _circleMaterial.SetColor("_Color", c);
   }

   public void show () {
      if (!meshRenderer.enabled) {
         meshRenderer.enabled = true;
         animator.SetBool("visible", true);
      }
   }

   public void hide () {
      if (meshRenderer.enabled) {
         meshRenderer.enabled = false;
         animator.SetBool("visible", false);
      }
   }

   #region Private Variables

   // The material that draws the circle
   private Material _circleMaterial;

   // The base circle color
   private Color _circleColor;

   #endregion
}