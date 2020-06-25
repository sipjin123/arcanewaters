using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour {
   #region Public Variables

   // The minimum amount of time we'll wait between playing the same clip
   public static float MIN_DELAY = .10f;

   // The minimum velocity we must be moving to trigger a footstep sound
   public static float MIN_FOOTSTEP_VELOCITY = .8f;

   // Whether or not sound effects are enabled
   public static bool effectsOn = true;

   // Whether or not the music is enabled
   public static bool musicOn = true;

   // The currently set volume level for the effects
   public static float effectsVolume = 1f;

   // The currently set volume level for the music
   public static float musicVolume = 1f;

   // The previous music we were playing
   public static Type previousMusicType = Type.None;

   // The Audio Mixer groups we use for various things
   public AudioMixerGroup musicParentGroup;
   public AudioMixerGroup musicChildGroup;
   public AudioMixerGroup musicGrandchildGroup;
   public AudioMixerGroup effectsParentGroup;
   public AudioMixerGroup effectsChildGroup;

   // Stores a reference to our instance
   public static SoundManager self;

   // The button that toggles the music
   public Button musicButton;

   // The Audio Source for our background music
   public AudioSource backgroundMusicAudioSource;

   // The type of sound to play
   public enum Type {
      None = 0, Silence = 1, Intro_Music = 2,

      // Sea Music
      Sea_Forest = 100, Sea_Desert = 101, Sea_Pine = 102, Sea_Snow = 103, Sea_Mushroom = 104, Sea_Lava = 105,

      // Town Music
      Town_Forest = 150, Town_Pine = 151, Town_Desert = 152, Town_Mushroom = 153, Town_Snow = 154, Town_Lava = 155,

      // Battle Music
      Battle_Music = 200,

      // Map effects
      Tick = 300, Tock = 301, Click = 302, Clock = 303, Coin_Pickup = 304, Coin_XP_Gain = 305,
      Door_Close = 306, Door_Open = 307, Footstep = 308, Footstep_Muffled = 309, Furnace = 310,
      Menu_Select = 311, Menu_Submit = 312, Open = 313, GUI_Hover = 314, GUI_Press = 315,
      Ship_Hit = 316, Seamonster_Hit = 317, Container_Found = 318, Container_Opened = 319,
      Character_Jump = 330, Sword_Swing = 331, Slash_Physical = 332, Slam_Physical = 333,
      Character_Block = 335, Death_Poof = 336, Clock_New = 337, Ship_Movement = 338,
      Seagulls_x1 = 339, Seagulls_x3 = 340, Seagulls_x6 = 341,

      // Melee enemy effects
      Golem_Death = 400, Enemy_Jump = 401, Flower_Death = 402, Plant_Chomp = 403, Slime_Attack = 404,
      Slime_Jump = 405, Slime_Death = 406, Boulder = 407,

      // Melee enemy death sounds
      Treeman_Death = 500, Muckspirit_Death = 501, Ent_Death = 502, Coralbow_Death = 503,

      // Melee enemy attacks
      Coralbow_Attack = 600, Attack_Blunt = 601, Ent_Attack = 602, Attack_Fire = 603, Haste = 604,
      Heal = 605, Treeman_Attack = 606, Roots = 607, Slash_Fire = 608, Slash_Ice = 609,
      Slash_Lightning = 610,

      // Battle clips
      Battle_Intro = 700, Battle_Outro = 701,

      // Sea enemy sounds
      Serpent_Attack = 800, Serpent_Hurt = 801, Serpent_Death = 802,
      Chomper_Attack = 810, Chomper_Hurt = 811, Chomper_Death = 812,
      Fishman_Attack = 820, Fishman_Hurt = 821, Fishman_Death = 822,
      Worm_Attack = 830, Worm_Hurt = 831, Worm_Death = 832,
      Reef_Giant_Attack = 840, Reef_Giant_Hurt = 841, Reef_Giant_Death = 842,
      Crusty_Small_Attack = 850, Crusty_Small_Hurt = 851, Crusty_Small_Death = 852,
      Crusty_Titan_Attack = 860, Crusty_Titan_Hurt = 861, Crusty_Titan_Death = 862,

      // Misc effects
      Blip_2 = 900, Powerup = 901,

      // Ambience
      Ambience_Ship_Creeks = 1000, Ambience_Forest_Chirps = 1001, Ambience_House = 1002, Ambience_Town = 1003,
      Ambience_Outdoor = 1004, Ambience_Ocean = 1005,

      // Cannons
      Splash_Cannon_1 = 1100, Ship_Hit_1 = 1101, Ship_Cannon_1 = 1102, Ship_Cannon_2 = 1103, Ship_Hit_2 = 1104,
   }

   #endregion

   void Awake () {
      self = this;

      // Load the saved values if there are any
      if (PlayerPrefs.HasKey(SaveKeys.EFFECTS_ON)) {
         effectsOn = PlayerPrefs.GetInt(SaveKeys.EFFECTS_ON) == 1;
      }
      if (PlayerPrefs.HasKey(SaveKeys.MUSIC_ON)) {
         musicOn = PlayerPrefs.GetInt(SaveKeys.MUSIC_ON) == 1;
      }
      if (PlayerPrefs.HasKey(SaveKeys.EFFECTS_VOLUME)) {
         effectsVolume = PlayerPrefs.GetFloat(SaveKeys.EFFECTS_VOLUME);
      }
      if (PlayerPrefs.HasKey(SaveKeys.MUSIC_VOLUME)) {
         musicVolume = PlayerPrefs.GetFloat(SaveKeys.MUSIC_VOLUME);
      }

      // Look up the background music for the Title Screen, if we have any
      setBackgroundMusic(Type.Intro_Music);
   }

   public void Start () {
      // Disable some stuff in Batch Mode
      if (Util.isBatch()) {
         musicOn = false;
         effectsOn = false;
         return;
      }

      // Toggle the icon for the button
      updateMusicButton();
   }

   void Update () {
      // Mute or unmute when the sound is toggled
      self.musicParentGroup.audioMixer.SetFloat("MusicParentVolume", musicOn ? VOLUME_UNCHANGED : VOLUME_OFF);
      self.effectsParentGroup.audioMixer.SetFloat("EffectsParentVolume", effectsOn ? VOLUME_UNCHANGED : VOLUME_OFF);

      // Consider SFX_MIN_DB as the lowest practical audible volume. If effectsVolume is 0, set the volume to VOLUME_OFF to ensure silence
      float sfxVolume = effectsVolume > 0 ? (1f - effectsVolume) * SFX_MIN_DB : VOLUME_OFF;

      // Make the audio mixer volume match the current volume settings
      self.effectsChildGroup.audioMixer.SetFloat("EffectsChildVolume", sfxVolume);

      // Set the music volume on the AudioSource
      backgroundMusicAudioSource.volume = musicVolume;
   }

   void OnDestroy () {
      if (Util.isBatch()) {
         return;
      }

      // Save our sound settings for the next time
      PlayerPrefs.SetInt(SaveKeys.EFFECTS_ON, effectsOn ? 1 : 0);
      PlayerPrefs.SetInt(SaveKeys.MUSIC_ON, musicOn ? 1 : 0);
      PlayerPrefs.SetFloat(SaveKeys.EFFECTS_VOLUME, effectsVolume);
      PlayerPrefs.SetFloat(SaveKeys.MUSIC_VOLUME, musicVolume);
   }

   public static float getVolumeForSound (Type type) {
      switch (type) {
         case Type.Attack_Fire:
         case Type.Slash_Fire:
            return 1f;
         case Type.Door_Open:
         case Type.Door_Close:
            return .15f;
         case Type.Clock:
            return .7f;
         case Type.Tick:
         case Type.Tock:
            return .25f;
         case Type.Furnace:
            return .2f;
         case Type.Open:
            return .8f;
         case Type.Footstep:
            return .40f;
         case Type.GUI_Hover:
         case Type.GUI_Press:
            return .15f;
         case Type.Seamonster_Hit:
         case Type.Ship_Hit:
            return .5f;
         case Type.Container_Found:
            return .1f;
         case Type.Container_Opened:
            return .25f;
         case Type.Chomper_Attack:
         case Type.Chomper_Hurt:
         case Type.Chomper_Death:
         case Type.Serpent_Attack:
         case Type.Serpent_Hurt:
         case Type.Serpent_Death:
         case Type.Fishman_Attack:
         case Type.Fishman_Hurt:
         case Type.Fishman_Death:
            return .7f;
         case Type.Ambience_House:
         case Type.Ambience_Outdoor:
         case Type.Ambience_Town:
            return 3f;
         case Type.Battle_Intro:
         case Type.Battle_Outro:
            return .4f;
         case Type.Ship_Cannon_2:
         case Type.Ship_Hit_2:
            return .25f;
         default:
            return 1f;

      }
   }

   public void musicButtonPressed () {
      musicOn = !musicOn;

      // Toggle the icon for the button
      updateMusicButton();
   }

   protected void updateMusicButton () {
      // Toggle the icon for the button
      musicButton.GetComponent<Image>().sprite = musicOn ?
         ImageManager.getSprite("GUI/sound_icon") : ImageManager.getSprite("GUI/sound_icon_disabled");
   }

   public static void setMusic (bool isOn) {
      musicOn = isOn;
      PlayerPrefs.SetInt(SaveKeys.MUSIC_ON, musicOn ? 1 : 0);

      // If the music isn't already playing, then start playing it
      if (!self.backgroundMusicAudioSource.isPlaying) {
         self.backgroundMusicAudioSource.Play();
      }
   }

   public static void setEffects (bool isOn) {
      effectsOn = isOn;
      PlayerPrefs.SetInt(SaveKeys.EFFECTS_ON, effectsOn ? 1 : 0);
   }

   public static void setEffectsVolume (float volume) {
      effectsVolume = volume;

      // Save the new volume
      PlayerPrefs.SetFloat(SaveKeys.EFFECTS_VOLUME, effectsVolume);
   }

   public static float getLastClipTime (Type type) {
      if (_lastClipTime.ContainsKey(type)) {
         return _lastClipTime[type];
      }

      return float.MinValue;
   }

   public static AudioSource createLoopedAudio (Type type, Transform creator) {
      // Create an AudioSource for the specified type of sound
      AudioSource audioSource = createAudioSource(type, creator.transform.position);

      // Set the parent
      audioSource.transform.SetParent(creator);

      // Set the source to loop and restart it
      applySoundEffectSettings(audioSource, type);
      audioSource.loop = true;
      audioSource.Play();

      // Keep track of the time at which we played the clip
      _lastClipTime[type] = Time.time;

      return audioSource;
   }

   public static AudioSource play2DClip (Type type, float spatialBlend = 1f) {
      // Don't do anything if not enough time has passed since the last hover event
      if (Time.time - getLastClipTime(type) < MIN_DELAY) {
         return null;
      }

      // Play the clip
      AudioSource source = playClipAtPoint(type, Camera.main.transform.position);
      source.spatialBlend = spatialBlend;

      return source;
   }

   public static void playAttachedClip (Type type, Transform parent) {
      AudioSource audioSource = playClipAtPoint(type, parent.transform.position);

      // The source might be null if effects are turned off
      if (audioSource != null) {
         // Attach the audio source so that it moves with the parent
         audioSource.transform.SetParent(parent);

         // Restart the sound
         audioSource.Stop();
         audioSource.Play();
      }
   }

   public static AudioSource playEnvironmentClipAtPoint (Type type, Vector3 pos) {
      AudioSource source = playClipAtPoint(type, pos);
      applySoundEffectSettings(source, type);

      return source;
   }

   public static AudioSource playClipAtPoint (AudioClip clip, Vector3 pos) {
      AudioSource source = createAudioSource(clip, pos);
      source.Play();

      // Cleanup after the clip finishes
      Destroy(source.gameObject, source.clip.length);

      return source;
   }

   public static AudioSource playClipAtPoint (Type type, Vector3 pos) {
      AudioSource source = createAudioSource(type, pos);
      source.Play();

      // Keep track of the time at which we played the clip
      _lastClipTime[type] = Time.time;

      // Cleanup after the clip finishes
      Destroy(source.gameObject, source.clip.length);

      return source;
   }

   protected static AudioSource createAudioSource (Vector3 pos) {
      // Get the Z position of the currently active camera
      float posZ = Global.isInBattle() ? BattleCamera.self.getCamera().transform.position.z : Camera.main.transform.position.z;
      pos = new Vector3(pos.x, pos.y, posZ);

      // Create a Game Object and audio source to play the clip
      GameObject soundObject = new GameObject();
      soundObject.transform.SetParent(self.transform, false);
      soundObject.name = "SFX";
      soundObject.transform.position = pos;
      AudioSource source = soundObject.AddComponent<AudioSource>();
      //applySoundEffectSettings(source, type);

      return source;
   }

   protected static AudioSource createAudioSource (Type type, Vector3 pos) {
      // Get the Z position of the currently active camera
      float posZ = Global.isInBattle() ? BattleCamera.self.getCamera().transform.position.z : Camera.main.transform.position.z;
      pos = new Vector3(pos.x, pos.y, posZ);

      // Create a Game Object and audio source to play the clip
      GameObject soundObject = new GameObject();
      soundObject.transform.SetParent(self.transform, false);
      soundObject.name = "Sound - " + type;
      soundObject.transform.position = pos;
      AudioSource source = soundObject.AddComponent<AudioSource>();
      string path = isAmbience(type) ? "Sound/Ambience/" : "Sound/Effects/";
      source.clip = Resources.Load<AudioClip>(path + type);
      applySoundEffectSettings(source, type);

      return source;
   }

   protected static AudioSource createAudioSource (AudioClip clip, Vector3 pos) {
      // Get the Z position of the currently active camera
      float posZ = Global.isInBattle() ? BattleCamera.self.getCamera().transform.position.z : Camera.main.transform.position.z;
      pos = new Vector3(pos.x, pos.y, posZ);

      // Create a Game Object and audio source to play the clip
      GameObject soundObject = new GameObject();
      soundObject.transform.SetParent(self.transform, false);
      soundObject.name = "Sound - " + clip.name;
      soundObject.transform.position = pos;
      AudioSource source = soundObject.AddComponent<AudioSource>();
      source.clip = clip;
      applySoundEffectSettings(source);

      return source;
   }

   protected static void applySoundEffectSettings (AudioSource source, Type type) {
      if (source == null) {
         return;
      }

      // Set the appropriate Audio Mixer group
      source.outputAudioMixerGroup = self.effectsChildGroup;

      // Apply any custom volume that we specified for this sound effect
      source.volume = getVolumeForSound(type);

      // Make the sound fade off at a good rate based on distance
      source.spatialBlend = isAmbience(type) ? 0f : 1f;
      source.rolloffMode = AudioRolloffMode.Linear;
      source.minDistance = .5f;
      source.maxDistance = 3f;
      source.spread = 90f;
   }

   protected static void applySoundEffectSettings (AudioSource source) {
      if (source == null) {
         return;
      }

      // Set the appropriate Audio Mixer group
      source.outputAudioMixerGroup = self.effectsChildGroup;

      // Make the sound fade off at a good rate based on distance
      source.spatialBlend = 1f;
      source.rolloffMode = AudioRolloffMode.Linear;
      source.minDistance = .5f;
      source.maxDistance = 3f;
      source.spread = 90f;
   }

   protected static bool isMusic (Type type) {
      if (type.ToString().Contains("Sea_") || type.ToString().Contains("Town_") || type.ToString().Contains("Music")) {
         return true;
      }

      return false;
   }

   protected static bool isAmbience (Type type) {
      if (type.ToString().StartsWith("Ambience")) {
         return true;
      }

      return false;
   }

   public static void setBackgroundMusic (string areaKey) {
      Type areaMusic = Area.getBackgroundMusic(areaKey);
      if (areaMusic != Type.None) {
         setBackgroundMusic(areaMusic);
      }
   }

   public static void setBackgroundMusic (Type type) {
      if (Util.isBatch()) {
         return;
      }

      // If we're already playing that music, there's nothing to do
      if (_currentMusicType == type) {
         return;
      }

      // Keep track of the previous music type, in case we need to switch back later
      previousMusicType = _currentMusicType;

      // Keep track of the music currently being played
      _currentMusicType = type;

      // Smoothly transition to the new music using a coroutine
      self.StartCoroutine(self.transitionBackgroundMusic(type));
   }

   public static void create3dSound (string audioClipName, Vector3 position, int countToChooseFrom = 0) {
      if (Util.isBatch()) {
         return;
      }

      AudioSource audioSource = Instantiate(PrefabsManager.self.sound3dPrefab, position, Quaternion.identity);
      audioSource.transform.SetParent(self.transform, true);
      string path = "Sound/Effects/" + audioClipName;
      if (countToChooseFrom > 1) {
         path += Random.Range(1, countToChooseFrom + 1);
      }
      audioSource.clip = Resources.Load<AudioClip>(path);

      // Play the clip
      audioSource.Play();

      // Destroy after the clip finishes
      Destroy(audioSource.gameObject, audioSource.clip.length);
   }

   protected IEnumerator transitionBackgroundMusic (Type type) {
      // Slowly fade the current music out
      musicGrandchildGroup.audioMixer.FindSnapshot(MUTED_MUSIC_GRANDCHILD).TransitionTo(FADE_DURATION);
      yield return new WaitForSeconds(FADE_DURATION);

      // Stop the previous music, if any was playing
      backgroundMusicAudioSource.Stop();

      // Assign the new music
      backgroundMusicAudioSource.clip = Resources.Load<AudioClip>("Sound/" + type);

      // And now we can play it
      backgroundMusicAudioSource.Play();

      // Slowly fade the new music in
      musicGrandchildGroup.audioMixer.FindSnapshot(DEFAULT_SNAPSHOT).TransitionTo(FADE_DURATION);
      yield return new WaitForSeconds(FADE_DURATION);
   }

   #region Private Variables

   // The current type of music being played
   protected static Type _currentMusicType = Type.None;

   // Tracks how much time has passed since we last played a clip of this type
   protected static Dictionary<Type, float> _lastClipTime = new Dictionary<Type, float>();

   // How long we take to fade the music in or out
   protected static float FADE_DURATION = .50f;

   // The minimum volume setting for Audio Mixer Groups
   protected static float VOLUME_OFF = -80f;

   // The dB value at which we can consider SFX to be inaudible
   protected static float SFX_MIN_DB = -25f;

   // The default volume setting for Audio Mixer Groups
   protected static float VOLUME_UNCHANGED = 0f;

   // The name of our default snapshot with all of the default audio settings
   protected static string DEFAULT_SNAPSHOT = "DefaultSnapshot";

   // The name of our audio snapshot with the music grandchild group muted
   protected static string MUTED_MUSIC_GRANDCHILD = "MutedMusicGrandchildSnapshot";

   #endregion
}
