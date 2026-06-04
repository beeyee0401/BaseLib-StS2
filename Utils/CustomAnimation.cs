using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Utils;

/// <summary>
/// Utility class for handling playing animations using Godot's built-in nodes.
/// </summary>
public static class CustomAnimation
{
    private static readonly SpireField<Node, (Node?, Func<string[], bool?>)> _animHandler = new(
        root =>
            FindNode<AnimationTree>(root)?.UseAnimationTree() ?? 
            SearchRecursive<AnimationTree>(root)?.UseAnimationTree() ??
            FindNode<AnimationPlayer>(root)?.UseAnimationPlayer() ??
            FindNode<AnimatedSprite2D>(root)?.UseAnimatedSprite2D() ??
            SearchRecursive<AnimationPlayer>(root)?.UseAnimationPlayer() ??
            SearchRecursive<AnimatedSprite2D>(root)?.UseAnimatedSprite2D() ??
            (null, NoAnimation)!);

    private static bool? NoAnimation(string[] _)
    {
        return null;
    }

    public static bool HasCustomAnimation(Node visualRoot)
    {
        return _animHandler[visualRoot].Item1 != null;
    }
    
    /// <summary>
    /// Returns true if any custom animation source exists.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="tryAnimNames"></param>
    /// <returns></returns>
    public static bool PlayCustomAnimation(Node n, params string[] tryAnimNames)
    {
        var handler = _animHandler[n];
        if (handler.Item1 != null && !handler.Item1.IsValid()) //Should have player but it's not valid
        {
            BaseLibMain.Logger.Debug("Rechecking for Godot animation player");
            _animHandler[n] = FindNode<AnimationTree>(n)?.UseAnimationTree() ??
                              SearchRecursive<AnimationTree>(n)?.UseAnimationTree() ??
                              FindNode<AnimationPlayer>(n)?.UseAnimationPlayer() ??
                              FindNode<AnimatedSprite2D>(n)?.UseAnimatedSprite2D() ??
                              SearchRecursive<AnimationPlayer>(n)?.UseAnimationPlayer() ??
                              SearchRecursive<AnimatedSprite2D>(n)?.UseAnimatedSprite2D() ??
                              (null, NoAnimation)!;
        }
        return _animHandler[n].Item2.Invoke(tryAnimNames) != null;
    }
    
    private static (Node, Func<string[], bool?>)? UseAnimationTree(this AnimationTree animationTree)
    {
        var treeRoot = animationTree.TreeRoot as AnimationNodeStateMachine;
        if (treeRoot == null)
        {
            BaseLibMain.Logger.Error("BaseLib only supports AnimationTree using AnimationNodeStateMachine as tree root");
            return null;
        }
        var stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        return (animationTree, animNames =>
        {
            foreach (var name in animNames)
            {
                BaseLibMain.Logger.Debug($"Checking for animation {name}");
                if (animationTree.HasAnimation(name))
                {
                    stateMachine.Travel(name);
                    return true;
                }
            }
            BaseLibMain.Logger.Debug($"Animations not found: {animNames.Stringify()}");

            return false;
        });
    }

    private static (Node, Func<string[], bool?>) UseAnimationPlayer(this AnimationPlayer animPlayer)
    {
        return (animPlayer, animNames =>
        {
            foreach (var name in animNames)
            {
                BaseLibMain.Logger.Debug($"Checking for animation {name}");
                if (animPlayer.HasAnimation(name))
                {
                    if (animPlayer.CurrentAnimation.Equals(name))
                        animPlayer.Stop();
                
                    animPlayer.Play(name);
                    return true;
                }
            }
            BaseLibMain.Logger.Debug($"Animations not found: {animNames.Stringify()}");

            return false;
        });
    }
    
    private static (Node, Func<string[], bool?>) UseAnimatedSprite2D(this AnimatedSprite2D animSprite)
    {
        return (animSprite, animNames =>
        {
            foreach (var name in animNames)
            {
                BaseLibMain.Logger.Debug($"Checking for animation {name}");
                if (animSprite.SpriteFrames.HasAnimation(name))
                {
                    animSprite.Play(name);
                    return true;
                }
            }
            BaseLibMain.Logger.Debug($"Animations not found: {animNames.Stringify()}");
            
            return false;
        });
    }

    private static T? FindNode<T>(Node root, string? name = null) where T : Node?
    {
        var tNode = root.GetChildren().OfType<T>().FirstOrDefault();
        if (tNode != null)
        {
            BaseLibMain.Logger.Debug($"Found {typeof(T).Name}");
            return tNode;
        }
        
        name ??= nameof(T);
        var n = root.GetNodeOrNull(name)
                ?? root.GetNodeOrNull("Visuals/" + name)
                ?? root.GetNodeOrNull("Body/" + name);
        tNode = n as T;

        if (tNode != null)
        {
            BaseLibMain.Logger.Debug($"Found {typeof(T).Name}");
        }
        return tNode;
    }

    private static T? SearchRecursive<T>(Node parent) where T : Node?
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is T nodeToFind) return nodeToFind;
            var found = SearchRecursive<T>(child);
            if (found != null)
            {
                BaseLibMain.Logger.Debug($"Found {typeof(T).Name} with recursive search");
                return found;
            }
        }
        return null;
    }
}