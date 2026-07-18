using UnityEngine;

public static class WallBoundsUtility
{
    public struct InnerFaces
    {
        public float LowZ;
        public float HighZ;
        public bool Found;
    }

    public static InnerFaces GetInnerFaces(string wallsParentName, Vector3 referencePosition)
    {
        var result = new InnerFaces
        {
            LowZ = float.NegativeInfinity,
            HighZ = float.PositiveInfinity,
            Found = false
        };

        GameObject wallsParent = GameObject.Find(wallsParentName);
        if (wallsParent == null || wallsParent.transform.childCount < 2)
            return result;

        bool sawLow = false;
        bool sawHigh = false;
        foreach (Transform wall in wallsParent.transform)
        {
            if (wall.GetComponent<BoxCollider>() == null)
                continue;

            float halfDepth = wall.localScale.z * 0.5f;
            if (wall.position.z < referencePosition.z)
            {
                result.LowZ = Mathf.Max(result.LowZ, wall.position.z + halfDepth);
                sawLow = true;
            }
            else
            {
                result.HighZ = Mathf.Min(result.HighZ, wall.position.z - halfDepth);
                sawHigh = true;
            }
        }

        // Only treat walls as resolved when both sides contributed a collider;
        // otherwise callers should keep their serialized fallback ranges.
        result.Found = sawLow && sawHigh;
        return result;
    }

    public static bool TryGetPaddedRange(string wallsParentName, Vector3 referencePosition, float padding,
        out float minZ, out float maxZ)
    {
        InnerFaces faces = GetInnerFaces(wallsParentName, referencePosition);
        if (!faces.Found)
        {
            minZ = 0f;
            maxZ = 0f;
            return false;
        }

        minZ = faces.LowZ + padding;
        maxZ = faces.HighZ - padding;
        return true;
    }
}
