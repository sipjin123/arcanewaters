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

   // The sprite renderer outline reference
   public SpriteRenderer oreOutline;
   
   // The sprite renderer reference
   public SpriteRenderer spriteRender;

   // The animator component
   public Animator animator;

   // The ore node reference
   public OreNode oreNode;

   // The id of the mine effect
   public int oreEffectId;

   // The user who interacted this ore
   public int ownerId;

   // Set to true if ore is a bonus ore
   public bool isBonusOre;

   // The voyage group that owns this ore
   public int voyageGroupId;

   // Refrence to the shadow
   public Transform shadow;

   #endregion

   public void initData (int ownerId, int voyageGroupId, int oreEffectId, OreNode oreNode, float randomSpeed, bool isBonus) {
      this.ownerId = ownerId;
      this.voyageGroupId = voyageGroupId;
      this.oreEffectId = oreEffectId;
      this.oreNode = oreNode;
      isBonusOre = isBonus;
      animator.speed = randomSpeed;

      oreOutline.gameObject.SetActive(isBonusOre);
      //SoundEffectManager.self.playSoundEffect(SoundEffectManager.ORE_DROP, transform);
   }

   private void LateUpdate () {
      shadow.transform.eulerAngles = new Vector3(0, 0, 0);
   }
   
   public void setOreSprite (Sprite oreSprite) {
      spriteRender.sprite = oreSprite;
      oreOutline.sprite = oreSprite;
   }

   public void endAnim () {
      if (!oreNode.orePickupCollection.ContainsKey(oreEffectId)) {
         GameObject spawnedObj = Instantiate(PrefabsManager.self.orePickupPrefab, oreNode.transform);
         OrePickup orePickup = spawnedObj.GetComponent<OrePickup>();
         if (isBonusOre) {
            Instantiate(PrefabsManager.self.sparkleBurstPrefab, spawnedObj.transform);
         }

         orePickup.initData(ownerId, voyageGroupId, oreEffectId, oreNode, spriteRender.sprite);

         oreNode.orePickupCollection.Add(oreEffectId, orePickup);

         orePickup.spriteRender.sprite = OreManager.self.getSprite(oreNode.oreType);
         spawnedObj.transform.position = animatingObj.position;
         spawnedObj.transform.localScale = new Vector3(transform.localScale.x, 1, 1);
         gameObject.SetActive(false);
      } else {
         D.debug("Error here! Ore pickup collection already contains {" + oreEffectId + "}");
      }
      
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