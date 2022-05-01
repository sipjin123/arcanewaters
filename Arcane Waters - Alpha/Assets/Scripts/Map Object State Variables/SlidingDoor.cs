using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class SlidingDoor : VaryingStateObject, IBiomable
{
   #region Public Variables

   [Header("Door")]
   // Parts of door we need to adjust for the width
   public Transform rightPost;
   public SpriteRenderer doorSprite;
   public SpriteRenderer shadowSprite;

   // Parts we need to adjust according to biome
   public SpriteRenderer leftPostRenderer;
   public SpriteRenderer rightPostRenderer;

   // Collider we use to block entrance
   public EdgeCollider2D entranceCollider;

   // The door we lift
   public Transform door;

   // All sprite animations part of lifting door
   public SpriteRendererAnimator[] doorLiftAnimators = new SpriteRendererAnimator[0];

   // Sprites for the wooden corner posts
   public BiomableSprite postSprites;

   #endregion

   private void Update () {
      //-0.315
      //1.815
      if (NetworkClient.active) {
         Vector3 prevPos = door.transform.localPosition;

         door.transform.localPosition = new Vector3(
            door.transform.localPosition.x,
            Mathf.MoveTowards(door.transform.localPosition.y, state.Equals("down") ? -0.315f : 1.815f, Time.deltaTime * 2f),
            door.transform.localPosition.z);

         foreach (SpriteRendererAnimator anim in doorLiftAnimators) {
            anim.playBackwards = state.Equals("down");
            anim.paused = prevPos == door.transform.localPosition;
         }
      }

      entranceCollider.enabled = state.Equals("down");
   }

   public void setWidth (int width) {
      width = Mathf.Clamp(width, 0, 100);

      doorSprite.size = new Vector2(2.5f + width * 0.5f, doorSprite.size.y);
      doorSprite.transform.localPosition = new Vector3(
         width * 0.25f - 0.05f,
         doorSprite.transform.localPosition.y,
         doorSprite.transform.localPosition.z);

      rightPost.localPosition = new Vector3(
         width * 0.5f + 0.7f,
         rightPost.localPosition.y,
         rightPost.localPosition.z);

      shadowSprite.size = new Vector2(2.5f + width * 0.5f, shadowSprite.size.y);
      shadowSprite.transform.localPosition = new Vector3(
         width * 0.25f - 0.05f,
         shadowSprite.transform.localPosition.y,
         shadowSprite.transform.localPosition.z
         );

      Vector2[] p = entranceCollider.points;
      p[1] = new Vector2(width * 0.5f + 0.5f, p[1].y);
      entranceCollider.points = p;
   }

   protected override void receiveMapEditorData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.Equals(DataField.SLIDING_DOOR_WIDTH_KEY)) {
            if (field.tryGetIntValue(out int val)) {
               setWidth(val);
            }
         }
      }
   }

   public void setBiome (Biome.Type biomeType) {
      leftPostRenderer.sprite = postSprites.get(biomeType);
      rightPostRenderer.sprite = postSprites.get(biomeType);
   }

   #region Private Variables

   #endregion
}
