using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CharacterPortrait : MonoBehaviour
{
   #region Public Variables

   // The character stack
   public CharacterStack characterStack;

   // The icon displayed when the portrait info is not available
   public GameObject unknownIcon;

   // The background image
   public Image backgroundImage;

   // The background sprites
   public Sprite seaBackground;
   public Sprite landBackground;
   public Sprite combatBackground;
   public Sprite unknownBackground;

   #endregion

   public void Awake () {
      characterStack.pauseAnimation();
   }

   public void initialize (NetEntity entity) {
      // If the entity is null, display a question mark
      if (entity == null) {
         unknownIcon.SetActive(true);
         backgroundImage.sprite = unknownBackground;
         return;
      }
      
      // Update the character stack
      characterStack.updateLayers(entity);
      characterStack.setDirection(Direction.East);

      // Set the background
      updateBackground(entity);

      // Hide the question mark icon
      unknownIcon.SetActive(false);
   }

   public void updateBackground (NetEntity entity) {
      if (entity.hasAnyCombat()) {
         backgroundImage.sprite = combatBackground;
      } else if (entity is SeaEntity) {
         backgroundImage.sprite = seaBackground;
      } else {
         backgroundImage.sprite = landBackground;
      }
   }

   #region Private Variables

   #endregion
}
