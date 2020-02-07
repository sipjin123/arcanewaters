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

   #region Private Variables

   // A mapping of userId to Entity object
   protected Dictionary<int, NetEntity> _entities = new Dictionary<int, NetEntity>();

   #endregion
}
