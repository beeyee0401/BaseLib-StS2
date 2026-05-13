using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace BaseLib.Utils.NodeFactories;

internal class NRestSiteCharacterFactory : NodeFactory<NRestSiteCharacter>
{
    //Root node - Node2D, generally should just have default properties, but can be scaled or moved.
    //ControlRoot - Single point Control node (can use Marker2D conversion). XScale is swapped to -1 to flip. Position is flip point, "seat" position
    //If using Spine, Spine Sprite should be child of root node.
    //Otherwise, make visuals a child of ControlRoot so they are also flipped.
    //Hitbox - Child of ControlRoot, for hovering.
    //SelectionReticle - basegame scene. Recommended to omit from scene and have it auto-generated based on hitbox.
    //ThoughtBubbleLeft/ThoughtBubbleRight - single point Controls, can use Marker2D, positioned left and right of head, around top of head.
    public NRestSiteCharacterFactory() : base([
        new NodeInfo<Control>("ControlRoot", false),
        new NodeInfo<Control>("%Hitbox"),
        new NodeInfo<NSelectionReticle>("%SelectionReticle"),
        new NodeInfo<Control>("%ThoughtBubbleRight"),
        new NodeInfo<Control>("%ThoughtBubbleLeft"),
    ])
    {
    }

    protected override NRestSiteCharacter CreateBareFromResource(object resource)
    {
        switch (resource)
        {
            case Texture2D img:
                BaseLibMain.Logger.Info("Creating NRestSiteCharacter from Texture2D");
                
                var imgSize = img.GetSize();
                var boundsSize = img.GetSize() * 1.05f;
            
                var visualsNode = new NRestSiteCharacter();
                visualsNode.Name = $"GeneratedRestSiteChar_{img.ResourcePath.GetFile()}";

                var controlRoot = new Control();
                controlRoot.Name = "ControlRoot";
                visualsNode.AddChild(controlRoot);
                controlRoot.Position = Vector2.Zero;
                controlRoot.Size = Vector2.Zero;
            
                var hitbox = new Control();
                controlRoot.AddUnique(hitbox, "Hitbox");
                hitbox.Position = new(-boundsSize.X * 0.5f, -boundsSize.Y * 0.6f);
                hitbox.Size = boundsSize;

                var visuals = new Sprite2D();
                visuals.Name = "Visuals";
                controlRoot.AddChild(visuals);
                visuals.Texture = img;
                visuals.Position = new(0, -imgSize.Y * 0.1f);

                return visualsNode;
        }
        
        return base.CreateBareFromResource(resource);
    }

    protected override void ConvertScene(NRestSiteCharacter target, Node? source)
    {
        if (source is Sprite2D sprite)
        {
            var tex = sprite.Texture;
            if (tex != null)
            {
                source.QueueFreeSafely();
                source = CreateBareFromResource(tex);
            }
        }
        base.ConvertScene(target, source);
    }

    protected override void GenerateNode(Node target, INodeInfo required)
    {
        switch (required.Path)
        {
            case "ControlRoot":
            case "%Hitbox":
                BaseLibMain.Logger.Warn($"{required.Path} must be defined in NRestSiteCharacter scene.");
                break;
            case "%ThoughtBubbleRight":
                var hitbox = target.GetNode<Control>("%Hitbox");
                var rightBubble = new Control();
                rightBubble.Size = Vector2.Zero;
                rightBubble.Position = hitbox.Position + (hitbox.Size * new Vector2(0.8f, 0.2f));
                target.AddUnique(rightBubble, "ThoughtBubbleRight");
                break;
            case "%ThoughtBubbleLeft":
                hitbox = target.GetNode<Control>("%Hitbox");
                var leftBubble = new Control();
                leftBubble.Size = Vector2.Zero;
                leftBubble.Position = hitbox.Position + (hitbox.Size * new Vector2(0.2f, 0.2f));
                target.AddUnique(leftBubble, "ThoughtBubbleLeft");
                break;
            case "%SelectionReticle":
                hitbox = target.GetNode<Control>("%Hitbox");
                var reticle = SceneHelper.Instantiate<NSelectionReticle>("ui/selection_reticle");
                CopyControlProperties(reticle, hitbox);
                target.AddUnique(reticle, "SelectionReticle");
                break;
        }
    }

    protected override Node ConvertNodeType(Node node, Type targetType)
    {
        if (targetType == typeof(NSelectionReticle))
        {
            if (node is not Control control)
            {
                return base.ConvertNodeType(node, targetType);
            }
            
            var reticle = SceneHelper.Instantiate<NSelectionReticle>("ui/selection_reticle");
            reticle.Name = control.Name;
            CopyControlProperties(reticle, control);
            control.ReplaceBy(reticle);
            control.QueueFree();
            return reticle;
        }

        if (targetType == typeof(Control) && node is Marker2D marker)
        {
            //Only allowed for the thought bubble markers
            if (marker.Name.Equals("ThoughtBubbleLeft")
                || marker.Name.Equals("ThoughtBubbleRight")
                || marker.Name.Equals("ControlRoot"))
            {
                var control = new Control
                {
                    Name = marker.Name,
                    Size = Vector2.Zero,
                    Position = marker.Position,
                };

                marker.ReplaceBy(control);
                marker.QueueFree();
                return control;
            }
            throw new InvalidOperationException(
                $"Marker2D can only be converted to Control for 'ControlRoot', 'ThoughtBubbleLeft', and 'ThoughtBubbleRight' in NRestSiteCharacter, not for '{marker.Name}'");
        }
        
        return base.ConvertNodeType(node, targetType);
    }
}