using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class ShopManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static ShopManager self;

   // The last time the crops were regenerated
   public DateTime lastCropRegenTime;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Routinely change out the items
      InvokeRepeating("randomlyGenerateCropOffers", 0f, (float) TimeSpan.FromHours(CropOffer.REGEN_INTERVAL).TotalSeconds);
   }

   public void initializeRandomGeneratedItems () {
      InvokeRepeating("randomlyGenerateItems", 0f, (float) TimeSpan.FromHours(1).TotalSeconds);
   }

   public void initializeRandomGeneratedShips () {
      InvokeRepeating("randomlyGenerateShips", 0f, (float) TimeSpan.FromHours(1).TotalSeconds);
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

   protected void randomlyGenerateItems () {
      // If we've already generated something previously, we might not generate anything more this time
      if (_items.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= .75f) {
         return;
      }

      // Generate items for each of the areas
      foreach (string areaKey in Area.getAllAreaKeys()) {
         Biome.Type biomeType = Area.getBiome(areaKey);

         // Clear out the previous list
         _itemsByArea[areaKey] = new List<int>();

         // Make 3 new items
         for (int i = 0; i < 3; i++) {
            Item item = null;

            if (UnityEngine.Random.Range(0f, 1f) > .5f) {
               Weapon.Type weaponType = getPossibleWeapons(biomeType).ChooseByRandom();
               item = Weapon.generateRandom(_itemId++, weaponType);
            } else {
               Armor.Type armorType = getPossibleArmor(biomeType).ChooseByRandom();
               item = Armor.generateRandom(_itemId++, armorType);
            }

            // Store the item
            _items[item.id] = item;

            // Add it to the list
            _itemsByArea[areaKey].Add(item.id);
         }
      }
   }

   protected void randomlyGenerateShips () {
      // If we've already generated something previously, we might not generate anything more this time
      if (_ships.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= .75f) {
         return;
      }

      // Generate ships for each of the areas
      foreach (string areaKey in Area.getAllAreaKeys()) {
         // Clear out the previous list
         _shipsByArea[areaKey] = new List<int>();

         // Make 3 new ships
         for (int i = 1; i <= 3; i++) {
            Ship.Type shipType = Util.randomEnum<Ship.Type>();
            Rarity.Type rarity = Rarity.getRandom();
            int speed = (int) (Ship.getBaseSpeed(shipType) * Rarity.getIncreasingModifier(rarity));
            speed = Mathf.Clamp(speed, 70, 130);
            int sailors = (int) (Ship.getBaseSailors(shipType) * Rarity.getDecreasingModifier(rarity));
            int suppliesRoom = (int) (Ship.getBaseSuppliesRoom(shipType) * Rarity.getIncreasingModifier(rarity));
            int cargoRoom = (int) (Ship.getBaseCargoRoom(shipType) * Rarity.getIncreasingModifier(rarity));
            int damage = (int) (Ship.getBaseDamage(shipType) * Rarity.getIncreasingModifier(rarity));
            int health = (int) (Ship.getBaseHealth(shipType) * Rarity.getIncreasingModifier(rarity));
            int price = (int) (Ship.getBasePrice(shipType) * Rarity.getIncreasingModifier(rarity));
            int attackRange = (int) (Ship.getBaseAttackRange(shipType) * Rarity.getIncreasingModifier(rarity));

            // Let's use nice numbers
            sailors = Util.roundToPrettyNumber(sailors);
            suppliesRoom = Util.roundToPrettyNumber(suppliesRoom);
            cargoRoom = Util.roundToPrettyNumber(cargoRoom);
            damage = Util.roundToPrettyNumber(damage);
            health = Util.roundToPrettyNumber(health);
            price = Util.roundToPrettyNumber(price);
            attackRange = Util.roundToPrettyNumber(attackRange);

            ShipInfo ship = new ShipInfo(_shipId--, 0, shipType, Ship.SkinType.None, Ship.MastType.Caravel_1, Ship.SailType.Caravel_1, shipType+"",
               ColorType.None, ColorType.None, ColorType.None, ColorType.None, suppliesRoom, suppliesRoom, cargoRoom, health, health, damage, attackRange, speed, sailors, rarity);

            // We note the price separately, since it's only used in this context
            ship.price = price;

            // Store the ship
            _ships[ship.shipId] = ship;


            // Add it to the list
            _shipsByArea[areaKey].Add(ship.shipId);
         }
      }
   }

   protected void randomlyGenerateCropOffers () {
      //// If we've already generated something previously, we might not generate anything more this time
      //if (_offers.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= .75f) {
      //   return;
      //}

      // Saves the current time
      lastCropRegenTime = DateTime.UtcNow;

      // Generate offers for each of the areas
      foreach (string areaKey in Area.getAllAreaKeys()) {
         Biome.Type biomeType = Area.getBiome(areaKey);

         // Clear out the previous list
         _offersByArea[areaKey] = new List<CropOffer>();

         // The types of crops that might show  up
         List<Crop.Type> cropList = Util.getAllEnumValues<Crop.Type>();
         cropList.Remove(Crop.Type.None);
         cropList.Remove(CropManager.STARTING_CROP);
         cropList.Shuffle();

         // Make 3 new offers
         for (int i = 0; i < 3; i++) {
            Rarity.Type rarity = Rarity.getRandom();

            Crop.Type cropType = cropList[i];
            int stockCount = Util.getBellCurveInt(1000, 100, 100, CropOffer.MAX_STOCK);
            stockCount = Util.roundToPrettyNumber(stockCount);
            int pricePerUnit = (int) (CropManager.getBasePrice(cropType) * Rarity.getCropSellPriceModifier(rarity));
            pricePerUnit = Util.roundToPrettyNumber(pricePerUnit);
            CropOffer offer = new CropOffer(_offerId++, areaKey, cropType, stockCount, pricePerUnit, rarity);

            // For the sake of the tutorial, there will always be an offer for the starting crop in the desert town
            if (Area.MERCHANT_SHOP_DESERT.Equals(areaKey) && i == 0) {
               offer.cropType = CropManager.STARTING_CROP;
               offer.rarity = Rarity.Type.Common;
               offer.pricePerUnit = 80;
               offer.amount = int.MaxValue;
            }

            // Store the offer
            _offers[offer.id] = offer;

            // Add it to the list
            _offersByArea[areaKey].Add(offer);
         }
      }

      // Update the Tip Manager now that the offers have changed
      TipManager.self.updateCropTips();
   }

   public List<CropOffer> getOffers (string areaKey) {
      if (_offersByArea.ContainsKey(areaKey)) {
         return _offersByArea[areaKey];
      }

      return new List<CropOffer>();
   }

   public List<CropOffer> getAllOffers () {
      return new List<CropOffer>(_offers.Values);
   }

   public List<Item> getItems (string areaKey) {
      List<Item> list = new List<Item>();

      foreach (int itemId in _itemsByArea[areaKey]) {
         Item item = _items[itemId];
         list.Add(item);
      }

      return list;
   }

   public List<ShipInfo> getShips (string areaKey) {
      List<ShipInfo> list = new List<ShipInfo>();

      foreach (int shipId in _shipsByArea[areaKey]) {
         ShipInfo ship = (ShipInfo) _ships[shipId];
         list.Add(ship);
      }

      return list;
   }

   public void decreaseItemCount (int shopItemId) {
      // Make sure we can find the specified item
      if (!_items.ContainsKey(shopItemId)) {
         D.warning("Could not find item: " + shopItemId);
         return;
      }

      _items[shopItemId].count--;
   }

   public void decreaseOfferCount (int offerId, int amount) {
      _offers[offerId].amount -= amount;

      // Clamp to 0
      if (_offers[offerId].amount < 0) {
         _offers[offerId].amount = 0;
      }
   }

   protected List<WeightedItem<Weapon.Type>> getPossibleWeapons (Biome.Type biomeType) {
      switch (biomeType) {
         default:
            return new List<WeightedItem<Weapon.Type>>() {
               WeightedItem.Create(.60f, Weapon.Type.Sword_2),
               WeightedItem.Create(.30f, Weapon.Type.Sword_3),
               WeightedItem.Create(.5f, Weapon.Type.Gun_2),
               WeightedItem.Create(.4f, Weapon.Type.Gun_3),
               WeightedItem.Create(.1f, Weapon.Type.Sword_1),
         };
      }
   }

   protected List<WeightedItem<Armor.Type>> getPossibleArmor (Biome.Type biomeType) {
      switch (biomeType) {
         default:
            return new List<WeightedItem<Armor.Type>>() {
               WeightedItem.Create(.60f, Armor.Type.Strapped),
               WeightedItem.Create(.40f, Armor.Type.Tunic),
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

   // Keeps lists of Crop Offers based on Area
   protected Dictionary<string, List<CropOffer>> _offersByArea = new Dictionary<string, List<CropOffer>>();

   #endregion
}
