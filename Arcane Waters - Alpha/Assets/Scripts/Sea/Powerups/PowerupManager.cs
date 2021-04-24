using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PowerupManager : MonoBehaviour {
   #region Public Variables

   // Powerup data populated in the inspector, to be replaced with fetching from the database eventually
   public List<PowerupData> powerupData;

   // Singleton instance
   public static PowerupManager self;

   #endregion

   private void Awake () {
      self = this;

      loadPowerupData();
   }

   private void loadPowerupData () {
      foreach (PowerupData data in powerupData) {
         Powerup.Type type = (Powerup.Type) data.powerupType;
         _powerupData[type] = data;
      }
   }

   public float getPowerupMultiplier (Powerup.Type type) {
      if (!_localPlayerPowerups.ContainsKey(type)) {
         return 1.0f;
      }

      return getTotalBoostFactor(_localPlayerPowerups[type]);
   }

   public float getPowerupMultiplierAdditive (Powerup.Type type) {
      return getPowerupMultiplier(type) - 1.0f;
   }

   public float getPowerupMultiplier (int userId, Powerup.Type type) {
      // Print a warning if this is called on a non-server
      if (!NetworkServer.active) {
         D.warning("getPowerupMultiplier is being called on a non-server client, it should only be called on the server");
         return 1.0f;
      }

      // If we don't have the user in our dictionary, return 1
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return 1.0f;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // If we don't have a value for this powerup, return 1
      if (!powerups.ContainsKey(type)) {
         return 1.0f;
      }

      return getTotalBoostFactor(powerups[type]);
   }

   public float getPowerupMultiplierAdditive (int userId, Powerup.Type type) {
      return getPowerupMultiplier(userId, type) - 1.0f;
   }

   private float getTotalBoostFactor (List<Powerup> powerups) {
      float boostFactor = 1.0f;

      PowerupData data = getPowerupData(powerups[0].powerupType);

      foreach (Powerup powerup in powerups) {
         boostFactor += data.rarityBoostFactors[(int)powerup.powerupRarity];
      }

      return boostFactor;
   }

   private float getBoostFactor (Powerup.Type type, Rarity.Type rarity) {
      return getPowerupData(type).rarityBoostFactors[(int)rarity] + 1.0f;
   }

   public PowerupData getPowerupData (Powerup.Type type) {
      if (!_powerupData.ContainsKey(type)) {
         D.error("Couldn't find local data for powerup of type: " + type.ToString());
      }

      return _powerupData[type];
   }

   public void addPowerupClient (Powerup newPowerup) {
      if (!NetworkClient.active) {
         D.error("addPowerupClient should only be called on a client");
         return;
      }

      // If we don't have a list for that powerup type yet, create it
      Powerup.Type powerupType = (Powerup.Type) newPowerup.powerupType;
      if (!_localPlayerPowerups.ContainsKey(powerupType)) {
         _localPlayerPowerups.Add(powerupType, new List<Powerup>());
      }

      _localPlayerPowerups[powerupType].Add(newPowerup);
      PowerupPanel.self.addPowerup((Powerup.Type) newPowerup.powerupType, (Rarity.Type) newPowerup.powerupRarity);
   }

   public void addPowerupServer (int userId, Powerup newPowerup) {
      if (!NetworkServer.active) {
         D.error("addPowerupServer should only be called on the server");
         return;
      }

      // If we haven't created an entry for this player yet, create one
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         _serverPlayerPowerups.Add(userId, new PlayerPowerups());
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // If we don't have a list for that powerup type yet, create it
      Powerup.Type powerupType = (Powerup.Type) newPowerup.powerupType;
      if (!powerups.ContainsKey(powerupType)) {
         powerups.Add(powerupType, new List<Powerup>());
      }

      powerups[powerupType].Add(newPowerup);
   }

   public bool powerupActivationRoll (int userId, Powerup.Type type) {
      // Performs a random roll to see if a powerup will activate, based on the points the user has in the powerup
      return (Random.Range(0.0f, 1.0f) <= getPowerupMultiplierAdditive(userId, type));
   }

   public List<CannonballEffector> getEffectors (int userId) {
      List<CannonballEffector> effectors = new List<CannonballEffector>();

      // If this user doesn't have powerups stored, return an empty list
      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return effectors;
      }

      PlayerPowerups powerups = _serverPlayerPowerups[userId];

      // Get an effector for each type of powerup
      foreach (List<Powerup> powerupsOfType in powerups.Values) {
         CannonballEffector newEffector = getEffector(powerupsOfType);
         if (newEffector != null) {
            effectors.Add(newEffector);
         }
      }

      return effectors;
   }

   private CannonballEffector getEffector (List<Powerup> powerups) {
      Powerup.Type type = (Powerup.Type) powerups[0].powerupType;
      float totalBoostFactor = getTotalBoostFactor(powerups);
      float totalBoostFactorAdditive = totalBoostFactor - 1.0f;
      
      switch (type) {
         case Powerup.Type.FireShots:
            return new CannonballEffector(CannonballEffector.Type.Fire, FIRE_BASE_DAMAGE * totalBoostFactor, duration: FIRE_BASE_DURATION);
         case Powerup.Type.ElectricShots:
            return new CannonballEffector(CannonballEffector.Type.Electric, ELECTRIC_BASE_DAMAGE, range: ELECTRIC_BASE_RANGE * totalBoostFactor);
         case Powerup.Type.IceShots:
            return new CannonballEffector(CannonballEffector.Type.Ice, ICE_BASE_STRENGTH * totalBoostFactorAdditive, duration: ICE_BASE_DURATION);
         case Powerup.Type.ExplosiveShots:
            return new CannonballEffector(CannonballEffector.Type.Explosion, EXPLOSIVE_BASE_DAMAGE * totalBoostFactor, range: EXPLOSIVE_BASE_RANGE * totalBoostFactor);
         case Powerup.Type.BouncingShots:
            return new CannonballEffector(CannonballEffector.Type.Bouncing, totalBoostFactorAdditive, range: BOUNCING_BASE_RANGE + totalBoostFactorAdditive);
         default:
            return null;
      }
   }

   public void clearPowerupsForUser (int userId) {
      if (_serverPlayerPowerups.ContainsKey(userId)) {
         _serverPlayerPowerups.Remove(userId);
      }
   }

   public List<Powerup> getPowerupsForUser (int userId) {
      List<Powerup> powerups = new List<Powerup>();

      if (!_serverPlayerPowerups.ContainsKey(userId)) {
         return powerups;
      }

      foreach (List<Powerup> powerupList in _serverPlayerPowerups[userId].Values) {
         powerups.AddRange(powerupList);
      }

      return powerups;
   }

   public void awardRandomPowerupToUser (int userId) {
      Powerup.Type type = Util.randomEnumStartAt<Powerup.Type>(1);
      Rarity.Type rarity = Rarity.getRandom();

      Powerup newPowerup = new Powerup(type, rarity);
      addPowerupServer(userId, newPowerup);
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player) {
         player.rpc.Target_AddPowerup(player.connectionToClient, newPowerup);
      }
   }

   #region Private Variables

   // Information for each existing powerup type, sorted by powerup type
   private Dictionary<Powerup.Type, PowerupData> _powerupData = new Dictionary<Powerup.Type, PowerupData>();

   // The organised powerups of the local player
   private PlayerPowerups _localPlayerPowerups = new PlayerPowerups();

   // A cache of powerups for each player
   private Dictionary<int, PlayerPowerups> _serverPlayerPowerups = new Dictionary<int, PlayerPowerups>();

   private class PlayerPowerups : Dictionary<Powerup.Type, List<Powerup>> { }

   // The base damage for the fire effect
   private const float FIRE_BASE_DAMAGE = 10.0f;

   // The base duration for the fire effect
   private const float FIRE_BASE_DURATION = 5.0f;

   // The base damage for the electric effect
   private const float ELECTRIC_BASE_DAMAGE = 10.0f;

   // The base range for the electric effect
   private const float ELECTRIC_BASE_RANGE = 1.0f;

   // The base strength for the ice effect
   private const float ICE_BASE_STRENGTH = 0.1f;

   // The base duration for the ice effect
   private const float ICE_BASE_DURATION = 2.5f;

   // The base damage for the explosive effect
   private const float EXPLOSIVE_BASE_DAMAGE = 30.0f;

   // The base range for the explosive effect
   private const float EXPLOSIVE_BASE_RANGE = 0.6f;

   // The base range for the bouncing effect
   private const float BOUNCING_BASE_RANGE = 2.0f;

   #endregion
}
