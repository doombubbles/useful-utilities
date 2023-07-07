using System.Linq;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using UnityEngine;

namespace UsefulUtilities.Utilities;

public class TowerSelecting : UsefulUtility
{
    private static readonly ModSettingHotkey SelectNextTower = new(KeyCode.RightArrow)
    {
        icon = GetTextureGUID<UsefulUtilitiesMod>("SelectNextTower"),
        description = "Changes your tower selection to the next nearest tower from where your cursor first was."
    };

    private static readonly ModSettingHotkey SelectPreviousTower = new(KeyCode.LeftArrow)
    {
        icon = GetTextureGUID<UsefulUtilitiesMod>("SelectPreviousTower"),
        description = "Changes your tower selection to the previouse nearest tower from where your cursor first was."
    };

    private static Vector2 lastMousePos;
    protected override bool CreateCategory => true;

    protected override string Icon => VanillaSprites.SelectedTowerMarker;

    public override void OnUpdate()
    {
        if (InGame.instance == null ||
            TowerSelectionMenu.instance == null ||
            InGame.instance.InputManager.IsInPlacementMode()) return;

        if (SelectNextTower.JustPressed())
        {
            CycleSelection(1);
        }

        if (SelectPreviousTower.JustPressed())
        {
            CycleSelection(-1);
        }
    }

    private static void CycleSelection(int delta)
    {
        var tsm = TowerSelectionMenu.instance;
        var bridge = InGame.Bridge;
        var currentSelection = tsm.selectedTower;

        if (currentSelection == null || lastMousePos == default)
        {
            lastMousePos = InGame.instance.InputManager.cursorPositionWorld;
        }


        var towers = bridge.GetAllTowers()
            .ToArray()
            .OrderBy(tts => Vector2.Distance(lastMousePos, tts.GetSimPosition()))
            .ToList();

        TowerToSimulation? newSelection = null;

        if (currentSelection != null)
        {
            var index = towers.FindIndex(tts => tts.Id == currentSelection.Id);

            if (index >= 0)
            {
                var newIndex = (index + delta + towers.Count) % towers.Count;
                newSelection = towers[newIndex];
            }
        }

        newSelection ??= towers.FirstOrDefault();

        if (newSelection != null)
        {
            InGame.instance.InputManager.SetSelected(newSelection);
        }
    }
}