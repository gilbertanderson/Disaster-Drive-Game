using UnityEngine;

// Shared math for scrolling props: where the bottom of the screen falls along
// a given travel direction, in world space.
public static class ScreenEdgeUtility
{
    public static float BottomAlongTravel(Camera camera, float planeY, Vector3 moveDirection)
    {
        float planeDistance = Mathf.Abs(planeY - camera.transform.position.y);
        Vector3 bottomWorld = camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, planeDistance));
        return Vector3.Dot(bottomWorld, moveDirection);
    }
}
