using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class ExtensionsUtil {

   public static void setSelected (this ToggleGroup toggleGroup, int selected, bool notify = false) {
      Toggle[] toggles = toggleGroup.GetComponentsInChildren<Toggle>();

      for (int i = 0; i < toggles.Length; i++) {
         Toggle t = toggles[i];
         toggleGroup.RegisterToggle(t);

         if (notify) {            
            t.isOn = selected == i;
         } else {
            t.SetIsOnWithoutNotify(i == selected);
         }
      }
   }

   public static Transform Search (this Transform target, string name) {
      if (target.name == name) return target;

      for (int i = 0; i < target.childCount; ++i) {
         var result = Search(target.GetChild(i), name);

         if (result != null) return result;
      }

      return null;
   }

   public static void DestroyChildren (this GameObject go) {
      List<GameObject> children = new List<GameObject>();
      foreach (Transform tran in go.transform) {
         children.Add(tran.gameObject);
      }
      children.ForEach(child => GameObject.Destroy(child));
   }

   public static void DestroyAllChildrenExcept (this GameObject go, GameObject dontDestroy) {
      List<GameObject> children = new List<GameObject>();
      foreach (Transform transform in go.transform) {
         if (transform != dontDestroy.transform) {
            children.Add(transform.gameObject);
         }
      }
            
      children.ForEach(child => GameObject.Destroy(child));
   }

   public static void DestroyAllChildrenExcept (this GameObject go, GameObject[] dontDestroy) {
      List<GameObject> children = new List<GameObject>();
      foreach (Transform transform in go.transform) {
         if (!dontDestroy.Any(x => x.transform == transform)) {
            children.Add(transform.gameObject);
         }
      }

      children.ForEach(child => GameObject.Destroy(child));
   }

   public static void DestroyChildrenInPrefabs (this GameObject go) {
      List<GameObject> children = new List<GameObject>();
      foreach (Transform tran in go.transform) {
         children.Add(tran.gameObject);
      }
      children.ForEach(child => GameObject.DestroyImmediate(child, true));
   }

   public static T FirstAncestorOfType<T> (this GameObject gameObject) where T : Component {
      var t = gameObject.transform.parent;
      T component = null;
      while (t != null && (component = t.GetComponent<T>()) == null) {
         t = t.parent;
      }
      return component;
   }

   public static T LastAncestorOfType<T> (this GameObject gameObject) where T : Component {
      var t = gameObject.transform.parent;
      T component = null;
      while (t != null) {
         var c = t.gameObject.GetComponent<T>();
         if (c != null) {
            component = c;
         }
         t = t.parent;
      }
      return component;
   }

   public static T ChooseRandom<T> (this IList<T> list) {
      // Get a random index from the list
      int randomIndex = r.Next(0, list.Count);

      return list[randomIndex];
   }

   public static T ChooseRandom<T> (this IList<T> list, int seed) {
      // Get a random index from the list
      System.Random r = new System.Random(seed+1);
      int randomIndex = r.Next(0, list.Count);

      return list[randomIndex];
   }

   public static TKey RandomKey<TKey, TValue> (this Dictionary<TKey, TValue> dictionary) {
      List<TKey> keys = dictionary.Keys.ToList();
      return keys.ChooseRandom();
   }

   public static void Shuffle<T> (this IList<T> list) {
      int n = list.Count;
      while (n > 1) {
         n--;
         int k = r.Next(n + 1);
         T value = list[k];
         list[k] = list[n];
         list[n] = value;
      }
   }

   public static Image GetImage (this MonoBehaviour target, string ident) {
      foreach (Image image in target.GetComponentsInChildren<Image>()) {
         if (image.name.Contains(ident)) {
            return image;
         }
      }

      return null;
   }

   public static void Invoke (this MonoBehaviour source, Action f, float delay) {
      source.Invoke(f.Method.Name, delay);
   }

   public static void Show (this CanvasGroup canvasGroup, bool blocksRaycasts = true) {
      if (canvasGroup == null) { return; }
      canvasGroup.interactable = true;
      canvasGroup.alpha = 1f;
      canvasGroup.blocksRaycasts = blocksRaycasts;
   }

   public static void Hide (this CanvasGroup canvasGroup) {
      if(canvasGroup == null) { return; }
      canvasGroup.interactable = false;
      canvasGroup.alpha = 0f;
      canvasGroup.blocksRaycasts = false;
   }

   public static bool IsShowing (this CanvasGroup canvasGroup) {
      return (canvasGroup.gameObject.activeSelf && canvasGroup.alpha > 0f);
   }

   public static T ChooseRandom<T> (this HashSet<T> hashSet) {
      return hashSet.ElementAt(r.Next(hashSet.Count));
   }

   public static bool HasComponent<T> (this GameObject obj) {
      return (obj.GetComponent<T>() as Component) != null;
   }

   public static void SetZ (this Transform transform, float newZ) {
      Vector3 pos = transform.position;
      pos.z = newZ;
      transform.position = pos;
   }

   public static void SetLocalZ (this Transform transform, float newZ) {
      Vector3 pos = transform.localPosition;
      pos.z = newZ;
      transform.localPosition = pos;
   }

   public static float squareDistanceTo (this Transform t1, Transform t2) {
      return ((Vector2)t1.position - (Vector2) t2.position).sqrMagnitude;
   }

   public static float squareDistanceTo (this GameObject t1, GameObject t2) {
      return ((Vector2) t1.transform.position - (Vector2) t2.transform.position).sqrMagnitude;
   }

   public static void PixelSnap (this Transform transform) {
      Vector3 pos = transform.localPosition;

      float ppu = 100f * 2f;

      // Round the pixel value
      float nextX = Mathf.Round(ppu * transform.position.x);
      float nextY = Mathf.Round(ppu * transform.position.y);
      pos = new Vector3(
          nextX / ppu,
          nextY / ppu,
          transform.localPosition.z
      );

      transform.localPosition = pos;
   }

   public static T ToEnum<T> (this string str) {
      return (T) System.Enum.Parse(typeof(T), str, true);
   }

   public static bool Includes<T> (this string thisString, string otherString) {
      if (otherString == null) {
         return false;
      }

      return thisString.ToLower().Contains(otherString.ToLower());
   }

   public static string SplitCamelCase (this string str) {
      return Regex.Replace(
          Regex.Replace(
              str,
              @"(\P{Ll})(\P{Ll}\p{Ll})",
              "$1 $2"
          ),
          @"(\p{Ll})(\P{Ll})",
          "$1 $2"
      );
   }

   public static Vector2 Rotate (this Vector2 v, float degrees) {
      float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
      float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

      float tx = v.x;
      float ty = v.y;
      v.x = (cos * tx) - (sin * ty);
      v.y = (sin * tx) + (cos * ty);
      return v;
   }

   /// <summary>
   /// Perform a deep Copy of the object.
   /// </summary>
   /// <typeparam name="T">The type of object being copied.</typeparam>
   /// <param name="source">The object instance to copy.</param>
   /// <returns>The copied object.</returns>
   public static T Clone<T> (this T source) {
      if (!typeof(T).IsSerializable) {
         throw new ArgumentException("The type must be serializable.", "source");
      }

      // Don't serialize a null object, simply return the default for that object
      if (System.Object.ReferenceEquals(source, null)) {
         return default(T);
      }

      IFormatter formatter = new BinaryFormatter();
      Stream stream = new MemoryStream();
      using (stream) {
         formatter.Serialize(stream, source);
         stream.Seek(0, SeekOrigin.Begin);
         return (T) formatter.Deserialize(stream);
      }
   }

   public static Vector3 ToFloatVector (this Vector3Int source) {
      return new Vector3(source.x, source.y, source.z);
   }

   public static Vector3 ToVector3 (this Vector2 source) {
      return new Vector3(source.x, source.y, 0.0f);
   }

   public static float NextFloat (this System.Random r, float lowerBound, float upperBound) {
      double value = r.NextDouble() * (upperBound - lowerBound) + lowerBound;
      return (float) value;
   }

   public static void AddExplosiveForce (this Rigidbody2D rb, float minForce, float maxForce, float radius, Vector2 explosionPosition, ForceMode2D mode = ForceMode2D.Force) {
      // Calculate the raw direction of the explosion
      Vector2 direction = rb.position - explosionPosition;

      // The magnitude of the raw direction vector is the distance between the explosion and the rigidbody
      float distance = direction.magnitude;

      // Normalize the direction method
      direction /= distance;

      // Calculate the force to apply using the distance
      float forceAmount = Mathf.InverseLerp(0, radius, distance);
      float force = Mathf.Lerp(minForce, maxForce, forceAmount);
            
      rb.AddForce(Mathf.Lerp(0, force, (1 - distance)) * direction, mode);
   }

   public static void Show (this GameObject[] gameObjectArray) {
      foreach (GameObject gO in gameObjectArray) {
         if (!gO.activeSelf) {
            gO.SetActive(true);
         }
      }
   }

   public static void Hide (this GameObject[] gameObjectArray) {
      foreach (GameObject gO in gameObjectArray) {
         if (gO.activeSelf) {
            gO.SetActive(false);
         }
      }
   }

   public static void SetActiveIfNeeded (this GameObject gO, bool value) {
      if (value && !gO.activeSelf) {
         gO.SetActive(true);
      } else if (!value && gO.activeSelf) {
         gO.SetActive(false);
      }
   }

   public static T[] RangeSubset<T> (this T[] array, int startIndex, int length) {
      T[] subset = new T[length];
      Array.Copy(array, startIndex, subset, 0, length);
      return subset;
   }

   #region Private Variables

   // An instance of Random for generating random numbers
   private static System.Random r = new System.Random();

   #endregion
}