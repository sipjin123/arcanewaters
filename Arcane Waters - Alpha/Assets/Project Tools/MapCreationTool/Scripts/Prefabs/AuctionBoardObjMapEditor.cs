using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class AuctionBoardObjMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      [SerializeField]
      private SpriteRenderer highlight = null;

      public override void createdForPreview () {
         setDefaultSprite();
      }

      public void dataFieldChanged (DataField field) {

      }

      public void setDefaultSprite () {
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setSpriteOutline(highlight, hovered, selected, deleting);
      }
   }
}