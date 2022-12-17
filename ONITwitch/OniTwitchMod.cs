﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using KMod;
using ONITwitchCore;
using ONITwitchCore.Config;
using ONITwitchCore.Content.Buildings;
using ONITwitchCore.Integration.DecorPackA;
using UnityEngine;

namespace ONITwitch;

[UsedImplicitly]
public class OniTwitchMod : UserMod2
{
	public override void OnLoad(Harmony harmony)
	{
		base.OnLoad(harmony);
		LocString.CreateLocStringKeys(typeof(TIStrings.STRINGS), null);

		// load config
		var unusedConfig = MainConfig.Instance;
		var unusedCommandConfig = UserCommandConfigManager.Instance;

		var mainThreadObject = new GameObject("MainThreadScheduler");
		mainThreadObject.AddOrGet<MainThreadScheduler>();
		Object.DontDestroyOnLoad(mainThreadObject);

		ModUtil.AddBuildingToPlanScreen("Base", SurpriseTileConfig.Id);
	}

	public const string DecorPackOneStaticID = "DecorPackA";

	public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
	{
		base.OnAllModsLoaded(harmony, mods);

		var decorPackEnabled = mods.Any(mod => (mod.staticID == DecorPackOneStaticID) && mod.IsEnabledForActiveDlc());
		if (decorPackEnabled)
		{
			DecorPack1Integration.LoadIntegration(harmony);
		}
	}
}

[HarmonyPatch(typeof(HoverTextConfiguration), "DrawTitle")]
public static class CellNumInTitle
{
	[UsedImplicitly]
	public static void Postfix(HoverTextDrawer drawer, HoverTextConfiguration __instance)
	{
		if (Camera.main != null)
		{
			var cell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.GetMousePos()));
			var pos = Grid.CellToPos(cell);
			drawer.NewLine();
			drawer.DrawText($"({pos.x}, {pos.y})", __instance.ToolTitleTextStyle);
			drawer.NewLine();
			drawer.DrawText($"Cell {cell}", __instance.ToolTitleTextStyle);
		}
	}
}

[HarmonyPatch(typeof(KImGuiUtil), nameof(KImGuiUtil.SetKAssertCB))]
public static class ImGui_Patch
{
	[UsedImplicitly]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> orig)
	{
		return new[] { new CodeInstruction(OpCodes.Ret) };
	}
}
