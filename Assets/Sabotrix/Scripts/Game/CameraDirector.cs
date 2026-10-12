using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Sabotrix.Game
{
    // Switches the live Cinemachine camera by name (Seat_P1, Seat_P2, Overhead, Cutaway_LightBox).
    // The CinemachineBrain on the Main Camera blends from the old view to the new one.
    public sealed class CameraDirector : MonoBehaviour
    {
        public const string Overhead = "Overhead";

        private readonly Dictionary<string, CinemachineCamera> _cameras = new Dictionary<string, CinemachineCamera>();

        private void Awake()
        {
            foreach (CinemachineCamera cinemachineCamera in GetComponentsInChildren<CinemachineCamera>(true))
            {
                _cameras[cinemachineCamera.name] = cinemachineCamera;
            }
        }

        public static string SeatName(int playerIndex) => $"Seat_P{playerIndex + 1}";

        public void Show(string cameraName) => _cameras[cameraName].Prioritize();
    }
}
