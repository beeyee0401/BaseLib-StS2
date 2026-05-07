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

        container.AnchorLeft = 0;
        container.AnchorTop = 0;
        container.AnchorRight = 0;
        container.AnchorBottom = 0;
        container.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        container.CallDeferred(GodotObject.MethodName.Set, "position", Vector2.Zero);
    }
}
