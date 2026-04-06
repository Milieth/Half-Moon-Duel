using Godot;
using System;

public partial class Sprite2dTween : Sprite2D
{
    // private Tween _tween;
    public override void _Ready()
    {
        Position = new Vector2(640, 360);

        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(2.0f, 2.0f), 1.0f).SetDelay(2.0);
        tween.TweenProperty(this, "self_modulate", new Color(1.0f, 1.0f, 1.0f) * 2.0f, 1.0f);
        tween.TweenSubtween(AddStarAnim());
        tween.Parallel().TweenSubtween(AddStarAnim()).SetDelay(0.4);
        tween.Parallel().TweenSubtween(AddStarAnim()).SetDelay(0.6);
        tween.TweenProperty(this, "scale", new Vector2(1.0f, 1.0f), 1.0);
        // _tween = tween;
    }

    private Tween AddStarAnim()
    {
        var star = new Sprite2D();
        AddChild(star);
        star.Texture = GD.Load<Texture2D>("res://aset/white star.png");
        star.Hide();

        var sub_tween = GetTree().CreateTween();
        sub_tween.TweenCallback(Callable.From(() =>
        {
            star.Show();
            star.GlobalPosition = GlobalPosition + new Vector2(-22, -22) * Scale.X;
        }));
        sub_tween.TweenProperty(star, "global_position", new Vector2(50.0f, 650.0f), 2.0);
        sub_tween.TweenCallback(Callable.From(() =>
        {
            star.QueueFree();
        }));

        return sub_tween;
    }
}
