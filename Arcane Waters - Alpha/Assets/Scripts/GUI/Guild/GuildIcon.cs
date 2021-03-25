﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GuildIcon : MonoBehaviour
{
   #region Public Variables

   // The directories for the layer sprites
   public static string BORDER_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons/Borders/";
   public static string MASK_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons/Masks/";
   public static string BACKGROUND_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons/Backgrounds/";
   public static string SIGIL_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons/Sigils/";

   public static string BORDER_SMALL_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons Small/Borders/";
   public static string MASK_SMALL_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons Small/Masks/";
   public static string BACKGROUND_SMALL_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons Small/Backgrounds/";
   public static string SIGIL_SMALL_PATH = "Assets/Resources/Sprites/GUI/Guild/Icons Small/Sigils/";

   public static string BLANK_SPRITE_PATH = "Assets/Resources/Sprites/";

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

   // The tooltip displayed when hovering the icon
   public ToolTipComponent tooltip;

   // Reference to the tooltipped icon which user is currently interacting with
   public static GuildIcon tooltippedIconRef;

   #endregion

   public void Awake () {
      // We need to manually create material instances for the recolor to work
      backgroundRecolored.setNewMaterial(MaterialManager.self.getGUIMaterial());
      sigilRecolored.setNewMaterial(MaterialManager.self.getGUIMaterial());
   }

   public void initialize (GuildInfo guildInfo) {
      if (guildInfo != null && guildInfo.guildId > 0) {
         show();
         setBorder(guildInfo.iconBorder);
         setBackground(guildInfo.iconBackground, guildInfo.iconBackPalettes);
         setSigil(guildInfo.iconSigil, guildInfo.iconSigilPalettes);

         tooltip.message = guildInfo.guildName;
         tooltip.tooltipType = ToolTipComponent.Type.DynamicText;

         // Download guild name from database if passed guildName was empty
         if (tooltip.message == "" || tooltip.message == null) {
            tooltippedIconRef = this;
            tooltip.message = "...";
            if (Global.player) {
               Global.player.rpc.Cmd_GetGuildNameInGuildIconTooltip(guildInfo.guildId);
            }
         }
      } else {
         hide();
      }
   }

   public void setGuildName(string name) {
      tooltip.message = name;
      if (tooltip.isActive()) {
         tooltip.OnPointerEnter(null);
      }
   }

   public void setBorder (string borderName) {
      border.sprite = getBorderSprite(borderName);
      mask.sprite = getMaskSprite(borderName);
   }

   public void setBackground (string backgroundName, string palettes) {
      background.sprite = getBackgroundSprite(backgroundName);
      backgroundRecolored.recolor(palettes);

      // The mask applied to the image uses a copy of the material to draw
      // To force an update, we disable and enable the image
      background.enabled = false;
      background.enabled = true;
   }

   public void setSigil (string sigilName, string palettes) {
      sigil.sprite = getSigilSprite(sigilName);
      sigilRecolored.recolor(palettes);

      // The mask applied to the image uses a copy of the material to draw
      // To force an update, we disable and enable the image
      sigil.enabled = false;
      sigil.enabled = true;
   }

   public void show () {
      if (canvasGroup.alpha < 1f) {
         canvasGroup.Show();
      }
   }

   public void hide () {
      if (canvasGroup.alpha > 0f) {
         canvasGroup.Hide();
      }
   }

   protected virtual Sprite getBorderSprite (string borderName) {
      if (borderName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(BORDER_PATH + borderName);
   }

   protected virtual Sprite getMaskSprite (string borderName) {
      if (borderName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(MASK_PATH + borderName + "_mask");
   }

   protected virtual Sprite getBackgroundSprite (string backgroundName) {
      if (backgroundName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return background.sprite = ImageManager.getSprite(BACKGROUND_PATH + backgroundName);
   }

   protected virtual Sprite getSigilSprite (string sigilName) {
      if (sigilName == null) {
         return ImageManager.getSprite(BLANK_SPRITE_PATH + "empty_layer");
      }
      return ImageManager.getSprite(SIGIL_PATH + sigilName);
   }

   #region Private Variables

   #endregion
}
