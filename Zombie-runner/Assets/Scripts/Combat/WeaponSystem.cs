using System.Collections.Generic;
using UnityEngine;

namespace ZombieRunner
{
    /// <summary>
    /// Tap-fire pistol with lane aim-assist and armour-piercing rounds.
    /// Mirrors fire() from the browser prototype, including the
    /// "only spend a round when there is something to hit" rule.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        GameManager m_game;
        float m_cooldown;
        readonly List<Entity> m_inRange = new List<Entity>(32);
        readonly List<Entity> m_pool = new List<Entity>(32);
        readonly List<Entity> m_line = new List<Entity>(32);

        public void Init(GameManager game)
        {
            m_game = game;
        }

        public void Tick(float dt)
        {
            if (m_cooldown > 0f) m_cooldown -= dt;
        }

        public void TryFire()
        {
            if (m_game == null || m_game.State != GameState.Playing) return;
            if (m_cooldown > 0f) return;
            m_cooldown = GameConfig.FireCooldown;

            // ---- out of ammo ------------------------------------------------
            if (m_game.Ammo <= 0)
            {
                m_game.Sfx.Dry();
                m_game.UI.FlashAmmo();
                return;
            }

            // ---- gather live targets ---------------------------------------
            m_inRange.Clear();
            List<Entity> active = m_game.Spawner.Pool.Active;
            for (int i = 0; i < active.Count; i++)
            {
                Entity e = active[i];
                if (e == null || e.Dead) continue;
                if (!GameConfig.IsZed(e.Type)) continue;
                if (e.Z >= 3f || e.Z <= -78f) continue;
                m_inRange.Add(e);
            }

            // aim assist: own lane first, then the adjacent lane
            m_pool.Clear();
            for (int i = 0; i < m_inRange.Count; i++)
            {
                if (Mathf.Abs(m_inRange[i].X - m_game.Player.X) < 1.75f) m_pool.Add(m_inRange[i]);
            }
            if (m_pool.Count == 0)
            {
                for (int i = 0; i < m_inRange.Count; i++)
                {
                    if (Mathf.Abs(m_inRange[i].X - m_game.Player.X) < 3.5f) m_pool.Add(m_inRange[i]);
                }
            }
            if (m_pool.Count == 0)
            {
                // nothing to shoot: soft trigger click, no ammo spent
                m_cooldown = 0.08f;
                m_game.Sfx.Dry();
                return;
            }

            m_game.Ammo--;
            m_game.Sfx.Shot();
            m_game.Player.OnFired();
            m_game.CamRig.AddShake(0.16f);

            Vector3 muzzle = m_game.Player.MuzzleWorldPosition;
            Vector3 forward = new Vector3(m_game.Player.X, 1.0f, -72f);

            // nearest target in the assisted lane
            Entity nearest = m_pool[0];
            for (int i = 1; i < m_pool.Count; i++)
            {
                if (m_pool[i].Z > nearest.Z) nearest = m_pool[i];
            }
            float targetX = nearest.X;

            m_line.Clear();
            for (int i = 0; i < m_inRange.Count; i++)
            {
                if (Mathf.Abs(m_inRange[i].X - targetX) < 1.9f) m_line.Add(m_inRange[i]);
            }
            m_line.Sort(CompareByDistance);

            int budget = Upgrades.Pierce;      // bodies one round can punch through
            int killed = 0;
            bool interrupted = false;
            Vector3 to = forward;

            for (int i = 0; i < m_line.Count; i++)
            {
                if (budget <= 0) break;
                Entity target = m_line[i];
                Vector3 sparkPos = new Vector3(target.X, 1.25f, target.Z);
                to = new Vector3(target.X, 1.15f, target.Z);
                target.Hp -= 1;

                if (target.Hp > 0)
                {
                    // armoured brute absorbs the round
                    m_game.Fx.Sparks(sparkPos, 14, new Color(1f, 0.82f, 0.54f), 4f, 4f, m_game.Speed);
                    target.Punch();
                    target.Z -= 0.12f;      // stagger
                    interrupted = true;
                    break;
                }

                target.Dead = true;
                target.Life = 0f;
                m_game.Kills++;
                killed++;
                budget--;
                m_game.Fx.Sparks(sparkPos, killed == 1 ? 20 : 12, new Color(1f, 0.48f, 0.24f), 6f, 5f, m_game.Speed);
                target.Punch();
                m_game.Ammo = Mathf.Min(Upgrades.MaxAmmo, m_game.Ammo + GameConfig.KillAmmoRefund);
                m_game.AddKillScore(target.Type);
            }

            if (killed > 0)
            {
                m_game.Sfx.Impact();
                m_game.Sfx.Flesh();
                m_game.CamRig.AddShake(0.34f);
                m_game.Hitstop = Mathf.Min(0.06f, 0.03f + killed * 0.012f);
                if (m_game.Combo % 5 == 0) m_game.Sfx.Combo(m_game.Combo);
            }
            else if (interrupted)
            {
                m_game.Sfx.Armor();
                m_game.CamRig.AddShake(0.18f);
            }

            m_game.Fx.Bolt(muzzle, to);
            m_game.HudRefresh();
        }

        static int CompareByDistance(Entity a, Entity b)
        {
            // closest to the player first (z grows toward the camera)
            return b.Z.CompareTo(a.Z);
        }
    }
}
