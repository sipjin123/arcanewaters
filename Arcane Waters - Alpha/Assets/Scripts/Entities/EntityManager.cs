using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class EntityManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static EntityManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void storeEntity (NetEntity entity) {
      _entities[entity.userId] = entity;
   }

   public void removeEntity (NetEntity entity) {
      if (_entities.ContainsKey(entity.userId) && _entities[entity.userId] == entity) {
         _entities.Remove(entity.userId);
      }
   }

   public NetEntity getEntity (int userId) {
      if (_entities.ContainsKey(userId)) {
         return _entities[userId];
      }

      return null;
   }

   public bool tryGetEntity (int userId, out NetEntity entity) {
      return _entities.TryGetValue(userId, out entity);
   }

   public bool tryGetEntityNotNull (int userId, out NetEntity entity) {
      if (_entities.TryGetValue(userId, out entity)) {
         if (entity == null) {
            return false;
         }
         return true;
      }
      return false;
   }

   public bool tryGetEntityNotNull<T> (int userId, out T entity) where T : NetEntity {
      if (_entities.TryGetValue(userId, out NetEntity en)) {
         if (en == null || !(en is T)) {
            entity = null;
            return false;
         }
         entity = en as T;
         return true;
      }
      entity = null;
      return false;
   }

   public NetEntity getEntityByNetId (uint userNetId) {
      NetEntity searchedEntity = _entities.Values.ToList().Find(_ => _.netId == userNetId);
      return searchedEntity;
   }

   public List<NetEntity> getAllEntities () {
      List<NetEntity> allEntities = new List<NetEntity>(_entities.Values);
      return allEntities;
   }

   public int getEntityCount () {
      return _entities.Count;
   }

   public NetEntity getEntityWithName (string name) {
      foreach (NetEntity entity in _entities.Values) {
         if (name.Equals(entity.entityName, System.StringComparison.InvariantCultureIgnoreCase)) {
            return entity;
         }
      }

      return null;
   }

   public List<NetEntity> getEntitiesWithGroupId (int groupId) {
      return _entities.Values.Where(_ => _.groupId == groupId).ToList();
   }

   public NetEntity getEntityWithAccId (int accId) {
      foreach (NetEntity entity in _entities.Values) {
         if (entity.accountId == accId) {
            return entity;
         }
      }

      return null;
   }

   [Client]
   public void cacheEntityName (int userId, string name) {
      _entityNames[userId] = name;
   }

   [Client]
   public bool tryGetEntityName (int userId, out string name) => _entityNames.TryGetValue(userId, out name);

   [Client]
   public string tryGetEntityName (int userId, string fallBackname) {
      if (tryGetEntityName(userId, out string name)) {
         return name;
      }

      return fallBackname;
   }

   public bool canUserBypassWarpRestrictions (int userId) {
      return _warpBypassingUsers.Contains(userId);
   }

   public void addBypassForUser (int userId) {
      if (!_warpBypassingUsers.Contains(userId)) {
         D.debug("User{" + userId + "} can warp anywhere without restriction for 1 instance");
         _warpBypassingUsers.Add(userId);
      }
   }

   public void removeBypassForUser (int userId) {
      if (_warpBypassingUsers.Contains(userId)) {
         _warpBypassingUsers.Remove(userId);
      }
   }

   #region Private Variables

   // A mapping of userId to Entity object
   protected Dictionary<int, NetEntity> _entities = new Dictionary<int, NetEntity>();

   // A list of user ids that can bypass warp restrictions
   protected List<int> _warpBypassingUsers = new List<int>();

   // Cached name for entities (client-only)
   private Dictionary<int, string> _entityNames = new Dictionary<int, string>();

   #endregion
}
