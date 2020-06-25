using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Xml.Serialization;

[CreateAssetMenu(fileName = "New AudioGroupData", menuName = "Audio Group Data", order = 51)]
public class AudioGroupData : ScriptableObject {
    #region Public Variables

    // Keeping track of the sound types for list filtering later on
    public string soundType;

    // Configurable Range to allow pitch bending possibility
    public Vector2 pitchRange = new Vector2(1.0f, 1.0f);

    // Configurable boolean to toggle use of pitch range
    public bool randomizePitch = false;

    // Sound clips to be played
    public List<AudioClip> sounds = new List<AudioClip>();

    // Function to return a random pitch using the provided range
    public float getRandomPitch () {
        return Random.Range(pitchRange.x, pitchRange.y);
    }

    // Returns randomized pitch or 1 (default pitch)
    public float getPitch () {
        return randomizePitch? getRandomPitch() : 1;
    }

    #endregion
    
    #region Private Variables
  
    #endregion
}