using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpShopPanel : Panel {
   #region Public Variables

   // Prefab spawning components
   public PvpShopTemplate shopTemplatePrefab;
   public Transform shopTemplateHolder;

   // Self
   public static PvpShopPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void populateShop (List<PvpShopItem> pvpItemDataList) {
      shopTemplateHolder.gameObject.DestroyChildren();

      foreach (PvpShopItem shopItemData in pvpItemDataList) {
         PvpShopTemplate shopTemplate = Instantiate(shopTemplatePrefab, shopTemplateHolder);
         shopTemplate.setupData(shopItemData);
         shopTemplate.buyButton.onClick.AddListener(() => {
            D.debug("Attempted to buy");
         });
      }
   }

   #region Private Variables
      
   #endregion
}
