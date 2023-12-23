using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace PiercingProjectiles
{
	internal static class HarmonyPatches
	{
		private static Harmony _harmony;
		private static bool _patchApplied = false;
		private static bool _isInitialized = false;

		public static void Initialize()
		{
			if (_isInitialized)
				return;
			_isInitialized = true;

			_harmony = new Harmony("sy.piercingprojectiles");

			_harmony.Patch(AccessTools.Method(typeof(Mission), "MissileHitCallback"),
				transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Mission_MissileHitCallback_Transpiler)));
			if (!_patchApplied)
				PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: failed to apply patch", false);
		}

		internal static IEnumerable<CodeInstruction> Mission_MissileHitCallback_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var added0 = false;
			var added1 = false;
			var list = new List<CodeInstruction>(instructions);

			_patchApplied = false;

			LocalBuilder lbFlag = null, lbBlow = null;
			for (int i = 0; i < list.Count; i++)
			{
				if (!added0)
				{
					// find local "flag" (see "MultiplePenetration") variable
					if (list[i].opcode == OpCodes.Stloc_S && list[i].operand is LocalBuilder lb0 && lb0.LocalType == typeof(bool))
						lbFlag = lb0;
					// find local "blow" variable
					else if (list[i].opcode == OpCodes.Stloc_S && list[i].operand is LocalBuilder lb1 && lb1.LocalType == typeof(Blow))
						lbBlow = lb1;
					// 0    ldfld System.Int32 TaleWorlds.MountAndBlade.CombatLogData::ModifiedDamage
					// 1    sub NULL
					// 2    stfld System.Int32 TaleWorlds.MountAndBlade.CombatLogData::InflictedDamage
					// 3 -- ldarg.2 NULL [Label62, Label63]
					//   ++ ldloca.s [lbFlag] (System.Boolean) [Label62, Label63]
					//   ++ ldarga.s 9
					//   ++ ldarg.s 10
					//   ++ ldarg.s 11
					//   ++ ldloca.s [lbBlow] (TaleWorlds.MountAndBlade.Blow)
					//   ++ ldloca.s 1 (TaleWorlds.MountAndBlade.MissionWeapon)
					//   ++ call static System.Void PiercingProjectiles.Patch::DeterminePierce(...)
					//   ++ ldarg.2 NULL
					// 4    call System.Boolean TaleWorlds.MountAndBlade.AttackCollisionData::get_IsColliderAgent()
					else if (lbFlag != null && lbBlow != null
						&& list[i].opcode == OpCodes.Ldfld && list[i].operand is FieldInfo fi0 && fi0.Name == "ModifiedDamage"
						&& list[i + 1].opcode == OpCodes.Sub
						&& list[i + 2].opcode == OpCodes.Stfld && list[i + 2].operand is FieldInfo fi2 && fi2.Name == "InflictedDamage"
						&& list[i + 3].opcode == OpCodes.Ldarg_2
						&& list[i + 4].opcode == OpCodes.Call && list[i + 4].operand is MethodBase mb && mb.Name == "get_IsColliderAgent")
					{
						//ldloca.s [lbFlag] (System.Boolean) [Label62, Label63]
						list[i + 3].opcode = OpCodes.Ldloca_S;
						list[i + 3].operand = lbFlag;
						//ldarga.s 9
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarga_S, 9));
						//ldarg.s 10
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_S, 10));
						//ldarg.s 11
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_S, 11));
						//ldloca.s [lbBlow] (TaleWorlds.MountAndBlade.Blow)
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldloca_S, lbBlow));
						//ldloca.s 1 (TaleWorlds.MountAndBlade.MissionWeapon)
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldloca_S, 1));
						//call static System.Void PiercingProjectiles.Patch::DeterminePierce(...)
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.DeterminePierce))));
						//ldarg.2 NULL
						list.Insert(i++ + 4, new CodeInstruction(OpCodes.Ldarg_2));

						added0 = true;
					}
				}
				else if (!added1) // -- doesn't work, there's something else preventing hitting more than 4 times with a missile, but what???
				{
					//	// 0 -- ldc.i4.3 NULL
					//	//   ++ call static PiercingProjectiles.MCMSettings MCM.Abstractions.Base.Global.GlobalSettings`1<PiercingProjectiles.MCMSettings>::get_Instance()
					//	//   ++ callvirt System.Int32 PiercingProjectiles.MCMSettings::get_MaxPassThroughs()
					//	// 1    bge.s Label78
					//	// 2    ldc.i4.1 NULL
					//	// 3    stloc.s 6 (TaleWorlds.MountAndBlade.Mission+MissileCollisionReaction)
					//	if (list[i].opcode == OpCodes.Ldc_I4_3
					//		&& list[i + 1].opcode == OpCodes.Bge_S
					//		&& list[i + 2].opcode == OpCodes.Ldc_I4_1
					//		&& list[i + 3].opcode == OpCodes.Stloc_S && list[i + 3].operand is LocalBuilder lb2 && lb2.LocalType == typeof(MissileCollisionReaction))
					//	{
					//		list.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(MCMSettings), nameof(MCMSettings.Instance))));
					//		list[i + 1] = new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(MCMSettings), nameof(MCMSettings.MaxPassThroughs)));

						added1 = true;
						break;
					//}
				}
			}

			if (added0 && added1)
				_patchApplied = true;

			return list;
		}


		// Missile pierce handling
		internal static void DeterminePierce(ref bool pierce, ref int numDamagedAgents, Agent attacker, Agent victim, ref Blow blow, ref MissionWeapon attackerWeapon)
		{
			try
			{
				if (PiercingProjectiles.Settings.DebugOutput)
					PiercingProjectiles.Message(
						$"-- Type: '{attackerWeapon.CurrentUsageItem?.WeaponClass}' Hits: {numDamagedAgents} Attk: '{attacker?.Name}' Vict: '{victim?.Name}'" +
						$"\nBefore: Pierce: {(pierce ? 1 : 0)} Dmg: '{blow.InflictedDamage} / {blow.AbsorbedByArmor} ({blow.InflictedDamage / (blow.InflictedDamage + blow.AbsorbedByArmor) * 100.0:N1}%)'",
						false, Colors.White);

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

				if (PiercingProjectiles.Settings.DebugOutput)
					PiercingProjectiles.Message(
						$"After:  Pierce: {(pierce ? 1 : 0)} Dmg: '{blow.InflictedDamage} / {blow.AbsorbedByArmor} ({blow.InflictedDamage / (blow.InflictedDamage + blow.AbsorbedByArmor) * 100.0:N1}%)'",
						false, Colors.White);
			}
			catch (Exception exc)
			{
				PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: {nameof(DeterminePierce)} failed: {exc?.GetType()}");
			}
		}
	}

	// DEBUG
