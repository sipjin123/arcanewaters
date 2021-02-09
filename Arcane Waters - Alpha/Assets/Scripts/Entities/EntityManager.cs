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

   public void removeEntity (int userId) {
      if (_entities.ContainsKey(userId)) {
         _entities.Remove(userId);
      }
   }

   public NetEntity getEntity (int userId) {
      if (_entities.ContainsKey(userId)) {
         return _entities[userId];
      }

      return null;
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

   #endregion
}
