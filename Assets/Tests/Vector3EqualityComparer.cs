using System.Collections.Generic;
using UnityEngine;

internal sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
{
    public static readonly Vector3EqualityComparer Instance = new Vector3EqualityComparer();
    private const float Epsilon = 1e-4f;

    public bool Equals(Vector3 x, Vector3 y)
    {
        return Vector3.SqrMagnitude(x - y) < Epsilon * Epsilon;
    }

    public int GetHashCode(Vector3 obj)
    {
        return obj.GetHashCode();
    }
}
