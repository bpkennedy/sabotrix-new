using UnityEngine;

namespace Sabotrix.Game
{
    // Which switch a light in the bar is wired to. Blackout turns off Room and Table and leaves the TVs on.
    public enum LightGroup
    {
        Room,
        Tv,
        Table
    }

    // Marks a glowing prop in the bar (lamp, TV, neon sign) and the Light components under it.
    // Turning it off disables those Lights and blacks out the glow (emission) on its materials.
    public sealed class RoomLight : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("The switch this prop is wired to.")]
        [SerializeField] private LightGroup _group;

        public LightGroup Group => _group;

        public void SetOn(bool on)
        {
            foreach (Light lamp in GetComponentsInChildren<Light>(true))
            {
                lamp.enabled = on;
            }

            // A property block overrides the material's glow on this object only, without changing the shared material.
            MaterialPropertyBlock dark = new MaterialPropertyBlock();
            dark.SetColor(EmissionColorId, Color.black);
            foreach (Renderer glowing in GetComponentsInChildren<Renderer>(true))
            {
                glowing.SetPropertyBlock(on ? null : dark);
            }
        }
    }
}
