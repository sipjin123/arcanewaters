using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GuildIcon : MonoBehaviour
{
   #region Public Variables

   // The directories for the layer sprites
   public static string BORDER_PATH = "Assets/Sprites/GUI/Guild/Borders/";
   public static string MASK_PATH = "Assets/Sprites/GUI/Guild/Masks/";
   public static string BACKGROUND_PATH = "Assets/Sprites/GUI/Guild/Backgrounds/";
   public static string SIGIL_PATH = "Assets/Sprites/GUI/Guild/Sigils/";

   // The icon layer images
   public Image border;
   public Image mask;
   public Image background;
   public Image sigil;

   // The Recolored components
   public RecoloredSprite backgroundRecolored;
   public RecoloredSprite sigilRecolored;

   // Our canvas group
   public CanvasGroup canvasGroup;

   #endregion

   public void Awake () {
      // We need to manually create material instances for the recolor to work
      backgroundRecolored.setNewMaterial(MaterialManager.self.getGUIMaterial());
      sigilRecolored.setNewMaterial(MaterialManager.self.getGUIMaterial());
   }

   public void setBorder (string borderName) {
      border.sprite = ImageManager.getSprite(BORDER_PATH + borderName);
      mask.sprite = ImageManager.getSprite(MASK_PATH + borderName + "_mask");
   }

   public void setBackground (string backgroundName, string palette1, string palette2) {
      background.sprite = ImageManager.getSprite(BACKGROUND_PATH + backgroundName);
      backgroundRecolored.recolor(palette1, palette2);

      // The mask applied to the image uses a copy of the material to draw
      // To force an update, we disable and enable the image
      background.enabled = false;
      background.enabled = true;
   }

   public void setSigil (string sigilName, string palette1, string palette2) {
      sigil.sprite = ImageManager.getSprite(SIGIL_PATH + sigilName);
      sigilRecolored.recolor(palette1, palette2);

      // The mask applied to the image uses a copy of the material to draw
      // To force an update, we disable and enable the image
      sigil.enabled = false;
      sigil.enabled = true;
   }

   public void show () {
      if (canvasGroup.alpha < 1f) {
         canvasGroup.alpha = 1f;
      }
   }

   public void hide () {
      if (canvasGroup.alpha > 0f) {
         canvasGroup.alpha = 0f;
      }
   }

   #region Private Variables

   #endregion
}
