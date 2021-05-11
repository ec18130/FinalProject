using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OrientedPoint
{
    public Vector3 point;
    public Quaternion rot;

    public OrientedPoint(Vector3 point, Quaternion rot)
    {
        this.point = point;
        this.rot = rot;
    }

    public OrientedPoint(Vector3 point, Vector3 forward)
    {
        this.point = point;
        this.rot = Quaternion.LookRotation(forward);
    }

    public Vector3 LocalToWorldPos(Vector3 localSpacePos)
    {
        return point + rot * localSpacePos;
    }

    public Vector3 LocalToWorldVector(Vector3 localSpacePos)
    {
        return rot * localSpacePos;
    }

}
