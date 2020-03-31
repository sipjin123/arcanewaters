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

   // The user who interacted this ore
   public int ownerId;

   // The voyage group that owns this ore
   public int voyageGroupId;

   #endregion

   public void initData (int ownerId, int voyageGroupId, int oreEffectId, OreNode oreNode, float randomSpeed) {
      this.ownerId = ownerId;
      this.voyageGroupId = voyageGroupId;
      this.oreEffectId = oreEffectId;
      this.oreNode = oreNode;
      animator.speed = randomSpeed;
   }

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
      orePickup.initData(ownerId, voyageGroupId, oreEffectId, oreNode, spriteRender.sprite);
      oreNode.orePickupCollection.Add(oreEffectId, orePickup);

      spawnedObj.transform.position = animatingObj.position;
      spawnedObj.transform.rotation = animatingObj.rotation;
      spawnedObj.transform.localScale = new Vector3(transform.localScale.x, 1, 1);
      gameObject.SetActive(false);
      Destroy(this.gameObject);
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