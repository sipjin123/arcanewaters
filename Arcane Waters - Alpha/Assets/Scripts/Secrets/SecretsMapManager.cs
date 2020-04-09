using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using System;

public class SecretsMapManager : MonoBehaviour {
   #region Public Variables

   // Self reference
   public static SecretsMapManager instance;

   #endregion

   private void Awake () {
      instance = this;
   }

   public SelectOption[] formSelectionOptions () {
      List<SelectOption> selectionList = new List<SelectOption>();
      foreach (SecretType category in Enum.GetValues(typeof(SecretType))) {
         selectionList.Add(new SelectOption(((int) category).ToString(), category.ToString()));
      }
      return selectionList.ToArray();
   }

   public SelectOption[] formInitialSprite () {
      List<SelectOption> selectionList = new List<SelectOption>();
      List<ImageManager.ImageData> initSprites = ImageManager.self.imageDataList.FindAll(_ => _.imagePath.Contains("Sprites/Secrets"));
      foreach (ImageManager.ImageData initSprite in initSprites) {
         selectionList.Add(new SelectOption(initSprite.imagePath, initSprite.imageName));
      }
      return selectionList.ToArray();
   }

   #region Private Variables

   #endregion
}

public enum SecretType {
   None = 0,
   Bookcase = 1,
   Stone = 2
}