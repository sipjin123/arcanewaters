using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CargoBox : MonoBehaviour
{
   #region Public Variables

   // The Text that contains the cargo count
   public Text cargoCountText;

   // The Image that shows the cargo type
   public Image cargoImage;

   // The Cargo Type
   public Crop.Type cropType;

   #endregion

   // Cargo boxes have been disabled as crops were made into regular items
   //public void updateBox (SiloInfo siloInfo) {
   //   cargoCountText.text = siloInfo.cropCount+"";
   //   cargoImage.sprite = ImageManager.getSprite("Cargo/" + siloInfo.cropType);
   //   cropType = siloInfo.cropType;

   //}

   #region Private Variables

   #endregion
}
