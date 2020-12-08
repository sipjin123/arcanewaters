using UnityEngine;
using System;

[Serializable]
public class GuildIconData
{
	#region Public Variables

	// The guild icon layers
	public string iconBorder;
	public string iconBackground;
	public string iconSigil;

	// The guild icon palettes
	public string iconBackPalettes;
	public string iconSigilPalettes;

	#endregion

	public GuildIconData(string iconBackground, string iconBackPalettes, string iconBorder, string iconSigil, string iconSigilPalettes) {
		this.iconBackground = iconBackground;
		this.iconBackPalettes = iconBackPalettes;
		this.iconBorder = iconBorder;
		this.iconSigil = iconSigil;
		this.iconSigilPalettes = iconSigilPalettes;
	}

	public GuildIconData() {

	}

	public static string guildIconDataToString(GuildIconData guildIconData) {
		return JsonUtility.ToJson(guildIconData);
	}

	public static GuildIconData guildIconDataFromString(string guildIconDataString) {
		return JsonUtility.FromJson<GuildIconData>(guildIconDataString);
	}

	#region Private Variables

	#endregion
}