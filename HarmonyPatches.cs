using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PiercingProjectiles
{
	[HarmonyPatch(typeof(Mission))]
	internal static class Patch_Mission_MissileHitCallback
	{
		[HarmonyTranspiler]
		[HarmonyPatch("MissileHitCallback")]
		internal static IEnumerable<CodeInstruction> MissileHitCallback(IEnumerable<CodeInstruction> instructions)
		{
			var list = new List<CodeInstruction>(instructions);
			for (int i = 0; i < list.Count; i++)
			{
				// 0    ldfld System.Int32 TaleWorlds.MountAndBlade.CombatLogData::ModifiedDamage
				// 1    sub NULL
				// 2    stfld System.Int32 TaleWorlds.MountAndBlade.CombatLogData::InflictedDamage
				// 3 -- ldarg.2 NULL [Label62, Label63]
				//   ++ ldloca.s 25 (System.Boolean) [Label62, Label63]
				//   ++ ldarg.s 10
				//   ++ ldarg.s 11
				//   ++ ldloca.s 26 (TaleWorlds.MountAndBlade.Blow)
				//   ++ ldloca.s 1 (TaleWorlds.MountAndBlade.MissionWeapon)
				//   ++ ldarga.s 9
				//   ++ call static System.Void PiercingProjectiles.Patch::DeterminePierce(...)
				//   ++ ldarg.2 NULL
				// 4    call System.Boolean TaleWorlds.MountAndBlade.AttackCollisionData::get_IsColliderAgent()
				if (list[i].opcode == OpCodes.Ldfld && list[i].operand is FieldInfo fi0 && fi0.Name == "ModifiedDamage"
					&& list[i + 1].opcode == OpCodes.Sub
					&& list[i + 2].opcode == OpCodes.Stfld && list[i + 2].operand is FieldInfo fi1 && fi1.Name == "InflictedDamage"
					&& list[i + 3].opcode == OpCodes.Ldarg_2
					&& list[i + 4].opcode == OpCodes.Call && list[i + 4].operand is MethodBase mb && mb.Name == "get_IsColliderAgent")
				{
					//ldloca.s 25 (System.Boolean) [Label62, Label63]
					list[i + 3].opcode = OpCodes.Ldloca_S;
					list[i + 3].operand = 25;
					//ldarg.s 10
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_S, 10));
					//ldarg.s 11
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_S, 11));
					//ldloca.s 26 (TaleWorlds.MountAndBlade.Blow)
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldloca_S, 26));
					//ldloca.s 1 (TaleWorlds.MountAndBlade.MissionWeapon)
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldloca_S, 1));
					//ldarga.s 9
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarga_S, 9));
					//call static System.Void PiercingProjectiles.Patch::DeterminePierce(...)
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Mission_MissileHitCallback), nameof(Patch_Mission_MissileHitCallback.DeterminePierce))));
					//ldarg.2 NULL
					list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_2));
					break;
				}
			}
			return list;
		}

		// Missile pierce handling
		internal static void DeterminePierce(ref bool pierce, Agent attacker, Agent victim, ref Blow blow, ref MissionWeapon attackerWeapon, ref int numDamagedAgents)
		{
			try
			{
				//PiercingProjectiles.Message($"--- Plonk!" +
				//	$"\n'{attacker?.Name}' '{victim?.Name}' '{attackerWeapon.CurrentUsageItem?.WeaponClass}' #{numDamagedAgents}" +
				//	$"\nBefore: {pierce} '{blow.InflictedDamage} / {blow.AbsorbedByArmor} ({blow.InflictedDamage / (blow.InflictedDamage + blow.AbsorbedByArmor)})'",
				//	false, Colors.White);

				// already is piercing; likely handled otherwise
				if (pierce)
					return;
				// already hit 3 enemies; maximum for 'MissileHitCallback' default code
				if (numDamagedAgents >= 3)
					return;
				// make sure CurrentUsageItem is not null; should not happen, just being careful
				if (attackerWeapon.CurrentUsageItem == null)
					return;

				// get settings
				var settings = PiercingProjectiles.Settings;

				// check if only player is allowed to pierce using missiles
				if (settings.OnlyPlayerProjectilesPierce && attacker.IsAIControlled)
					return;

				// check weapon type and get settings values
				double damageRetained, ratioRequiredCutThroughArmor;
				switch (attackerWeapon.CurrentUsageItem.WeaponClass)
				{
					case WeaponClass.Arrow:
						// check if it is allowed to pierce
						if (!settings.EnableArrowPierce)
							return;
						// get modifiers
						damageRetained = settings.ArrowPierceDamageRetained;
						ratioRequiredCutThroughArmor = settings.ArrowPierceRatioRequiredThroughArmor;
						break;

					case WeaponClass.Bolt:
						// check if it is allowed to pierce
						if (!settings.EnableBoltPierce)
							return;
						// get modifiers
						damageRetained = settings.BoltPierceDamageRetained;
						ratioRequiredCutThroughArmor = settings.BoltPierceRatioRequiredThroughArmor;
						break;

					case WeaponClass.Javelin:
						// check if it is allowed to pierce
						if (!settings.EnableJavelinPierce)
							return;
						// get modifiers
						damageRetained = settings.JavelinPierceDamageRetained;
						ratioRequiredCutThroughArmor = settings.JavelinPierceRatioRequiredThroughArmor;
						break;

					case WeaponClass.Cartridge:
						// check if it is allowed to pierce
						if (!settings.EnableCartridgePierce)
							return;
						// get modifiers
						damageRetained = settings.CartridgePierceDamageRetained;
						ratioRequiredCutThroughArmor = settings.CartridgePierceRatioRequiredThroughArmor;
						break;

					//case WeaponClass.Stone:
					//case WeaponClass.Boulder:
					//case WeaponClass.ThrowingAxe:
					//case WeaponClass.ThrowingKnife:

					//case WeaponClass.Bow:
					//case WeaponClass.Crossbow:
					//case WeaponClass.Pistol:
					//case WeaponClass.Musket:

					default:
						// everything else is not allowed to pierce
						//PiercingProjectiles.Message($"{attackerWeapon.CurrentUsageItem.WeaponClass} go plonk...", false, Colors.White, false);
						return;
				}

				// reduce damage for each target pierced
				if (numDamagedAgents > 0)
					blow.InflictedDamage = MathF.Round(blow.InflictedDamage * MathF.Pow(damageRetained, numDamagedAgents));

				// check if damage is zero
				if (blow.InflictedDamage <= 0)
					return;

				// check if it is a killing blow in case that only those allow piercing
				if (settings.PierceOnKillingBlowOnly && victim.Health > blow.InflictedDamage)
					return;

				// check if inflicted damage is below "cut through armor" threshold
				if ((blow.InflictedDamage / (blow.InflictedDamage + blow.AbsorbedByArmor)) < ratioRequiredCutThroughArmor)
					return;

				// finally set pierce flag
				pierce = true;

				//PiercingProjectiles.Message($"After:  {pierce} '{blow.InflictedDamage} / {blow.AbsorbedByArmor} ({blow.InflictedDamage / (blow.InflictedDamage + blow.AbsorbedByArmor)})'",
				//	false, Colors.White);
			}
			catch (Exception exc)
			{
				PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: {nameof(DeterminePierce)} failed: {exc?.GetType()}");
			}
		}
	}
}
