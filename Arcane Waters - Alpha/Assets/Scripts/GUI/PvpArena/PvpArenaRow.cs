using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;
using TMPro;

public class PvpArenaRow : MonoBehaviour
{
   #region Public Variables

   // The voyage id
   public TextMeshProUGUI idText;

   // The area name
   public TextMeshProUGUI areaText;

   // The player count
   public TextMeshProUGUI playerCount;

   // The time since the instance was created
   public TextMeshProUGUI timeText;

   // The state of the pvp game
   public TextMeshProUGUI gameState;

   // The minimap image
   public Image minimapImage;

   // A default minimap image used when the minimap doesn't exist
   public Sprite defaultMinimap;

   // The row backgrounds
   public Image backgroundImage;
   public Image backgroundTopImage;
   public Image backgroundBottomImage;

   // The backgrounds used when the arena can or cannot be joined
   public Sprite canBeJoinedBackground;
   public Sprite cannotBeJoinedBackground;

   // The row button
   public Button button;

   // The texts that must become greyed out when the arena cannot be joined
   public TextMeshProUGUI[] textsToGreyOut;

   // The texts that must get a red shadow when the arena cannot be joined
   public TextMeshProUGUI[] textsToShadowRed;

   // The text materials
   public Material greyedOutTextMaterial;
   public Material redShadowTextMaterial;
   public Material whiteTextMaterial;
   public Material greenShadowTextMaterial;

   // The voyage data
   [HideInInspector]
   public Voyage voyage;

   #endregion

   public void setRowForVoyage (Voyage voyage, bool isFirstRow, bool isLastRow) {
      this.voyage = voyage;
      idText.text = "ID: " + voyage.voyageId.ToString();
      areaText.text = voyage.areaName;
      playerCount.text = voyage.playerCount.ToString() + "/" + voyage.pvpGameMaxPlayerCount.ToString();
      timeText.text = DateTime.UtcNow.Subtract(DateTime.FromBinary(voyage.creationDate)).ToString(@"mm\:ss");
      gameState.text = PvpGame.getGameStateLabel(voyage.pvpGameState);

      // The first and last row have extra decorations
      backgroundTopImage.enabled = false;
      backgroundBottomImage.enabled = false;
      if (isFirstRow) {
         backgroundTopImage.enabled = true;
      } else if (isLastRow) {
         backgroundBottomImage.enabled = true;
      }

      // Try to find an existing minimap
      minimapImage.sprite = ImageManager.getSprite("Minimaps/" + voyage.areaKey, true);
      if (minimapImage.sprite == null || minimapImage.sprite == ImageManager.self.blankSprite) {
         // If no minimap exist, search for a placeholder one
         minimapImage.sprite = ImageManager.getSprite("GUI/Pvp Arena/" + voyage.areaKey, true);
         if (minimapImage.sprite == null || minimapImage.sprite == ImageManager.self.blankSprite) {
            minimapImage.sprite = defaultMinimap;
         }
      }
      
      if (PvpArenaInfoPanel.canPvpArenaBeJoined(voyage)) {
         backgroundImage.sprite = canBeJoinedBackground;
         button.interactable = true;
         setTextMaterial(textsToGreyOut, whiteTextMaterial);
         setTextMaterial(textsToShadowRed, greenShadowTextMaterial);
      } else {
         backgroundImage.sprite = cannotBeJoinedBackground;
         button.interactable = false;
         setTextMaterial(textsToGreyOut, greyedOutTextMaterial);
         setTextMaterial(textsToShadowRed, redShadowTextMaterial);
      }
   }

   public void onRowPressed () {
      ((PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena)).onPvpArenaRowPressed(voyage);
   }

   private void setTextMaterial (TextMeshProUGUI[] textArray, Material material) {
      if (textArray == null || textArray.Length == 0) {
         return;
      }

      foreach (TextMeshProUGUI text in textArray) {
         text.fontSharedMaterial = material;
      }
   }

   #region Private Variables

   #endregion
}

