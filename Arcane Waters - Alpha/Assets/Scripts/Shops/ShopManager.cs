using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class ShopManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShopManager self;

   // Default Shop Name
   public static string DEFAULT_SHOP_NAME = "None";

   // If this is initialized
   public bool hasInitialized;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      ShipDataManager.self.finishedDataSetup.AddListener(() => checkIfDataSetupIsFinished());
      ShipAbilityManager.self.finishedDataSetup.AddListener(() => checkIfDataSetupIsFinished());
      ShopXMLManager.self.finishedDataSetup.AddListener(() => checkIfDataSetupIsFinished());
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => checkIfDataSetupIsFinished());
      PaletteSwapManager.self.paletteCompleteEvent.AddListener(() => checkIfDataSetupIsFinished());
   }

   private void checkIfDataSetupIsFinished () {
      // Initialize random generated ships only when ship data and ship abilities data are setup
      if ((ShipDataManager.self.hasInitialized && ShipDataManager.self.shipDataList.Count > 0) && ShipAbilityManager.self.hasInitialized && ShopXMLManager.self.hasInitialized) {
         InvokeRepeating(nameof(randomlyGenerateShips), 0f, (float) TimeSpan.FromHours(1).TotalSeconds);
      }

      // TODO: Confirm if palette swap manager is still needed for shop initialization
      if (ShopXMLManager.self.hasInitialized && EquipmentXMLManager.self.loadedAllEquipment && !hasInitialized) {// && PaletteSwapManager.self.getPaletteList().Count > 0) {
         hasInitialized = true;

         // Routinely change out the items
         InvokeRepeating(nameof(generateItemsFromXML), 0f, (float) TimeSpan.FromHours(1).TotalSeconds);

         // Initialize the crop offers
         initializeCropOffers();
      }
   }

   public Item getItem (int itemId) {
      if (_items.ContainsKey(itemId)) {
         return _items[itemId];
      }

      return null;
   }

   public ShipInfo getShip (int shipId) {
      if (_ships.ContainsKey(shipId)) {
         return _ships[shipId];
      }

      return null;
   }

   protected void generateItemsFromXML () {
      // If we've already generated something previously, we might not generate anything more this time
      if (_items.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= .75f) {
         return;
      }

      // Generate items for each of the areas
      foreach (string areaKey in AreaManager.self.getAreaKeys()) {
         Biome.Type biomeType = AreaManager.self.getDefaultBiome(areaKey);

         // Clear out the previous list
         _itemsByArea[areaKey] = new List<int>();

         // Make 3 new items
         for (int i = 0; i < 3; i++) {
            Item item = null;

            if (UnityEngine.Random.Range(0f, 1f) > .5f) {
               List<WeightedItem<int>> weaponsGenerated = getPossibleWeapons(biomeType);
               if (weaponsGenerated.Count > 0) {
                  int weaponType = weaponsGenerated.ChooseByRandom();
                  item = Weapon.generateRandom(_itemId++, weaponType);
               } else {
                  // TODO: Remove after bugfix confirmation
                  D.debug("========= NO weapon generated for biome: " + biomeType);
                  item = Weapon.generateRandom(_itemId++, 1);
               }
            } else {
               List<WeightedItem<int>> armorsGenerated = getPossibleArmor(biomeType);
               if (armorsGenerated.Count > 0) {
                  int armorType = armorsGenerated.ChooseByRandom();
                  item = Armor.generateRandom(_itemId++, armorType);
               } else {
                  // TODO: Remove after bugfix confirmation
                  D.debug("========= NO armor generated for biome: " + biomeType);
                  item = Armor.generateRandom(_itemId++, 1);
               }
            }

            // Store the item
            _items[item.id] = item;

            // Add it to the list
            _itemsByArea[areaKey].Add(item.id);
         }
      }
      generateShopItems();
   }

   private void generateShopItems () {
      foreach (ShopData shopData in ShopXMLManager.self.shopDataList) {
         _itemsByShopName[shopData.shopName] = new List<int>();
         foreach (ShopItemData rawItemData in ShopXMLManager.self.getShopDataByName(shopData.shopName).shopItems) {
            if (rawItemData.shopItemCategory == ShopToolPanel.ShopCategory.Armor || rawItemData.shopItemCategory == ShopToolPanel.ShopCategory.Weapon) {
               float randomizedChance = UnityEngine.Random.Range(0, 100);
               if (randomizedChance < rawItemData.dropChance) {
                  Item item = new Item {
                     category = (Item.Category) rawItemData.shopItemCategoryIndex,
                     itemTypeId = rawItemData.shopItemTypeIndex,
                     count = 1,
                     id = _itemId++,
                     paletteNames = "",
                     data = ""
                  };

                  Rarity.Type rarity = Rarity.getRandom();
                  int randomizedPrice = rawItemData.shopItemCostMax;
                  string data = "";
                  if ((Item.Category) rawItemData.shopItemCategoryIndex == Item.Category.Weapon) {
                     List<PaletteToolManager.PaletteRepresentation> primary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Weapon, PaletteDef.Weapon.primary.name);
                     List<PaletteToolManager.PaletteRepresentation> secondary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Weapon, PaletteDef.Weapon.secondary.name);
                     List<PaletteToolManager.PaletteRepresentation> power = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Weapon, PaletteDef.Weapon.power.name);
                     string[] palettes = new string[3] { primary.Count > 0 ? primary.ChooseRandom().name : "", secondary.Count > 0 ? secondary.ChooseRandom().name : "", power.Count > 0 ? power.ChooseRandom().name : "" };
                     item.paletteNames = Item.parseItmPalette(palettes);

                     data = string.Format("damage={0}, rarity={1}, price={2}", 0, (int) rarity, randomizedPrice);
                  }
                  if ((Item.Category) rawItemData.shopItemCategoryIndex == Item.Category.Armor) {
                     List<PaletteToolManager.PaletteRepresentation> primary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.primary.name);
                     List<PaletteToolManager.PaletteRepresentation> secondary = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.secondary.name);
                     List<PaletteToolManager.PaletteRepresentation> accent = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Armor, PaletteDef.Armor.accent.name);
                     string[] palettes = new string[3] { primary.Count > 0 ? primary.ChooseRandom().name : "", secondary.Count > 0 ? secondary.ChooseRandom().name : "", accent.Count > 0 ? accent.ChooseRandom().name : "" };
                     item.paletteNames = Item.parseItmPalette(palettes);

                     data = string.Format("armor={0}, rarity={1}, price={2}", 0, (int) rarity, randomizedPrice);
                  }
                  if ((Item.Category) rawItemData.shopItemCategoryIndex == Item.Category.Hats) {
                     data = string.Format("armor={0}, rarity={1}, price={2}", 0, (int) rarity, randomizedPrice);
                  }

                  item.data = data;

                  // Store the item
                  _items[item.id] = item.getCastItem();

                  // Add it to the list
                  _itemsByShopName[shopData.shopName].Add(item.id);
               }
            } else if (rawItemData.shopItemCategory == ShopToolPanel.ShopCategory.CraftingIngredient) {
               Rarity.Type rarity = Rarity.getRandom();
               int randomizedPrice = rawItemData.shopItemCostMax;

               CraftingIngredients item = new CraftingIngredients {
                  category = Item.Category.CraftingIngredients,
                  itemTypeId = rawItemData.shopItemTypeIndex,
                  count = rawItemData.shopItemCountMin,
                  id = _itemId++,
                  paletteNames = "",
                  data = ""
               };

               string data = string.Format("armor={0}, rarity={1}, price={2}", 0, (int) rarity, randomizedPrice);
               item.data = data;

               // Store the item
               _items[item.id] = item;

               // Add it to the list
               _itemsByShopName[shopData.shopName].Add(item.id);
            } else if (rawItemData.shopItemCategory == ShopToolPanel.ShopCategory.Blueprint) {
               Rarity.Type rarity = Rarity.getRandom();
               int randomizedPrice = rawItemData.shopItemCostMax;
               Item item = new Item {
                  category = (Item.Category) rawItemData.shopItemCategoryIndex,
                  itemTypeId = rawItemData.shopItemTypeIndex,
                  count = UnityEngine.Random.Range(rawItemData.shopItemCountMin, rawItemData.shopItemCountMax),
                  id = _itemId++,
                  paletteNames = "",
                  data = ""
               };

               string data = string.Format("rarity={1}, price={2}", 0, (int) rarity, randomizedPrice);
               item.data = data;

               _items[item.id] = item;
               _itemsByShopName[shopData.shopName].Add(item.id);
            }
         }
      }
   }

   protected void randomlyGenerateShips () {
      // If we've already generated something previously, we might not generate anything more this time
      if (_ships.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= .75f) {
         return;
      }

      // Generate ships for each of the areas
      foreach (string areaKey in AreaManager.self.getAreaKeys()) {
         // Clear out the previous list
         _shipsByArea[areaKey] = new List<int>();

         // Make 3 new ships
         for (int i = 1; i <= 3; i++) {
            Ship.Type shipType = Util.randomEnumStartAt<Ship.Type>(1);
            Rarity.Type rarity = Rarity.getRandom();

            ShipInfo ship = Ship.generateNewShip(shipType, rarity);
            ship.shipId = _shipId--;

            // Store the ship
            _ships[ship.shipId] = ship;

            // Add it to the list
            _shipsByArea[areaKey].Add(ship.shipId);
         }
      }
      generateShopShips();
   }
   
   private void generateShopShips () {
      foreach (ShopData shopData in ShopXMLManager.self.shopDataList) {
         _shipsByShopName[shopData.shopName] = new List<int>();
         foreach (ShopItemData shopItem in ShopXMLManager.self.getShopDataByName(shopData.shopName).shopItems) {
            if (shopItem.shopItemCategory == ShopToolPanel.ShopCategory.Ship) {
               float randomizedChance = UnityEngine.Random.Range(0, 100);
               if (randomizedChance < shopItem.dropChance) {
                  int shipXmlId = shopItem.shopItemTypeIndex;
                  ShipData shipData = ShipDataManager.self.getShipData(shipXmlId);
                  Ship.Type shipType = shipData.shipType;
                  Rarity.Type rarity = Rarity.getRandom();

                  ShipInfo ship = Ship.generateNewShip(shipType, rarity);
                  ship.shipId = _shipId--;

                  // Set a custom price
                  int price = shopItem.shopItemCostMax;
                  price = Util.roundToPrettyNumber(price);
                  ship.price = price;

                  // Store the ship
                  _ships[ship.shipId] = ship;

                  // Add it to the list
                  _shipsByShopName[shopData.shopName].Add(ship.shipId);
               }
            }
         }
      }
   }

   private void initializeCropOffers () {
      if (_areCropOffersInitialized) {
         return;
      }

      foreach (ShopData shopData in ShopXMLManager.self.shopDataList) {
         _offersByShopName[shopData.shopName] = new List<CropOffer>();
         foreach (ShopItemData rawItemData in ShopXMLManager.self.getShopDataByName(shopData.shopName).shopItems) {
            if (rawItemData.shopItemCategory == ShopToolPanel.ShopCategory.Crop) {
               // Set the offer characteristics
               Crop.Type cropType = (Crop.Type) rawItemData.shopItemTypeIndex;
               Rarity.Type rarity = Rarity.getRandom();

               CropOffer offer = new CropOffer(_offerId++, "None", cropType, CropOffer.MAX_DEMAND / 2, rawItemData.shopItemCostMax, rarity);

               // Store the offer
               _offers[offer.id] = offer;

               // Add it to the list
               _offersByShopName[shopData.shopName].Add(offer);
            }
         }
      }

      _areCropOffersInitialized = true;
   }

   public List<CropOffer> getOffersByShopName (string shopName) {
      if (_offersByShopName.ContainsKey(shopName)) {
         return _offersByShopName[shopName];
      }

      return new List<CropOffer>();
   }

   public List<Item> getItems (string areaKey) {
      List<Item> list = new List<Item>();

      if (!_itemsByArea.ContainsKey(areaKey)) {
         D.debug("Area key does not exist!: " + areaKey);
      }

      foreach (int itemId in _itemsByArea[areaKey]) {
         Item item = _items[itemId];
         list.Add(item);
      }

      return list;
   }

   public List<Item> getItemsByShopName (string shopName) {
      List<Item> list = new List<Item>();

      if (!_itemsByShopName.ContainsKey(shopName)) {
         D.debug("Shop name does not exist!: " + shopName + " : " + _itemsByShopName.Count);
      } else {
         foreach (int itemId in _itemsByShopName[shopName]) {
            if (_items.ContainsKey(itemId)) {
               Item item = _items[itemId];
               list.Add(item);
            } else {
               D.editorLog("Does not contain: " + itemId, Color.red);
            }
         }
      }

      return list;
   }

   public List<ShipInfo> getShips (string areaKey) {
      List<ShipInfo> list = new List<ShipInfo>();

      foreach (int shipId in _shipsByArea[areaKey]) {
         ShipInfo ship = (ShipInfo) _ships[shipId];

         XmlSerializer ser = new XmlSerializer(ship.shipAbilities.GetType());
         var sb = new StringBuilder();
         using (var writer = XmlWriter.Create(sb)) {
            ser.Serialize(writer, ship.shipAbilities);
         }

         string longString = sb.ToString();
         ship.shipAbilityXML = longString;
         list.Add(ship);
      }

      return list;
   }

   public List<ShipInfo> getShipsByShopName (string shopName) {
      List<ShipInfo> list = new List<ShipInfo>();

      if (_shipsByShopName.ContainsKey(shopName)) {
         foreach (int shipId in _shipsByShopName[shopName]) {
            ShipInfo ship = (ShipInfo) _ships[shipId];

            XmlSerializer ser = new XmlSerializer(ship.shipAbilities.GetType());
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb)) {
               ser.Serialize(writer, ship.shipAbilities);
            }

            string longString = sb.ToString();
            ship.shipAbilityXML = longString;
            list.Add(ship);
         }
      } 

      return list;
   }

   public void onUserSellCrop (string shopName, int offerId, float amount) {
      if (!_offersByShopName.TryGetValue(shopName, out List<CropOffer> shopOffers) || shopOffers.Count == 0) {
         return;
      }

      // The demand lost by the crop will be gained by the others in the same shop
      float demandIncreaseValue = amount / (shopOffers.Count - 1);

      foreach (CropOffer offer in shopOffers) {
         if (offer.id == offerId) {
            decreaseCropOfferDemand(offer, amount);
         } else {
            increaseCropOfferDemand(offer, demandIncreaseValue);
         }
      }
   }

   private void increaseCropOfferDemand (CropOffer offer, float amount) {
      offer.demand += amount;

      if (offer.demand >= CropOffer.MAX_DEMAND) {
         float excess = offer.demand - CropOffer.MAX_DEMAND;

         // When the demand reaches the maximum, we jump to the higher rarity and increase the price accordingly
         if (!offer.isHighestRarity()) {
            offer.rarity += 1;
            offer.recalculatePrice();
            offer.demand = CropOffer.MAX_DEMAND / 2;

            // If the increase was higher than the max demand, continue increasing it in the next tier
            increaseCropOfferDemand(offer, excess);
         } else {
            // If we reached the maximum rarity, simply clamp the demand
            offer.demand = CropOffer.MAX_DEMAND;
         }
      }
   }

   private void decreaseCropOfferDemand (CropOffer offer, float amount) {
      offer.demand -= amount;

      if (offer.demand <= 0) {
         float excess = -offer.demand;

         // When the demand reaches the minimum, we jump to the lower rarity and decrease the price accordingly
         if (!offer.isLowestRarity()) {
            offer.rarity -= 1;
            offer.recalculatePrice();
            offer.demand = CropOffer.MAX_DEMAND / 2;

            // If the decrease was lower than 0, continue decreasing in the next tier
            decreaseCropOfferDemand(offer, excess);
         } else {
            // If we reached the minimum rarity, simply clamp the demand
            offer.demand = 0;
         }
      }
   }

   protected List<WeightedItem<int>> getPossibleWeapons (Biome.Type biomeType) {
      switch (biomeType) {
         default:
            // TODO: Find alternatives to determine these entries
            // TODO: Confirm with sir mike if these "Weighted Items" are still being used
            List<WeightedItem<int>> returnList = new List<WeightedItem<int>>();

            if (EquipmentXMLManager.self.weaponStatList != null && EquipmentXMLManager.self.weaponStatList.Count >= 5) {
               returnList.Add(WeightedItem.Create(.60f, EquipmentXMLManager.self.weaponStatList[1].sqlId));// Steel Sword
               returnList.Add(WeightedItem.Create(.30f, EquipmentXMLManager.self.weaponStatList[3].sqlId)); // Lance Steel
               returnList.Add(WeightedItem.Create(.5f, EquipmentXMLManager.self.weaponStatList[4].sqlId)); // Steel Mace
               returnList.Add(WeightedItem.Create(.4f, EquipmentXMLManager.self.weaponStatList[5].sqlId)); // Golden Star
               returnList.Add(WeightedItem.Create(.1f, EquipmentXMLManager.self.weaponStatList[6].sqlId)); // Rune Blade
            }

            return returnList;
      }
   }

   protected List<WeightedItem<int>> getPossibleArmor (Biome.Type biomeType) {
      switch (biomeType) {
         default:
            return new List<WeightedItem<int>>() {
               WeightedItem.Create(.60f, 2),
               WeightedItem.Create(.40f, 3),
         };
      }
   }

   #region Private Variables

   // A unique ID we can assign to the items we generate
   protected int _itemId = 1;

   // A unique ID we can assign to the ships we generate
   protected int _shipId = -1;

   // A unique ID we can assign to the crop offers we generate
   protected int _offerId = -1;

   // Gets set to true when crop offers have been initialized
   protected bool _areCropOffersInitialized = false;

   // Stores the items we've generated
   protected Dictionary<int, Item> _items = new Dictionary<int, Item>();

   // Stores the ships we've generated
   protected Dictionary<int, ShipInfo> _ships = new Dictionary<int, ShipInfo>();

   // Stores the offers we've generated
   protected Dictionary<int, CropOffer> _offers = new Dictionary<int, CropOffer>();

   // Keeps lists of items based on Area
   protected Dictionary<string, List<int>> _itemsByArea = new Dictionary<string, List<int>>();

   // Keeps lists of ships based on Area
   protected Dictionary<string, List<int>> _shipsByArea = new Dictionary<string, List<int>>();

   // Keeps lists of items based on Shop Name
   protected Dictionary<string, List<int>> _itemsByShopName = new Dictionary<string, List<int>>();

   // Keeps lists of ships based on Shop Name
   protected Dictionary<string, List<int>> _shipsByShopName = new Dictionary<string, List<int>>();

   // Keeps lists of Crop Offers based on Shop Name
   protected Dictionary<string, List<CropOffer>> _offersByShopName = new Dictionary<string, List<CropOffer>>();

   #endregion
}
