using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Chase camera: lateral follow, speed-driven FOV kick, screen shake and the
    /// slow dolly used behind the menus.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Transform Sun;

        public float Shake;

        float m_idleTime;
        Vector3 m_lookTarget;

        public void Init(Camera cam, Transform sun)
        {
            Cam = cam;
            Sun = sun;
            m_lookTarget = new Vector3(0f, 1.55f, -9.5f);
        }

        public void AddShake(float amount)
        {
            Shake = Mathf.Max(Shake, amount);
        }

        public void SnapToPlayer(PlayerController player)
        {
            Cam.transform.position = new Vector3(player.X * 0.5f, 3.5f, 7.7f);
            Cam.fieldOfView = 64f;
            Cam.transform.LookAt(new Vector3(player.X * 0.32f, 1.55f, -9.5f));
        }

        /// <summary>Per-frame camera work while a run is live.</summary>
        public void TickPlaying(PlayerController player, float dt, float speed)
        {
            float bob = Mathf.Abs(Mathf.Sin(player.RunPhase)) * 0.045f;
            float targetFov = 64f + Mathf.Max(0f, speed - 11f) * 0.75f;
            Cam.fieldOfView += (targetFov - Cam.fieldOfView) * Mathf.Min(1f, dt * 3f);

            float sx = 0f, sy = 0f;
            if (Shake > 0f)
            {
                Shake = Mathf.Max(0f, Shake - dt * 2.2f);
                sx = (Random.value - 0.5f) * Shake * 0.8f;
                sy = (Random.value - 0.5f) * Shake * 0.6f;
            }

            bool sliding = player.Sliding;
            float camY = 3.55f + bob + player.Y * 0.42f - (sliding ? 0.45f : 0f);
            float followX = player.X * 0.5f;

            Vector3 p = Cam.transform.position;
            p.x += (followX + sx - p.x) * Mathf.Min(1f, dt * 6f);
            p.y += (camY + sy - p.y) * Mathf.Min(1f, dt * 6f);
            p.z = 7.7f;
            Cam.transform.position = p;

            m_lookTarget = new Vector3(player.X * 0.32f, 1.55f + player.Y * 0.55f, -9.5f);
            Cam.transform.LookAt(m_lookTarget);
        }

        /// <summary>Slow cinematic dolly for menus / base camp.</summary>
        public void TickIdle(float dt)
        {
            m_idleTime += dt * 0.14f;
            float x = Mathf.Sin(m_idleTime) * 3.2f;
            float y = 3.0f + Mathf.Sin(m_idleTime * 0.8f) * 0.5f;
            Cam.transform.position = new Vector3(x, y, 8.4f);
            Cam.transform.LookAt(new Vector3(0f, 1.6f, -10f));
            Cam.fieldOfView += (58f - Cam.fieldOfView) * Mathf.Min(1f, dt * 3f);
            Shake = 0f;
        }

        /// <summary>Death stumble: pull back and watch the survivor go down.</summary>
        public void TickDying(PlayerController player, float dt)
        {
            Shake = Mathf.Max(Shake, 0.25f);
            Vector3 p = Cam.transform.position;
            p.z = 7.7f + player.transform.localPosition.z * 0.35f;
            Cam.transform.position = p;
            Cam.transform.LookAt(new Vector3(player.X * 0.3f, 1.2f, -9.5f));
        }
    }
}
