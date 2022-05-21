using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class PvpNpcMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      #region Public Variables

      // Scale of this object when displayed in the palette panel
      public const float PALETTE_SCALE = 4.0f;

      // The starting scale of the model if its not fixed to one
      public float startingScale = 1;

      // The direction the NPC will be facing
      public Direction facingDirection = Direction.South;

      // A reference to the sprite renderer for this NPC
      public SpriteRenderer npcSprite;

      // A reference to the simple animation component for this NPC
      public SimpleAnimation npcAnimation;

      #endregion

      private void Awake () {
         _outline = GetComponentInChildren<SpriteOutline>();
         startingScale = transform.localScale.x;
      }

      public void dataFieldChanged (DataField field) {
         // Adjust the scale of this building when spawned in the drawing board
         if (transform.parent.GetComponent<Palette>() != null) {
            transform.localScale = new Vector3(startingScale * PALETTE_SCALE, startingScale * PALETTE_SCALE, 1);
         } else {
            transform.localScale = new Vector3(startingScale, startingScale, startingScale);
         }

         if (field.k.CompareTo(DataField.NPC_DIRECTION_KEY) == 0) {
            try {
               Direction newDirection = (Direction) System.Enum.Parse(typeof(Direction), field.v.Split(':')[0]);
               facingDirection = newDirection;

               int directionIndex = (int) newDirection;
               npcAnimation.minIndex = _idleStartFramesByDirection[directionIndex];
               npcAnimation.maxIndex = _idleEndFramesByDirection[directionIndex];

               if (facingDirection == Direction.West) {
                  npcSprite.flipX = true;
               } else {
                  npcSprite.flipX = false;
               }
            } catch {

            }
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(_outline, hovered, selected, deleting);
      }

      #region Private Variables

      // A reference to our outline
      private SpriteOutline _outline;

      // Start and end frames for idle animations, indexed by direction enum
      private readonly int[] _idleStartFramesByDirection = { 4, 0, 0, 0, 8, 0, 0, 0 };
      private readonly int[] _idleEndFramesByDirection = { 7, 3, 3, 3, 11, 3, 3, 3 };

      #endregion
   }
}


