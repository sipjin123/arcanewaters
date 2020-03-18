using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropHarvest : MonoBehaviour {
   #region Public Variables

   // The crop spot associated with this pickable crop
   public CropSpot cropSpot;

   // The crop being animated
   public Transform animatingObj;

   #endregion

   public void endAnim () {
      GameObject spawnedObj = Instantiate(PrefabsManager.self.cropPickupPrefab);
      spawnedObj.GetComponent<CropPickup>().cropSpot = cropSpot;
      spawnedObj.transform.position = animatingObj.position;
      gameObject.SetActive(false);
   }

   #region Private Variables
      
   #endregion
}
