using HarmonyLib;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PiercingProjectiles
{
	public class PiercingProjectiles : MBSubModuleBase
	{
		public static MCMSettings Settings { get; private set; }

		private bool isInitialized = false;

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			base.OnBeforeInitialModuleScreenSetAsRoot();

			if (isInitialized)
				return;
			try
			{
				Settings = GlobalSettings<MCMSettings>.Instance ?? throw new NullReferenceException("Settings is null");
				isInitialized = true;
			}
			catch (Exception exc)
			{
				Message($"{nameof(PiercingProjectiles)}: initializing Settings failed: {exc.GetType()}: {exc.Message}\n{exc.StackTrace}", false);
			}
		}

		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();

			try
			{
				HarmonyPatches.Initialize();
			}
			catch (Exception exc)
			{
				Message($"{nameof(PiercingProjectiles)}: initializing Harmony failed: {exc.GetType()}: {exc.Message}\n{exc.StackTrace}", false);
			}
		}


		public static void Message(string s, bool stacktrace = true, Color? color = null, bool log = true)
		{
			try
			{
				if (log)
					FileLog.Log(s + (stacktrace ? $"\n{Environment.StackTrace}" : ""));

				InformationManager.DisplayMessage(new InformationMessage(s, color ?? new Color(1f, 0f, 0f)));
			}
			catch
			{ }
		}
	}
}
