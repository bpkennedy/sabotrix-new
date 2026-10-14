using System;
using System.Collections;
using UnityEngine;

namespace Sabotrix.Game
{
    // Blackout's room effect (GDD 10.4): the light box sparks, the lights flicker and die, and the box drops
    // and swings on its chain. Only the TVs keep glowing. Restore() puts it all back once the dark round resolves.
    public sealed class BlackoutEffect : MonoBehaviour
    {
        private const string CutawayCamera = "Cutaway_LightBox";
        private const int Flickers = 3;
        private const float FlickerSeconds = 0.08f;
        private const float DropSeconds = 1.2f;
        private const float DropDistance = 0.25f;
        private const float SwingDegrees = 18f;
        private const float SwingsPerSecond = 1.5f;
        private const float HoldSeconds = 0.5f;

        private CameraDirector _cameras;
        private RoomLighting _lighting;
        private Transform _lightBox;
        private ParticleSystem _sparks;
        private Vector3 _lightBoxPosition;
        private Quaternion _lightBoxRotation;

        public bool IsDark { get; private set; }

        private void Awake()
        {
            _cameras = GetComponent<CameraDirector>();
            _lighting = GetComponent<RoomLighting>();
            _lightBox = transform.Find("LightBox");
            _sparks = _lightBox.GetComponentInChildren<ParticleSystem>();
            _lightBoxPosition = _lightBox.localPosition;
            _lightBoxRotation = _lightBox.localRotation;
        }

        // cutaway: cut to the light box for the effect, then back overhead.
        // isSkipped: once it returns true, the effect jumps to the end (dark room, overhead camera).
        public IEnumerator Play(bool cutaway, Func<bool> isSkipped)
        {
            if (cutaway)
            {
                _cameras.Show(CutawayCamera);
                yield return Wait(CameraDirector.BlendSeconds, isSkipped);
            }

            // The box sparks and falls while the lights flicker, so you see it happen before the room goes dark.
            _sparks.Play();
            const float flickerEnd = Flickers * 2 * FlickerSeconds;
            for (float elapsed = 0; elapsed < DropSeconds && !isSkipped(); elapsed += Time.deltaTime)
            {
                bool on = elapsed < flickerEnd && Mathf.FloorToInt(elapsed / FlickerSeconds) % 2 == 1;
                if (on == IsDark)
                {
                    SetLights(on);
                }

                PoseLightBox(elapsed / DropSeconds);
                yield return null;
            }

            SetLights(false);
            PoseLightBox(1f);
            yield return Wait(HoldSeconds, isSkipped);

            if (cutaway)
            {
                _cameras.Show(CameraDirector.Overhead);
                yield return Wait(CameraDirector.BlendSeconds, isSkipped);
            }
        }

        public void Restore()
        {
            _lightBox.localPosition = _lightBoxPosition;
            _lightBox.localRotation = _lightBoxRotation;
            SetLights(true);
        }

        private void SetLights(bool on)
        {
            _lighting.SetGroup(LightGroup.Room, on);
            _lighting.SetGroup(LightGroup.Table, on);
            IsDark = !on;
        }

        // progress 0 to 1. The box falls in the first third, then swings on its chain until the swing dies out.
        private void PoseLightBox(float progress)
        {
            float drop = Mathf.Min(1f, progress * 3f);
            float swing = SwingDegrees * (1f - progress) * Mathf.Sin(progress * DropSeconds * SwingsPerSecond * 2f * Mathf.PI);
            _lightBox.localPosition = _lightBoxPosition + Vector3.down * DropDistance * drop;
            _lightBox.localRotation = _lightBoxRotation * Quaternion.Euler(0, 0, swing);
        }

        private static IEnumerator Wait(float seconds, Func<bool> isSkipped)
        {
            for (float elapsed = 0; elapsed < seconds && !isSkipped(); elapsed += Time.deltaTime)
            {
                yield return null;
            }
        }
    }
}
