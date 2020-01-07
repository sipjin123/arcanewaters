using System;

[Serializable]
public struct SpawnID
{
   // The key of the area that the spawn is placed in
   public string areaKey;

   // The key of a specific spawn
   public string spawnKey;

   public SpawnID (string areaKey, string spawnKey) {
      this.areaKey = areaKey;
      this.spawnKey = spawnKey;
   }

   public override bool Equals (object obj) {
      return obj is SpawnID && Equals((SpawnID) obj);
   }

   public bool Equals(SpawnID other) {
      return
         areaKey.CompareTo(other.areaKey) == 0 &&
         spawnKey.CompareTo(other.spawnKey) == 0;
   }

   public override int GetHashCode () {
      return areaKey.GetHashCode() ^ spawnKey.GetHashCode();
   }
}