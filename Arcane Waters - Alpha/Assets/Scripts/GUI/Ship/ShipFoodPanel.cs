using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipFoodPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The amount of food represented by one unit
   public static int FOOD_PER_UNIT = 100;

   // The maximum number of food units that will be displayed
   public static int MAX_UNITS = 18;

   // The prefab we use for creating food units
   public FoodUnit unitPrefab;

   // The container of food objects
   public GameObject foodContainer;

   // The canvas group component
   public CanvasGroup canvasGroup;

   // Self
   public static ShipFoodPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      foodContainer.DestroyChildren();

      for (int i = 0; i < MAX_UNITS; i++) {
         FoodUnit unit = Instantiate(unitPrefab, foodContainer.transform, false);
         unit.deactivate();
         _foods.Add(unit);
      }
   }

   private void Update () {
      // Only enable at sea in open world
      if (Global.player == null || !Global.player.isPlayerShip() || !WorldMapManager.isWorldMapArea(Global.player.areaKey)) {
         hide();
         return;
      }

      show();

      if (Global.player.currentFood == _lastFood && Global.player.maxFood == _lastMaxFood) {
         return;
      }

      float foodPerUnit = getFoodPerUnit();
      int unitsNeeded = Mathf.RoundToInt(Global.player.maxFood / foodPerUnit);

      for (int i = 0; i < _foods.Count; i++) {
         if (i < unitsNeeded) {
            _foods[i].activate();
         } else {
            _foods[i].deactivate();
         }

         float hpLeft = (Global.player.currentFood - i * foodPerUnit);
         float hpF = Mathf.Clamp01(hpLeft / foodPerUnit);
         _foods[i].setAmountLeft(hpF);
      }

      int curUnits = Mathf.CeilToInt(Global.player.currentFood / foodPerUnit);
      int prevUnits = Mathf.CeilToInt(_lastFood / foodPerUnit);

      if (prevUnits - curUnits == 1 && curUnits < unitsNeeded && prevUnits <= unitsNeeded) {
         for (int i = 0; i < _foods.Count; i++) {
            if (_foods[i].gameObject.activeSelf) {
               _foods[i].blink();
            }
         }
      }

      _lastFood = Global.player.currentFood;
      _lastMaxFood = Global.player.maxFood;

   }

   private void hide () {
      if (canvasGroup.IsShowing()) {
         _lastFood = 0;
         canvasGroup.alpha = 0;

         // Deactivate all sailors so the layout group can shrink
         for (int i = 0; i < _foods.Count; i++) {
            _foods[i].deactivate();
         }
      }
   }

   private void show () {
      if (!canvasGroup.IsShowing()) {
         canvasGroup.alpha = 1;
      }
   }

   private float getFoodPerUnit () {

      // If max food is small enough to be shown on screen with default FOOD_PER_UNIT, don't change it
      if (Global.player.maxHealth <= MAX_UNITS * FOOD_PER_UNIT) {
         return FOOD_PER_UNIT;

         // If max food is too large to be shown on screen with default FOOD_PER_UNIT, we will scale FOOD_PER_UNIT to fit
      } else {
         return (float) Global.player.maxHealth / MAX_UNITS;
      }
   }

   #region Private Variables

   // The list of all food objects
   private List<FoodUnit> _foods = new List<FoodUnit>();

   // The last registered ship food value
   private float _lastFood = 0, _lastMaxFood;

   #endregion
}
