using System.Collections.Generic;
using UnityEngine;

public delegate bool CanBeUsedCondition (MonoBehaviour element);

public class Pool<T> where T : MonoBehaviour
{
   // The prefab instantiated by this pool
   private readonly T _prefab;

   // The list of pooled object
   private List<T> _pooledObjects = new List<T>();

   public Pool (T prefab) {
      this._prefab = prefab;
   }

   // The condition for a pooled object to be considered available for recycle
   public CanBeUsedCondition isAvailableCondition = ((x) => {
      return !x.gameObject.activeInHierarchy;
   });

   public T get (bool enableGameObject = true) {
      foreach (T o in _pooledObjects) {
         if (isAvailableCondition(o)) {
            if (enableGameObject) {
               o.gameObject.SetActive(true);
            }

            return o;
         }
      }

      return createNew();
   }

   private T createNew () {
      T newObject = MonoBehaviour.Instantiate(_prefab);
      _pooledObjects.Add(newObject);
      return newObject;
   }

   public IReadOnlyCollection<T> getPooledObjects () {
      return _pooledObjects.AsReadOnly();
   }
}
