using UnityEngine;

namespace Sabotrix.Game
{
    // The bar's light switches. Turns every RoomLight in a group on or off.
    // The Room group also controls the ambient light and sky reflections (the back-bar mirror),
    // so the bar goes properly dark without it.
    public sealed class RoomLighting : MonoBehaviour
    {
        private static readonly Color DarkAmbient = new Color(0.01f, 0.01f, 0.02f);

        private RoomLight[] _lights;
        private Color _ambient;
        private float _reflections;

        private void Awake()
        {
            _lights = GetComponentsInChildren<RoomLight>(true);
            _ambient = RenderSettings.ambientLight;
            _reflections = RenderSettings.reflectionIntensity;
        }

        public void SetGroup(LightGroup group, bool on)
        {
            foreach (RoomLight roomLight in _lights)
            {
                if (roomLight.Group == group)
                {
                    roomLight.SetOn(on);
                }
            }

            if (group == LightGroup.Room)
            {
                RenderSettings.ambientLight = on ? _ambient : DarkAmbient;
                RenderSettings.reflectionIntensity = on ? _reflections : 0f;
            }
        }
    }
}
