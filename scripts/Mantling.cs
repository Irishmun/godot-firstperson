using Godot;
using System;

public partial class Mantling : Node
{
    [Export] private RayCast3D wallRay;
    [Export] private RayCast3D floorRay;
    [Export] private RayCast3D edgeRay;

    [Export] private Node3D mantleHit;

    public override void _Ready()
    {
        base._Ready();
        if (mantleHit != null)
        {
            mantleHit.TopLevel = true;
        }
    }

    public bool HandleMantle(float delta, out Vector3 mantlePos)
    {
        mantlePos = Vector3.Inf;
        wallRay.ForceRaycastUpdate();
        GodotObject hit = wallRay.GetCollider();
        if (hit is StaticBody3D || hit is CsgShape3D)
        { return false; }
        floorRay.ForceRaycastUpdate();
        hit = floorRay.GetCollider();
        if (hit is StaticBody3D || hit is CsgShape3D)
        {
            //set edge ray height, then force update
            Vector3 edgePos = edgeRay.GlobalPosition;
            edgePos.Y = floorRay.GetCollisionPoint().Y - 0.01f;
            edgeRay.GlobalPosition = edgePos;
            edgeRay.ForceRaycastUpdate();
            GD.Print("edge point:" + edgePos);
            GodotObject edgeHit = edgeRay.GetCollider();
            if (edgeHit != hit)
            { return false; }
            GD.Print("mantle");
            mantlePos = edgeRay.GetCollisionPoint();
            mantlePos.Y = floorRay.GetCollisionPoint().Y;

            if (mantleHit != null)
            {
                mantleHit.GlobalPosition = mantlePos;
            }
            return true;
        }
        GD.Print("no mantle");
        return false;
    }

}
