using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PiercingProjectiles
{
	public class MCMSettings : AttributeGlobalSettings<MCMSettings>
	{
		public override string Id => 
			"PiercingProjectiles";
		public override string DisplayName => 
			"Piercing Projectiles";
		public override string FolderName => 
			"PiercingProjectiles";
		public override string FormatType => 
			"json";

		#region GENERAL
		[SettingPropertyBool(
			"Only player projectiles pierce",
			RequireRestart = false,
			HintText = "Allow only player projectiles to pierce [Default: false]",
			Order = 0)]
		[SettingPropertyGroup(
			"General",
			GroupOrder = 0)]
		public bool OnlyPlayerProjectilesPierce { get; set; } = false;

		[SettingPropertyBool(
			"Pierce on killing blow only",
			RequireRestart = false,
			HintText = "Only allow piercing on a killing blow [Default: false]",
			Order = 1)]
		[SettingPropertyGroup(
			"General",
			GroupOrder = 0)]
		public bool PierceOnKillingBlowOnly { get; set; } = false;
		#endregion

		#region ARROWS
		[SettingPropertyBool(
			"Piercing Arrows",
			RequireRestart = false,
			HintText = "Allow arrows to pierce [Default: false]",
			IsToggle = true,
			Order = 0)]
		[SettingPropertyGroup(
			"Piercing Arrows",
			GroupOrder = 1)]
		public bool EnableArrowPierce { get; set; } = false;
		[SettingPropertyFloatingInteger(
			"Damage retained per Hit",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Percentage of damage retained after each enemy pierced [Default: 25%]",
			Order = 1)]
		[SettingPropertyGroup(
			"Piercing Arrows",
			GroupOrder = 1)]
		public float ArrowPierceDamageRetained { get; set; } = 0.25f;
		[SettingPropertyFloatingInteger(
			"Damage ratio required to pierce through Armor",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Inflicted damage percentage of total damage (inflicted + absorbed by armor) [Default: 80%]",
			Order = 2)]
		[SettingPropertyGroup(
			"Piercing Arrows",
			GroupOrder = 1)]
		public float ArrowPierceRatioRequiredThroughArmor { get; set; } = 0.80f;
		#endregion

		#region BOLTS
		[SettingPropertyBool(
			"Piercing Bolts",
			RequireRestart = false,
			HintText = "Allow bolts to pierce [Default: false]",
			IsToggle = true,
			Order = 0)]
		[SettingPropertyGroup(
			"Piercing Bolts",
			GroupOrder = 2)]
		public bool EnableBoltPierce { get; set; } = false;
		[SettingPropertyFloatingInteger(
			"Damage retained per Hit",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Percentage of damage retained after each enemy pierced [Default: 33%]",
			Order = 1)]
		[SettingPropertyGroup(
			"Piercing Bolts",
			GroupOrder = 2)]
		public float BoltPierceDamageRetained { get; set; } = 0.33f;
		[SettingPropertyFloatingInteger(
			"Damage ratio required to pierce through Armor",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Inflicted damage percentage of total damage (inflicted + absorbed by armor) [Default: 75%]",
			Order = 2)]
		[SettingPropertyGroup(
			"Piercing Bolts",
			GroupOrder = 2)]
		public float BoltPierceRatioRequiredThroughArmor { get; set; } = 0.75f;
		#endregion

		#region JAVELINS
		[SettingPropertyBool(
			"Piercing Javelins",
			RequireRestart = false,
			HintText = "Allow javelins to pierce [Default: false]",
			IsToggle = true,
			Order = 0)]
		[SettingPropertyGroup(
			"Piercing Javelins",
			GroupOrder = 3)]
		public bool EnableJavelinPierce { get; set; } = false;
		[SettingPropertyFloatingInteger(
			"Damage retained per Hit",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Percentage of damage retained after each enemy pierced [Default: 20%]",
			Order = 1)]
		[SettingPropertyGroup(
			"Piercing Javelins",
			GroupOrder = 3)]
		public float JavelinPierceDamageRetained { get; set; } = 0.20f;
		[SettingPropertyFloatingInteger(
			"Damage ratio required to pierce through Armor",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Inflicted damage percentage of total damage (inflicted + absorbed by armor) [Default: 67%]",
			Order = 2)]
		[SettingPropertyGroup(
			"Piercing Javelins",
			GroupOrder = 3)]
		public float JavelinPierceRatioRequiredThroughArmor { get; set; } = 0.67f;
		#endregion

		#region CARTRIDGES
		[SettingPropertyBool(
			"Piercing Bullets",
			RequireRestart = false,
			HintText = "Allow bullets to pierce [Compatibility in case a mod uses 'Cartridges'] [Default: false]",
			IsToggle = true,
			Order = 0)]
		[SettingPropertyGroup(
			"Piercing Bullets",
			GroupOrder = 4)]
		public bool EnableCartridgePierce { get; set; } = false;
		[SettingPropertyFloatingInteger(
			"Damage retained per Hit",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Percentage of damage retained after each enemy pierced [Default: 50%]",
			Order = 1)]
		[SettingPropertyGroup(
			"Piercing Bullets",
			GroupOrder = 4)]
		public float CartridgePierceDamageRetained { get; set; } = 0.50f;
		[SettingPropertyFloatingInteger(
			"Damage ratio required to pierce through Armor",
			0f,
			1f,
			"0%",
			RequireRestart = false,
			HintText = "Inflicted damage percentage of total damage (inflicted + absorbed by armor) [Default: 50%]",
			Order = 2)]
		[SettingPropertyGroup(
			"Piercing Bullets",
			GroupOrder = 4)]
		public float CartridgePierceRatioRequiredThroughArmor { get; set; } = 0.50f;
		#endregion
	}
}
