using UnityEngine;

// Shared math for scrolling props: where the bottom of the screen falls along
// a given travel direction, in world space.
public static class ScreenEdgeUtility
{
    public static float BottomAlongTravel(Camera camera, float planeY, Vector3 moveDirection)
    {
        return Vector3.Dot(BottomWorldPoint(camera, planeY), moveDirection);
    }

    public static float TopAlongTravel(Camera camera, float planeY, Vector3 moveDirection)
    {
        return Vector3.Dot(TopWorldPoint(camera, planeY), moveDirection);
    }

    public static Vector3 BottomWorldPoint(Camera camera, float planeY)
    {
        float planeDistance = Mathf.Abs(planeY - camera.transform.position.y);
        return camera.ViewportToWorldPoint(new Vector3(0.5f, 0f, planeDistance));
    }

    public static Vector3 TopWorldPoint(Camera camera, float planeY)
    {
        float planeDistance = Mathf.Abs(planeY - camera.transform.position.y);
        return camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, planeDistance));
    }

    // World-space direction that reads as "down the screen" on the ground plane,
    // shared by anything that needs to move or spawn along that axis.
    public static Vector3 ComputeTravelDirection(Camera camera)
    {
        Vector3 screenUp = camera.transform.forward;
        screenUp.y = 0f;
        if (screenUp.sqrMagnitude < 0.001f)
            screenUp = camera.transform.up;
        screenUp.y = 0f;
        screenUp.Normalize();

        return -screenUp;
    }
}
