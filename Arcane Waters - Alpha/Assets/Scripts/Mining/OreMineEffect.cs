using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class OreMineEffect : MonoBehaviour {
   #region Public Variables

   // The ore being animated
   public Transform animatingObj;

   // The sprite renderer reference
   public SpriteRenderer spriteRender;

   // The list of ores and their associated sprites
   public List<OreSprite> oreSpriteList;

   // The animator component
   public Animator animator;

   // The ore node reference
   public OreNode oreNode;

   // The id of the mine effect
   public int oreEffectId;

   #endregion

   public void setSprite (OreNode.Type oreType) {
      OreSprite cropSprite = oreSpriteList.Find(_ => _.oreType == oreType);
      if (cropSprite != null) {
         spriteRender.sprite = cropSprite.sprite;
      }

      Invoke("endAnim", 1);
   }

   public void endAnim () {
      GameObject spawnedObj = Instantiate(PrefabsManager.self.orePickupPrefab);
      OrePickup orePickup = spawnedObj.GetComponent<OrePickup>();
      orePickup.spriteRender.sprite = spriteRender.sprite;
      orePickup.oreNode = oreNode;
      orePickup.oreEffectId = oreEffectId;
      oreNode.orePickupCollection.Add(oreEffectId, orePickup);

      spawnedObj.transform.position = animatingObj.position;
      spawnedObj.transform.rotation = animatingObj.rotation;
      spawnedObj.transform.localScale = new Vector3(transform.localScale.x, 1, 1);
      gameObject.SetActive(false);
   }

   #region Private Variables

   #endregion
}

[Serializable]
public class OreSprite
{
   // The ore type
   public OreNode.Type oreType;

   // The sprite associated with the ore type
   public Sprite sprite;
}