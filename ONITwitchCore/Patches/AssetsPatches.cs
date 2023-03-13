using HarmonyLib;
using JetBrains.Annotations;
using ONITwitchCore.Content;

namespace ONITwitchCore.Patches;

internal static class AssetsPatches
{
	[HarmonyPatch(typeof(Assets), "CreatePrefabs")]
	private static class Assets_CreatePrefabs_Patch
	{
		[UsedImplicitly]
		private static void Postfix()
		{
			ComponentsExt.CollectFloorTiles();
		}
	}
}
