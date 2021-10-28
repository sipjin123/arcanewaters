using System.Collections.Generic;
using UnityEngine;

public delegate bool CanBeUsedCondition (MonoBehaviour element);

public class Pool<T> where T : MonoBehaviour
{
   #region Public Variables

   #endregion

   public Pool (T prefab) {
      this._prefab = prefab;
   }

   public T pop (bool enableGameObject = true) {
      foreach (T o in _pooledObjects) {
         if (isCheckedOut(o)) {
            continue;
         }

         o.gameObject.SetActive(enableGameObject);
         _checkedOutObjects.Add(o);
         return o;
      }

      T createdObj = createNew();
      _checkedOutObjects.Add(createdObj);
      return createdObj;
   }

   public void push (T o) {
      if (!isCheckedOut(o)) {
         return;
      }

      _checkedOutObjects.Remove(o);
   }

   private bool isCheckedOut (T o) {
      return _checkedOutObjects.Contains(o);
   }

   private T createNew () {
      T newObject = MonoBehaviour.Instantiate(_prefab);
      _pooledObjects.Add(newObject);
      return newObject;
   }

   public IReadOnlyCollection<T> getPooledObjects () {
      return _pooledObjects.AsReadOnly();
   }

   #region Private Variables

   // The prefab instantiated by this pool
   private readonly T _prefab;

   // The list of pooled object
   private List<T> _pooledObjects = new List<T>();

   // The list of checked out objects
   private List<T> _checkedOutObjects = new List<T>();

   #endregion
}
