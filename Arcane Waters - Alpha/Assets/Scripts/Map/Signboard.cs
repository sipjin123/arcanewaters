using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public abstract class Signboard : ToolTipSign, IBiomable
{
   #region Public Variables

   // The sort point of the sign
   public Transform sortPoint;

   // Renderer for main signboard sprite
   public SpriteRenderer signboardRen;

   // Sprite for signboard in various biomes
   public BiomableSprite signboardSprites;

   #endregion

   protected abstract void onClick ();

   protected override void Awake () {
      base.Awake();

      _outline = GetComponent<SpriteOutline>();
      _outline.setNewColor(Color.white);
      _outline.setVisibility(false);
   }

   public override void toggleToolTip (bool isActive) {
      base.toggleToolTip(isActive);

      _outline.setVisibility(isActive);
   }

   public void onClickSignboard () {
      if (Global.player == null) {
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(sortPoint.position, Global.player.transform.position) > NPC.TALK_DISTANCE) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .5f)).asTooFar();
         return;
      }

      onClick();
   }

   public void setBiome (Biome.Type biomeType) {
      signboardRen.sprite = signboardSprites.get(biomeType);
   }

   #region Private Variables

   // The outline of the sign
   protected SpriteOutline _outline;

   #endregion
}
