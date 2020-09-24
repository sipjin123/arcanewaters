using System.Collections;
using UnityEngine;

/// <summary>
/// Like WaitForSeconds, but allows passing a double as parameter
/// </summary>
public class WaitForSecondsDouble : IEnumerator
{
   public object Current => null;

   // The time to wait
   private readonly double _waitTime;

   // The time elapsed since the instruction was created
   private double _elapsedTime;

   public WaitForSecondsDouble (double seconds) {
      _waitTime = seconds;
   }

   public bool MoveNext () {
      _elapsedTime += Time.deltaTime;
      return _elapsedTime < _waitTime;
   }

   public void Reset () {
      _elapsedTime = 0;
   }
}