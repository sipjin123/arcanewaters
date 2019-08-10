using UnityEngine;


public class tester : MonoBehaviour {
   #region Public Variables

   #endregion

   #region Private Variables

   #endregion
   Random.State seedGenerator;
   int seedGeneratorSeed = 1337;

   private void Update () {
      Debug.LogError("SADASDASD");
      if (Input.GetKeyDown(KeyCode.Alpha4)) {
         Random.InitState(seedGeneratorSeed); //This is the seed

         int itemType = Random.Range(0, 20);
         int itemCount = Random.Range(0, 5);
         Debug.LogError(itemType + " - " + itemCount);
      }

      if (Input.GetKeyDown(KeyCode.Alpha2)) {
         seedGeneratorSeed = 1137;
      }
      if (Input.GetKeyDown(KeyCode.Alpha3)) {
         seedGeneratorSeed = 1037;
      }

      if (Input.GetKeyDown(KeyCode.Alpha1)) {
         bool seedGeneratorInitialized = false;
         // remember old seed
         var temp = Random.state;

         // initialize generator state if needed
         if (!seedGeneratorInitialized) {
            Random.InitState(seedGeneratorSeed);
            seedGenerator = Random.state;
            seedGeneratorInitialized = true;
         }

         // set our generator state to the seed generator
         Random.state = seedGenerator;
         // generate our new seed
         var generatedSeed = Random.Range(int.MinValue, int.MaxValue);
         // remember the new generator state
         seedGenerator = Random.state;
         // set the original state back so that normal random generation can continue where it left off
         Random.state = temp;

         generatedSeed = Mathf.Abs(generatedSeed);
         Debug.LogError("SEED GEB : " + generatedSeed);
      }
   }
}
