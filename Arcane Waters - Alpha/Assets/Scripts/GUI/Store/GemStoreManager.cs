using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class GemStoreManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static GemStoreManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Create the various items for the store
      createHairStyleBoxes();
      createHaircutBoxes();
      createShipSkinBoxes();
   }

   public StoreItemBox getItemBox (int itemId) {
      foreach (StoreItemBox box in GetComponentsInChildren<StoreItemBox>()) {
         if (box.itemId == itemId) {
            return box;
         }
      }

      foreach (StoreItemBox box in StoreScreen.self.GetComponentsInChildren<StoreItemBox>()) {
         if (box.itemId == itemId) {
            return box;
         }
      }

      return null;
   }

   protected void createHairStyleBoxes () {
      // Define the colors we want
      List<ColorType> colors = new List<ColorType>() {
         ColorType.LightGreen, ColorType.LightBlue, ColorType.LightPurple, ColorType.White,
         ColorType.Teal, ColorType.Orange, ColorType.DarkPink, ColorType.LightYellow,
         ColorType.DarkRed, ColorType.DarkBlue, ColorType.DarkGreen, ColorType.LightGray
      };

      // Create an instance from the prefab for each
      foreach (ColorType color in colors) {
         StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
         box.transform.SetParent(this.transform);
         box.itemId = getNextId();
         box.itemName = ColorDef.get(color).colorName;
         box.itemCost = 15;
         box.itemDescription = "This dye will allow you to change your hair color!";
         box.colorType = color;
      }
   }

   protected void createHaircutBoxes () {
      // Define the types we want
      List<HairLayer.Type> haircuts = new List<HairLayer.Type>() {
         HairLayer.Type.Female_Hair_1, HairLayer.Type.Female_Hair_2, HairLayer.Type.Female_Hair_3, HairLayer.Type.Female_Hair_4,
         HairLayer.Type.Female_Hair_5, HairLayer.Type.Female_Hair_6, HairLayer.Type.Female_Hair_7, HairLayer.Type.Female_Hair_8,
         HairLayer.Type.Female_Hair_9, HairLayer.Type.Female_Hair_10,

         HairLayer.Type.Male_Hair_2, HairLayer.Type.Male_Hair_3, HairLayer.Type.Male_Hair_4,
         HairLayer.Type.Male_Hair_5, HairLayer.Type.Male_Hair_6, HairLayer.Type.Male_Hair_7, HairLayer.Type.Male_Hair_8,
         HairLayer.Type.Male_Hair_9,
      };

      // Create an instance from the prefab for each
      foreach (HairLayer.Type haircut in haircuts) {
         StoreHaircutBox box = Instantiate(PrefabsManager.self.haircutBoxPrefab);
         box.transform.SetParent(this.transform);
         box.itemId = getNextId();
         box.itemName = "Haircut";
         box.itemCost = 15;
         box.itemDescription = "You'll look amazing with this new haircut!";
         box.hairType = haircut;
      }
   }

   protected void createShipSkinBoxes () {
      // Define the skins we want to show up in the store
      List<Ship.SkinType> skinTypes = new List<Ship.SkinType>() {
         Ship.SkinType.Caravel_Fancy
      };

      // TESTING
      skinTypes = System.Enum.GetValues(typeof(Ship.SkinType)).OfType<Ship.SkinType>().ToList();
      skinTypes.Remove(Ship.SkinType.None);

      // Create an instance from the prefab for each ship skin
      foreach (Ship.SkinType skinType in skinTypes) {
         string shipClass = skinType.ToString().Split('_')[0];
         string skinName = skinType.ToString().Split('_')[1];

         StoreShipBox box = Instantiate(PrefabsManager.self.shipBoxPrefab);
         box.transform.SetParent(this.transform);
         box.itemId = getNextId();
         box.itemName = skinName;
         box.itemCost = 20;
         box.itemDescription = "This paint will make your " + shipClass + " look incredible!";
         box.skinType = skinType;
      }
   }

   public StoreHairDyeBox[] getHairstyles () {
      return GetComponentsInChildren<StoreHairDyeBox>();
   }

   public StoreHaircutBox[] getHaircuts () {
      return GetComponentsInChildren<StoreHaircutBox>();
   }

   public StoreShipBox[] getShipSkins () {
      return GetComponentsInChildren<StoreShipBox>();
   }

   protected int getNextId () {
      int maxSoFar = 0;

      // Find the highest item ID in the store so far
      foreach (StoreItemBox box in this.GetComponentsInChildren<StoreItemBox>()) {
         maxSoFar = Mathf.Max(maxSoFar, box.itemId);
      }

      return maxSoFar + 1;
   }

   #region Private Variables

   #endregion
}
