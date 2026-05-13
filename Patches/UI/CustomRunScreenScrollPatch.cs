using BaseLib.BaseLibScenes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NCustomRunScreen), "InitCharacterButtons")]
internal static class CustomRunScreenScrollPatch
{
    [HarmonyPostfix]
    private static void MakeScrollable(NCustomRunScreen __instance)
    {
        var container = __instance.GetNodeOrNull<Control>("LeftContainer/CharSelectButtons/ButtonContainer");
        if (container == null) return;

        var buttons = container.GetChildren().OfType<NCharacterSelectButton>().ToList();
        if (buttons.Count <= 5) return;

        foreach (var button in buttons)
        {
            //Improves drag scrolling
            button.MouseFilter = Control.MouseFilterEnum.Pass;
        }

        var parent = container.GetParent();
        var index = container.GetIndex();

        parent.RemoveChild(container);

        var scroll = NHorizontalScrollContainer.Create(
            "ButtonScrollContainer",
            container,
            c =>
            {
                c.AnchorLeft = 0.5f;
                c.AnchorTop = 0.5f;
                c.AnchorRight = 0.5f;
                c.AnchorBottom = 0.5f;
                c.OffsetLeft = -330f;
                c.OffsetTop = -177.0f;
                c.OffsetBottom = -10f;
                c.OffsetRight = 330f;
                c.GrowHorizontal = Control.GrowDirection.Both;
                c.GrowVertical = Control.GrowDirection.Both;
                c.ClipContents = true;
            });

        parent.AddChild(scroll);
        parent.MoveChild(scroll, index);
        scroll.AddChild(container);
        scroll.InitFocusScrolling();
        scroll.CallDeferred(Node.MethodName.SetProcessInput, false);

        
        container.AnchorLeft = 0;
        container.AnchorTop = 0;
        container.AnchorRight = 0;
        container.AnchorBottom = 0;
        container.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        container.CallDeferred(GodotObject.MethodName.Set, "position", Vector2.Zero);
        
        var seedInput = __instance.GetNodeOrNull<Control>("%SeedInput");

        for (var i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            btn.FocusNeighborLeft   = i > 0               ? buttons[i - 1].GetPath() : btn.GetPath();
            btn.FocusNeighborRight  = i < buttons.Count-1  ? buttons[i + 1].GetPath() : btn.GetPath();
            btn.FocusNeighborTop    = seedInput != null ? seedInput.GetPath() : btn.GetPath();
            btn.FocusNeighborBottom = btn.GetPath();
        }
    }
}
