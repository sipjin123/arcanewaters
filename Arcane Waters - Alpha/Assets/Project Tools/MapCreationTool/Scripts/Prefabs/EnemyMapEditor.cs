using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class EnemyMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      [SerializeField]
      private bool isSea = false;

      // Main sprite renderer used for the body of the enemy
      public SpriteRenderer ren;

      private SpriteOutline outline;

      private void Awake () {
         ren = GetComponentInChildren<SpriteRenderer>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(isSea ? DataField.SEA_ENEMY_DATA_KEY : DataField.LAND_ENEMY_DATA_KEY) == 0) {
            Texture2D texture = isSea
               ? MonsterManager.instance.getSeaMonsterTexture(int.Parse(field.v.Split(':')[0]))
               : MonsterManager.instance.getLandMonsterTexture(int.Parse(field.v.Split(':')[0]));

            if (texture != null) {
               ren.sprite = ImageManager.getSprites(texture)[0];
            }
         }
      }

      public override void createdInPalette () {
         setDefaultSprite();
         if (isSea) {
            ren.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            transform.localScale = Vector3.one * 0.5f;
         }
      }

      public override void createdForPreview () {
         setDefaultSprite();
         if (isSea) {
            ren.maskInteraction = SpriteMaskInteraction.None;
            transform.localScale = Vector3.one;
         }
      }

      public override void placedInEditor () {
         if (isSea) {
            ren.maskInteraction = SpriteMaskInteraction.None;
            transform.localScale = Vector3.one;
         }
      }

      public void setDefaultSprite () {
         Texture2D texture = isSea
            ? MonsterManager.instance.getFirstSeaMonsterTexture()
            : MonsterManager.instance.getFirstLandMonsterTexture();

         if (texture != null) {
            ren.sprite = ImageManager.getSprites(texture)[0];
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(outline, hovered, selected, deleting);
      }
   }
}
