using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropHarvest : MonoBehaviour {
   #region Public Variables

   // The crop spot associated with this pickable crop
   public CropSpot cropSpot;

   // The crop being animated
   public Transform animatingObj;

   // The sprite renderer reference
   public SpriteRenderer spriteRender;

   // The list of crops and their associated sprites
   public List<CropSprite> cropSpriteList;

   // Animator component
   public Animator animator;

   #endregion

   public void setSprite (Crop.Type cropType) {
      CropSprite cropSprite = cropSpriteList.Find(_ => _.cropType == cropType);
      if (cropSprite != null) {
         spriteRender.sprite = cropSprite.sprite;
      }

      Invoke("endAnim", 1);
   }

   public void endAnim () {
      GameObject spawnedObj = Instantiate(PrefabsManager.self.cropPickupPrefab);
      spawnedObj.GetComponent<CropPickup>().cropSpot = cropSpot;
      spawnedObj.GetComponent<CropPickup>().spriteRender.sprite = spriteRender.sprite;
      spawnedObj.transform.position = animatingObj.position;
      spawnedObj.transform.rotation = animatingObj.rotation;
      spawnedObj.transform.localScale = new Vector3(transform.localScale.x, 1, 1);
      gameObject.SetActive(false);
   }

   #region Private Variables
      
   #endregion
}

[Serializable]
public class CropSprite
{
   // The crop type
   public Crop.Type cropType;

   // The sprite associated with the crop type
   public Sprite sprite;
}