#if false
	[HarmonyPatch(typeof(Test))]
	internal static class Patch_Test_MissileHitCallback
	{
		[HarmonyTranspiler]
		[HarmonyPatch("MissileHitCallback")]
		internal static IEnumerable<CodeInstruction> MissileHitCallback(IEnumerable<CodeInstruction> instructions)
		{
			FileLog.Log("mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm --- TEST");
			foreach (var instr in instructions)
				FileLog.Log($"{instr}");

			return instructions;
		}
	}

	internal class Test
	{
		private Dictionary<int, Missile> _missiles;
		public bool ForceNoFriendlyFire;
		private MissionMode _missionMode;
		public MissionMode Mode => _missionMode;
		public MissionCombatType CombatType { get; set; }
		private List<MissionBehavior> _missionBehaviorList;
		public List<MissionBehavior> MissionBehaviors => _missionBehaviorList;

		internal bool MissileHitCallback(out int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, Agent attacker, Agent victim, GameEntity hitEntity)
		{
			Missile missile = _missiles[collisionData.AffectorWeaponSlotOrMissileIndex];
			MissionWeapon attackerWeapon = missile.Weapon;
			WeaponFlags missileWeaponFlags = attackerWeapon.CurrentUsageItem.WeaponFlags;
			float num = 1f;
			WeaponComponentData shieldOnBack = null;
			MissionGameModels.Current.AgentApplyDamageModel.DecideMissileWeaponFlags(attacker, missile.Weapon, ref missileWeaponFlags);
			extraHitParticleIndex = -1;
			MissileCollisionReaction missileCollisionReaction = MissileCollisionReaction.Invalid;
			bool flag = !GameNetwork.IsSessionActive;
			bool missileHasPhysics = collisionData.MissileHasPhysics;
			PhysicsMaterial fromIndex = PhysicsMaterial.GetFromIndex(collisionData.PhysicsMaterialIndex);
			PhysicsMaterialFlags num2 = (fromIndex.IsValid ? fromIndex.GetFlags() : PhysicsMaterialFlags.None);
			bool flag2 = (missileWeaponFlags & WeaponFlags.AmmoSticksWhenShot) != 0;
			bool flag3 = (num2 & PhysicsMaterialFlags.DontStickMissiles) == 0;
			bool flag4 = (num2 & PhysicsMaterialFlags.AttacksCanPassThrough) != 0;
			MissionObject missionObject = null;
			if (victim == null && hitEntity != null)
			{
				GameEntity gameEntity = hitEntity;
				do
				{
					missionObject = gameEntity.GetFirstScriptOfType<MissionObject>();
					gameEntity = gameEntity.Parent;
				}
				while (missionObject == null && gameEntity != null);
				hitEntity = missionObject?.GameEntity;
			}
			MissileCollisionReaction missileCollisionReaction2 = (flag4 ? MissileCollisionReaction.PassThrough : (missileWeaponFlags.HasAnyFlag(WeaponFlags.Burning) ? MissileCollisionReaction.BecomeInvisible : ((!flag3 || !flag2) ? MissileCollisionReaction.BounceBack : MissileCollisionReaction.Stick)));
			bool flag5 = false;
			bool flag6 = victim != null && victim.CurrentMortalityState == Agent.MortalityState.Invulnerable;
			CombatLogData combatLog;
			if (collisionData.MissileGoneUnderWater || collisionData.MissileGoneOutOfBorder || flag6)
			{
				missileCollisionReaction = MissileCollisionReaction.BecomeInvisible;
			}
			else if (victim == null)
			{
				if (hitEntity != null)
				{
					GetAttackCollisionResults(attacker, victim, hitEntity, num, in attackerWeapon, crushedThrough: false, cancelDamage: false, crushedThroughWithoutAgentCollision: false, ref collisionData, out shieldOnBack, out combatLog);
					Blow b = CreateMissileBlow(attacker, in collisionData, in attackerWeapon, missilePosition, missileStartingPosition);
					RegisterBlow(attacker, null, hitEntity, b, ref collisionData, in attackerWeapon, ref combatLog);
				}
				missileCollisionReaction = missileCollisionReaction2;
			}
			else if (collisionData.AttackBlockedWithShield)
			{
				GetAttackCollisionResults(attacker, victim, hitEntity, num, in attackerWeapon, crushedThrough: false, cancelDamage: false, crushedThroughWithoutAgentCollision: false, ref collisionData, out shieldOnBack, out combatLog);
				if (!collisionData.IsShieldBroken)
				{
					MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeStone, collisionData.CollisionGlobalPosition, soundCanBePredicted: false, isReliable: false, -1, -1);
				}
				bool flag7 = false;
				if (missileWeaponFlags.HasAnyFlag(WeaponFlags.CanPenetrateShield))
				{
					if (!collisionData.IsShieldBroken)
					{
						EquipmentIndex wieldedItemIndex = victim.GetWieldedItemIndex(Agent.HandIndex.OffHand);
						if ((float)collisionData.InflictedDamage > ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationOffset) + ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationFactor) * (float)victim.Equipment[wieldedItemIndex].GetGetModifiedArmorForCurrentUsage())
						{
							flag7 = true;
						}
					}
					else
					{
						flag7 = true;
					}
				}
				if (flag7)
				{
					num *= 0.4f + MBRandom.RandomFloat * 0.2f;
					missileCollisionReaction = MissileCollisionReaction.PassThrough;
				}
				else
				{
					missileCollisionReaction = (collisionData.IsShieldBroken ? MissileCollisionReaction.BecomeInvisible : missileCollisionReaction2);
				}
			}
			else if (collisionData.MissileBlockedWithWeapon)
			{
				GetAttackCollisionResults(attacker, victim, hitEntity, num, in attackerWeapon, crushedThrough: false, cancelDamage: false, crushedThroughWithoutAgentCollision: false, ref collisionData, out shieldOnBack, out combatLog);
				missileCollisionReaction = MissileCollisionReaction.BounceBack;
			}
			else
			{
				if (attacker != null && attacker.IsFriendOf(victim))
				{
					if (ForceNoFriendlyFire)
					{
						flag5 = true;
					}
					else if (!missileHasPhysics)
					{
						if (flag)
						{
							if (attacker.Controller == Agent.ControllerType.AI)
							{
								flag5 = true;
							}
						}
						else if ((MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue() <= 0 && MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue() <= 0) || Mode == MissionMode.Duel)
						{
							flag5 = true;
						}
					}
				}
				else if (victim.IsHuman && !attacker.IsEnemyOf(victim))
				{
					flag5 = true;
				}
				else if (flag && attacker != null && attacker.Controller == Agent.ControllerType.AI && victim.RiderAgent != null && attacker.IsFriendOf(victim.RiderAgent))
				{
					flag5 = true;
				}
				if (flag5)
				{
					if (flag && attacker == Agent.Main && attacker.IsFriendOf(victim))
					{
						InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_you_hit_a_friendly_troop").ToString(), Color.ConvertStringToColor("#D65252FF")));
					}
					missileCollisionReaction = MissileCollisionReaction.BecomeInvisible;
				}
				else
				{
					bool flag8 = (missileWeaponFlags & WeaponFlags.MultiplePenetration) != 0;
					GetAttackCollisionResults(attacker, victim, null, num, in attackerWeapon, crushedThrough: false, cancelDamage: false, crushedThroughWithoutAgentCollision: false, ref collisionData, out shieldOnBack, out combatLog);
					Blow blow = CreateMissileBlow(attacker, in collisionData, in attackerWeapon, missilePosition, missileStartingPosition);
					if (collisionData.IsColliderAgent && flag8 && numDamagedAgents > 0)
					{
						blow.InflictedDamage /= numDamagedAgents;
						blow.SelfInflictedDamage /= numDamagedAgents;
						combatLog.InflictedDamage = blow.InflictedDamage - combatLog.ModifiedDamage;
					}

					//HarmonyPatches.DeterminePierce(ref flag8, ref numDamagedAgents, attacker, victim, ref blow, ref attackerWeapon); // <-- ADDED

					if (collisionData.IsColliderAgent)
					{
						if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentShrugOffBlow(victim, collisionData, in blow))
						{
							blow.BlowFlag |= BlowFlags.ShrugOff;
						}
						else if (victim.IsHuman)
						{
							Agent mountAgent = victim.MountAgent;
							if (mountAgent != null)
							{
								if (mountAgent.RiderAgent == victim && MissionGameModels.Current.AgentApplyDamageModel.DecideAgentDismountedByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
								{
									blow.BlowFlag |= BlowFlags.CanDismount;
								}
							}
							else
							{
								if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedBackByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
								{
									blow.BlowFlag |= BlowFlags.KnockBack;
								}
								if (MissionGameModels.Current.AgentApplyDamageModel.DecideAgentKnockedDownByBlow(attacker, victim, in collisionData, attackerWeapon.CurrentUsageItem, in blow))
								{
									blow.BlowFlag |= BlowFlags.KnockDown;
								}
							}
						}
					}
					if (victim.State == AgentState.Active)
					{
						RegisterBlow(attacker, victim, null, blow, ref collisionData, in attackerWeapon, ref combatLog);
					}
					extraHitParticleIndex = MissionGameModels.Current.DamageParticleModel.GetMissileAttackParticle(attacker, victim, in blow, in collisionData);
					if (flag8 && numDamagedAgents < MCMSettings.Instance.MaxPassThroughs) // <-- CHANGED from 3
					{
						missileCollisionReaction = MissileCollisionReaction.PassThrough;
					}
					else
					{
						missileCollisionReaction = missileCollisionReaction2;
						if (missileCollisionReaction2 == MissileCollisionReaction.Stick && !collisionData.CollidedWithShieldOnBack)
						{
							bool flag9 = CombatType == MissionCombatType.Combat;
							if (flag9)
							{
								bool flag10 = victim.IsHuman && collisionData.VictimHitBodyPart == BoneBodyPartType.Head;
								flag9 = victim.State != AgentState.Active || !flag10;
							}
							if (flag9)
							{
								float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.MissileMinimumDamageToStick);
								float num3 = 2f * managedParameter;
								if ((float)blow.InflictedDamage < managedParameter && blow.AbsorbedByArmor > num3 && !GameNetwork.IsClientOrReplay)
								{
									missileCollisionReaction = MissileCollisionReaction.BounceBack;
								}
							}
							else
							{
								missileCollisionReaction = MissileCollisionReaction.BecomeInvisible;
							}
						}
					}
				}
			}
			if (collisionData.CollidedWithShieldOnBack && shieldOnBack != null && victim != null && victim.IsMainAgent)
			{
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_hit_shield_on_back").ToString(), Color.ConvertStringToColor("#FFFFFFFF")));
			}
			MatrixFrame attachLocalFrame;
			bool isAttachedFrameLocal;
			if (!collisionData.MissileHasPhysics && missileCollisionReaction == MissileCollisionReaction.Stick)
			{
				attachLocalFrame = CalculateAttachedLocalFrame(in attachGlobalFrame, collisionData, missile.Weapon.CurrentUsageItem, victim, hitEntity, movementVelocity, missileAngularVelocity, affectedShieldGlobalFrame, shouldMissilePenetrate: true);
				isAttachedFrameLocal = true;
			}
			else
			{
				attachLocalFrame = attachGlobalFrame;
				attachLocalFrame.origin.z = Math.Max(attachLocalFrame.origin.z, -100f);
				missionObject = null;
				isAttachedFrameLocal = false;
			}
			Vec3 velocity = Vec3.Zero;
			Vec3 angularVelocity = Vec3.Zero;
			if (missileCollisionReaction == MissileCollisionReaction.BounceBack)
			{
				WeaponFlags weaponFlags = missileWeaponFlags & WeaponFlags.AmmoBreakOnBounceBackMask;
				if ((weaponFlags == WeaponFlags.AmmoCanBreakOnBounceBack && collisionData.MissileVelocity.Length > ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BreakableProjectileMinimumBreakSpeed)) || weaponFlags == WeaponFlags.AmmoBreaksOnBounceBack)
				{
					missileCollisionReaction = MissileCollisionReaction.BecomeInvisible;
					extraHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_broken_arrow");
				}
				else
				{
					missile.CalculateBounceBackVelocity(missileAngularVelocity, collisionData, out velocity, out angularVelocity);
				}
			}
			HandleMissileCollisionReaction(collisionData.AffectorWeaponSlotOrMissileIndex, missileCollisionReaction, attachLocalFrame, isAttachedFrameLocal, attacker, victim, collisionData.AttackBlockedWithShield, collisionData.CollisionBoneIndex, missionObject, velocity, angularVelocity, -1);
			foreach (MissionBehavior missionBehavior in MissionBehaviors)
			{
				missionBehavior.OnMissileHit(attacker, victim, flag5, collisionData);
			}
			return missileCollisionReaction != MissileCollisionReaction.PassThrough;
		}

		private CombatLogData GetAttackCollisionResults(Agent attackerAgent, Agent victimAgent, GameEntity hitObject, float momentumRemaining, in MissionWeapon attackerWeapon, bool crushedThrough, bool cancelDamage, bool crushedThroughWithoutAgentCollision, ref AttackCollisionData attackCollisionData, out WeaponComponentData shieldOnBack, out CombatLogData combatLog)
		{
			throw new NotImplementedException();
		}
		private Blow CreateMissileBlow(Agent attacker, in AttackCollisionData collisionData, in MissionWeapon attackerWeapon, Vec3 missilePosition, Vec3 missileStartingPosition)
		{
			throw new NotImplementedException();
		}
		private void RegisterBlow(Agent attacker, object value, GameEntity hitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon, ref CombatLogData combatLog)
		{
			throw new NotImplementedException();
		}
		private void MakeSound(int soundCodePhysicsArrowlikeStone, Vec3 collisionGlobalPosition, bool soundCanBePredicted, bool isReliable, int v1, int v2)
		{
			throw new NotImplementedException();
		}
		private void HandleMissileCollisionReaction(int affectorWeaponSlotOrMissileIndex, MissileCollisionReaction missileCollisionReaction, MatrixFrame attachLocalFrame, bool isAttachedFrameLocal, Agent attacker, Agent victim, bool attackBlockedWithShield, sbyte collisionBoneIndex, MissionObject missionObject, Vec3 velocity, Vec3 angularVelocity, int v)
		{
			throw new NotImplementedException();
		}
		private MatrixFrame CalculateAttachedLocalFrame(in MatrixFrame attachGlobalFrame, AttackCollisionData collisionData, WeaponComponentData currentUsageItem, Agent victim, GameEntity hitEntity, Vec3 movementVelocity, Vec3 missileAngularVelocity, MatrixFrame affectedShieldGlobalFrame, bool shouldMissilePenetrate)
		{
			throw new NotImplementedException();
		}
	}

	

		//public static void OnGameStart()
		//{
		//	try
		//	{
		//		if (PiercingProjectiles.Settings.DebugOutput)
		//			PiercingProjectiles.Message($"{nameof(OnGameStart)} ({nameof(_isPatched)}: {_isPatched})", false, Colors.Green);

		//		if (_isPatched)
		//			return;
		//		_isPatched = true;

		//		_harmony.Patch(AccessTools.Method(typeof(Mission), "MissileHitCallback"),
		//			transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.Mission_MissileHitCallback_Transpiler)));
		//		if (!_patchApplied)
		//			PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: failed to apply patch", false);
		//	}
		//	catch (Exception exc)
		//	{
		//		PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: {nameof(HarmonyPatches)}.{nameof(OnGameStart)} failed: {exc.GetType()}: {exc.Message}\n{exc.StackTrace}", false);
		//	}
		//}
		//public static void OnGameEnd()
		//{
		//	try
		//	{
		//		if (PiercingProjectiles.Settings.DebugOutput)
		//			PiercingProjectiles.Message($"{nameof(OnGameEnd)} ({nameof(_isPatched)}: {_isPatched})", false, Colors.Red);

		//		if (!_isPatched)
		//			return;
		//		_isPatched = false;

		//		_harmony.Unpatch(AccessTools.Method(typeof(Mission), "MissileHitCallback"),
		//			AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Mission_MissileHitCallback_Transpiler)));
		//	}
		//	catch (Exception exc)
		//	{
		//		PiercingProjectiles.Message($"{nameof(PiercingProjectiles)}: {nameof(HarmonyPatches)}.{nameof(OnGameEnd)} failed: {exc.GetType()}: {exc.Message}\n{exc.StackTrace}", false);
		//	}
		//}
#endif
}
