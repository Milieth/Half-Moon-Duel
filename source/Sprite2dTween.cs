using Godot;
using System;

public partial class Sprite2dTween : Sprite2D
{
    private Line2D _line2D;
    public override void _Ready()
    {
        var line2D = new Line2D();
        AddChild(line2D);
        line2D.Width = 5.0f;
        _line2D = line2D;

        var tween = GetTree().CreateTween().SetLoops();
        tween.TweenSubtween(DrawAnimatedLine(new Vector2(300, 300), new Vector2(600, 600), 0.2f));
        tween.TweenSubtween(DrawAnimatedLine(new Vector2(600, 600), new Vector2(200, 600), 0.2f));
        tween.TweenSubtween(DrawAnimatedLine(new Vector2(200, 600), new Vector2(300, 300), 0.2f));
    }
    
    public Tween DrawAnimatedLine(Vector2 startPos, Vector2 endPos, float duration)
    {
        // 1. Clear previous points and set initial state

        // 2. Create the Tween
        Tween tween = GetTree().CreateTween();
        tween.TweenCallback(Callable.From(() =>
        {
            _line2D.ClearPoints();
            _line2D.AddPoint(startPos);
            _line2D.AddPoint(startPos); // The second point starts at the beginning            
        }));
        // 3. Animate the second point (index 1) to the end position
        // We use "SetPointPosition" method via Tween
        tween.TweenMethod(Callable.From<Vector2>(pos => _line2D.SetPointPosition(1, pos)), 
                          startPos,
                          endPos,
                          duration)
             .SetTrans(Tween.TransitionType.Cubic)
             .SetEase(Tween.EaseType.Out);

        return tween;
    }
}
