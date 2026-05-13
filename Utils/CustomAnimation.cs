using Godot;

namespace BaseLib.Utils;

/// <summary>
/// Utility class for handling playing animations using Godot's built-in nodes.
/// </summary>
public static class CustomAnimation
{
    private static readonly SpireField<Node, Func<string[], bool?>> _animHandler = new(() => null);

    private static bool? NoAnimation(string[] _)
    {
        return null;
    }

    public static bool HasCustomAnimation(Node visualRoot)
    {
        if (_animHandler[visualRoot] == null)
        {
            _animHandler[visualRoot] = FindNode<AnimationPlayer>(visualRoot)?.UseAnimationPlayer() ??
                                       FindNode<AnimatedSprite2D>(visualRoot)?.UseAnimatedSprite2D() ??
                                       SearchRecursive<AnimationPlayer>(visualRoot)?.UseAnimationPlayer() ??
                                       SearchRecursive<AnimatedSprite2D>(visualRoot)?.UseAnimatedSprite2D() ??
                                       NoAnimation;

        }
        
        return _animHandler[visualRoot] != NoAnimation;
    }
    
    /// <summary>
    /// Returns true if any custom animation source exists.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="tryAnimNames"></param>
    /// <returns></returns>
    public static bool PlayCustomAnimation(Node n, params string[] tryAnimNames)
    {
        if (_animHandler[n] == null)
        {
            BaseLibMain.Logger.Debug("Looking for Godot animation player");
            _animHandler[n] = FindNode<AnimationTree>(n)?.UseAnimationTree() ??
                              SearchRecursive<AnimationTree>(n)?.UseAnimationTree() ??
                              FindNode<AnimationPlayer>(n)?.UseAnimationPlayer() ??
                              FindNode<AnimatedSprite2D>(n)?.UseAnimatedSprite2D() ??
                              SearchRecursive<AnimationPlayer>(n)?.UseAnimationPlayer() ??
                              SearchRecursive<AnimatedSprite2D>(n)?.UseAnimatedSprite2D() ??
                              NoAnimation;
        }
        return _animHandler[n]?.Invoke(tryAnimNames) != null;
    }
    
    private static Func<string[], bool?>? UseAnimationTree(this AnimationTree animationTree)
    {
        var treeRoot = animationTree.TreeRoot as AnimationNodeStateMachine;
        if (treeRoot == null)
        {
            BaseLibMain.Logger.Error("BaseLib only supports AnimationTree using AnimationNodeStateMachine as tree root");
            return null;
        }
        var stateMachine = (AnimationNodeStateMachinePlayback)animationTree.Get("parameters/playback");
        return (animNames) =>
        {
            foreach (var name in animNames)
            {
                if (animationTree.HasAnimation(name))
                {
                    stateMachine.Travel(name);
                    return true;
                }
            }
            BaseLibMain.Logger.Debug($"Animations not found: {animNames.Stringify()}");

            return false;
        };
    }

    private static Func<string[], bool?> UseAnimationPlayer(this AnimationPlayer animPlayer)
    {
        return (animNames) =>
        {
            foreach (var name in animNames)
            {
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
        };
    }
    
    private static Func<string[], bool?> UseAnimatedSprite2D(this AnimatedSprite2D animSprite)
    {
        return (animNames) =>
        {
            foreach (var name in animNames)
            {
                if (animSprite.SpriteFrames.HasAnimation(name))
                {
                    animSprite.Play(name);
                    return true;
                }
            }
            BaseLibMain.Logger.Debug($"Animations not found: {animNames.Stringify()}");
            
            return false;
        };
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