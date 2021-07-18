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

   public void populateShop (List<PvpShopData> pvpDataList) {
      shopTemplateHolder.gameObject.DestroyChildren();

      foreach (PvpShopData shopData in pvpDataList) {
         PvpShopTemplate shopTemplate = Instantiate(shopTemplatePrefab, shopTemplateHolder);
         shopTemplate.setupData(shopData);
         shopTemplate.buyButton.onClick.AddListener(() => {
            D.debug("Attempted to buy");
         });
      }
   }

   #region Private Variables
      
   #endregion
}
