using System.IO;
using Sabotrix.Core;
using Sabotrix.Game;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Sabotrix.Editor
{
    // Builds the gray-box bar (M2.0) out of primitives and saves it as the Bar prefab.
    // Rerunning overwrites the prefab in place, so scenes that use it pick up the changes.
    // Props are named so later code can find them by name: LightBox, BreakerBox, Beholder, Drink_P1, Seat_P1, ...
    // Units are meters. The table sits at the origin. Player 1 faces +Z, Player 2 faces -Z.
    public static class BarBlockoutBuilder
    {
        private const string PrefabPath = "Assets/Sabotrix/Prefabs/Bar.prefab";
        private const string MaterialFolder = "Assets/Sabotrix/Art/Blockout";
        private const string LitShader = "Universal Render Pipeline/Lit";

        private const float RoomWidth = 10f;
        private const float RoomDepth = 10f;
        private const float RoomHeight = 4f;
        private const float WallThickness = 0.2f;
        private const float TrussHeight = 3.6f;
        private const float TableHeight = 0.75f;
        private const float SeatedEyeHeight = 1.2f;
        private const float SeatDistance = 0.85f;
        private const int HazardStripes = 10;
        private const float SeatCameraTilt = 12f;
        private const float CameraFieldOfView = 60f;

        // The bar counter along the back wall, with stools in front and the back-bar behind.
        private const float CounterZ = RoomDepth / 2 - 1.5f;
        private const float CounterLength = 5f;
        private const float CounterDepth = 0.6f;
        private const float CounterHeight = 1.05f;
        private const float StoolZ = CounterZ - 0.75f;
        private const float StoolHeight = 0.75f;
        private const int StoolCount = 5;
        private const float BackBarZ = RoomDepth / 2 - 0.35f;
        private const float BackBarWidth = 4.4f;
        private const int BackBarShelves = 3;

        // Player patrons hold their hand as a fan of face-down cards.
        private const float CardFanAngle = 10f;
        private const float CardFanSpacing = 0.05f;

        // Fixed seed so the bottles come out the same on every rebuild.
        private const int BottleSeed = 7;

        [MenuItem("Sabotrix/Build Bar Blockout")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();

            GameObject root = new GameObject("Bar");
            BuildRoom(root.transform);
            BuildTable(root.transform);
            BuildPlayerSide(root.transform, 1, -1f);
            BuildPlayerSide(root.transform, 2, 1f);
            BuildLightBox(root.transform);
            BuildBeholder(root.transform);
            BuildWallProps(root.transform);
            BuildBarCounter(root.transform);
            BuildBarStools(root.transform);
            BuildBackBar(root.transform);
            BuildWallDecorations(root.transform);
            BuildCameras(root.transform);
            root.AddComponent<CameraDirector>();
            root.AddComponent<SeatedPlayers>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Bar blockout saved to {PrefabPath}");
        }

        private static void BuildRoom(Transform parent)
        {
            Transform room = Group("Room", parent);
            Material brick = GetMaterial("Brick", new Color(0.55f, 0.22f, 0.16f));
            Material steel = GetMaterial("RedSteel", new Color(0.7f, 0.1f, 0.08f));
            Material pipe = GetMaterial("Pipe", new Color(0.45f, 0.45f, 0.48f));

            Box("Floor", room, new Vector3(0, -0.05f, 0), new Vector3(RoomWidth, 0.1f, RoomDepth), GetMaterial("FloorWood", new Color(0.3f, 0.2f, 0.13f)));
            Box("Ceiling", room, new Vector3(0, RoomHeight, 0), new Vector3(RoomWidth, 0.1f, RoomDepth), GetMaterial("Ceiling", new Color(0.12f, 0.12f, 0.13f)));
            Box("Wall_Back", room, new Vector3(0, RoomHeight / 2, RoomDepth / 2), new Vector3(RoomWidth, RoomHeight, WallThickness), brick);
            Box("Wall_Front", room, new Vector3(0, RoomHeight / 2, -RoomDepth / 2), new Vector3(RoomWidth, RoomHeight, WallThickness), brick);
            Box("Wall_Left", room, new Vector3(-RoomWidth / 2, RoomHeight / 2, 0), new Vector3(WallThickness, RoomHeight, RoomDepth), brick);
            Box("Wall_Right", room, new Vector3(RoomWidth / 2, RoomHeight / 2, 0), new Vector3(WallThickness, RoomHeight, RoomDepth), brick);

            // Exposed red steel trusses across the ceiling, with pipes running the other way.
            foreach (float z in new[] { -4f, -2f, 0f, 2f, 4f })
            {
                Box($"Truss_{z:0}", room, new Vector3(0, TrussHeight, z), new Vector3(RoomWidth, 0.25f, 0.2f), steel);
            }

            foreach (float x in new[] { -3f, 3f })
            {
                Cylinder($"Pipe_{x:0}", room, new Vector3(x, TrussHeight + 0.2f, 0), new Vector3(0.15f, RoomDepth / 2, 0.15f), pipe, new Vector3(90, 0, 0));
            }
        }

        private static void BuildTable(Transform parent)
        {
            Transform table = Group("Table", parent);
            Material wood = GetMaterial("TableWood", new Color(0.45f, 0.28f, 0.15f));
            Box("Top", table, new Vector3(0, TableHeight, 0), new Vector3(1.2f, 0.06f, 1f), wood);
            Cylinder("Leg", table, new Vector3(0, TableHeight / 2, 0), new Vector3(0.12f, TableHeight / 2, 0.12f), wood);
        }

        // side: -1 puts this player on the -Z side of the table (facing +Z), 1 on the +Z side.
        private static void BuildPlayerSide(Transform parent, int player, float side)
        {
            Material chair = GetMaterial("Chair", new Color(0.1f, 0.1f, 0.1f));
            float facing = side < 0 ? 0 : 180;

            Transform chairRoot = Group($"Chair_P{player}", parent);
            chairRoot.localPosition = new Vector3(0, 0, side * SeatDistance);
            chairRoot.localEulerAngles = new Vector3(0, facing, 0);
            Box("Seat", chairRoot, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chair);
            Box("Back", chairRoot, new Vector3(0, 0.72f, -0.22f), new Vector3(0.45f, 0.5f, 0.05f), chair);
            Box("Base", chairRoot, new Vector3(0, 0.22f, 0), new Vector3(0.08f, 0.44f, 0.08f), chair);
            BuildPlayerPatron(parent, player, chairRoot);

            // The seated player's eyes, tilted down toward the table. This is the first-person camera.
            Transform seat = Group($"Seat_P{player}", parent);
            seat.localPosition = new Vector3(0, SeatedEyeHeight, side * SeatDistance);
            seat.localEulerAngles = new Vector3(SeatCameraTilt, facing, 0);
            AddCamera(seat);

            // Drink on the table, to this player's right.
            Cylinder($"Drink_P{player}", parent, new Vector3(-side * 0.4f, TableHeight + 0.1f, side * 0.35f), new Vector3(0.08f, 0.075f, 0.08f),
                GetMaterial("Drink", new Color(0.85f, 0.55f, 0.1f)));
        }

        // The player sitting in this chair, gray-box until human models arrive. Local +Z faces the table.
        // SeatedPlayers hides your own patron in first person and shows the other one's hand as card backs.
        private static void BuildPlayerPatron(Transform parent, int player, Transform chair)
        {
            Transform patron = Group($"Patron_P{player}", parent);
            patron.localPosition = chair.localPosition;
            patron.localRotation = chair.localRotation;

            Color shirtColor = player == 1 ? new Color(0.8f, 0.45f, 0.15f) : new Color(0.3f, 0.6f, 0.5f);
            Material shirt = GetMaterial($"PlayerShirt_P{player}", shirtColor);
            Material pants = GetMaterial("Pants", new Color(0.15f, 0.17f, 0.25f));
            Material skin = GetMaterial("PatronSkin", new Color(0.85f, 0.65f, 0.5f));

            Capsule("Body", patron, new Vector3(0, 0.85f, 0), new Vector3(0.42f, 0.38f, 0.28f), shirt);
            Sphere("Head", patron, new Vector3(0, 1.32f, 0), Vector3.one * 0.25f, skin);
            Box("Thighs", patron, new Vector3(0, 0.52f, 0.18f), new Vector3(0.34f, 0.12f, 0.36f), pants);
            Box("Shins", patron, new Vector3(0, 0.25f, 0.36f), new Vector3(0.3f, 0.5f, 0.12f), pants);
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                Box($"Arm_{(x < 0 ? "L" : "R")}", patron, new Vector3(x, 0.95f, 0.18f), new Vector3(0.09f, 0.09f, 0.36f), shirt);
            }

            // One card back per possible card in hand. SeatedPlayers turns on as many as the player holds.
            Transform hand = Group("Hand", patron);
            hand.localPosition = new Vector3(0, 1.05f, 0.4f);
            hand.localEulerAngles = new Vector3(-20, 0, 0);
            Material cardBack = GetMaterial("CardBack", new Color(0.2f, 0.2f, 0.45f));
            float middle = (Rules.MaxHandSize - 1) / 2f;
            for (int i = 0; i < Rules.MaxHandSize; i++)
            {
                GameObject card = Box($"Card_{i + 1}", hand, new Vector3((i - middle) * CardFanSpacing, 0, i * 0.002f), new Vector3(0.1f, 0.14f, 0.003f), cardBack);
                card.transform.localEulerAngles = new Vector3(0, 0, -(i - middle) * CardFanAngle);
            }
        }

        // The pendant light box over the table. Blackout smashes it.
        private static void BuildLightBox(Transform parent)
        {
            Transform lightBox = Group("LightBox", parent);
            lightBox.localPosition = new Vector3(0, 2.4f, 0);
            Box("Box", lightBox, Vector3.zero, new Vector3(0.8f, 0.2f, 0.5f), GetMaterial("LightBox", new Color(1f, 0.85f, 0.55f), emissive: true));
            Cylinder("Chain", lightBox, new Vector3(0, (TrussHeight - 2.4f) / 2, 0), new Vector3(0.02f, (TrussHeight - 2.4f) / 2, 0.02f), GetMaterial("Chain", new Color(0.3f, 0.3f, 0.32f)));
        }

        // The beholder sculpture hanging on a chain. Beholder Drop swings it into the table.
        private static void BuildBeholder(Transform parent)
        {
            Transform beholder = Group("Beholder", parent);
            beholder.localPosition = new Vector3(-2.2f, 2.5f, 1.5f);
            Sphere("Body", beholder, Vector3.zero, Vector3.one, GetMaterial("Beholder", new Color(0.45f, 0.3f, 0.55f)));
            Sphere("Eye", beholder, new Vector3(0.2f, 0, -0.38f), Vector3.one * 0.35f, GetMaterial("EyeWhite", Color.white));
            Sphere("Pupil", beholder, new Vector3(0.27f, 0, -0.53f), Vector3.one * 0.12f, GetMaterial("Pupil", Color.black));
            Cylinder("Chain", beholder, new Vector3(0, 0.8f, 0), new Vector3(0.03f, 0.3f, 0.03f), GetMaterial("Chain", new Color(0.3f, 0.3f, 0.32f)));
        }

        private static void BuildWallProps(Transform parent)
        {
            Material screen = GetMaterial("TvScreen", new Color(0.25f, 0.55f, 1f), emissive: true);
            float backWall = RoomDepth / 2 - WallThickness;
            Box("TV_1", parent, new Vector3(-2f, 2.9f, backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);
            Box("TV_2", parent, new Vector3(2f, 2.9f, backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);
            Box("TV_3", parent, new Vector3(0, 2.5f, -backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);

            // Breaker box on the right wall, near the table. Short Circuit sparks it.
            Box("BreakerBox", parent, new Vector3(RoomWidth / 2 - WallThickness, 1.5f, 0.8f), new Vector3(0.12f, 0.6f, 0.4f), GetMaterial("BreakerBox", new Color(0.35f, 0.38f, 0.35f)));

            BuildGameShelf(parent);
            BuildHazardStep(parent);
        }

        private static void BuildGameShelf(Transform parent)
        {
            Transform shelf = Group("GameShelf", parent);
            shelf.localPosition = new Vector3(-RoomWidth / 2 + 0.4f, 0, -1.2f);
            Box("Frame", shelf, new Vector3(0, 1f, 0), new Vector3(0.4f, 2f, 2.4f), GetMaterial("ShelfWood", new Color(0.25f, 0.15f, 0.08f)));

            Color[] boxColors = { new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.8f), new Color(0.2f, 0.7f, 0.3f), new Color(0.9f, 0.8f, 0.2f) };
            for (int i = 0; i < boxColors.Length; i++)
            {
                Box($"Game_{i + 1}", shelf, new Vector3(0.25f, 0.5f + i * 0.4f, -0.8f + i * 0.5f), new Vector3(0.1f, 0.3f, 0.4f), GetMaterial($"Game_{i + 1}", boxColors[i]));
            }
        }

        private static void BuildHazardStep(Transform parent)
        {
            Transform step = Group("HazardStep", parent);
            step.localPosition = new Vector3(3f, 0, -RoomDepth / 2 + 0.6f);
            const float stepWidth = 3f;
            Box("Step", step, new Vector3(0, 0.075f, 0), new Vector3(stepWidth, 0.15f, 0.8f), GetMaterial("Step", new Color(0.2f, 0.2f, 0.2f)));

            // Yellow and black stripes along the front edge.
            Material yellow = GetMaterial("HazardYellow", new Color(0.95f, 0.8f, 0.1f));
            Material black = GetMaterial("HazardBlack", new Color(0.05f, 0.05f, 0.05f));
            float stripeWidth = stepWidth / HazardStripes;
            for (int i = 0; i < HazardStripes; i++)
            {
                float x = -stepWidth / 2 + stripeWidth * (i + 0.5f);
                Box($"Stripe_{i + 1}", step, new Vector3(x, 0.152f, 0.32f), new Vector3(stripeWidth, 0.01f, 0.16f), i % 2 == 0 ? yellow : black);
            }
        }

        // A long dark counter with rounded ends and a white neon strip along the bottom
        // (reference: the bar in The Amazing Digital Circus, episode 5).
        private static void BuildBarCounter(Transform parent)
        {
            Transform counter = Group("BarCounter", parent);
            counter.localPosition = new Vector3(0, 0, CounterZ);
            Material body = GetMaterial("CounterBody", new Color(0.12f, 0.12f, 0.13f));
            Material top = GetMaterial("CounterTop", new Color(0.05f, 0.05f, 0.06f));
            Box("Body", counter, new Vector3(0, CounterHeight / 2, 0), new Vector3(CounterLength, CounterHeight, CounterDepth), body);
            Box("Top", counter, new Vector3(0, CounterHeight + 0.03f, 0), new Vector3(CounterLength, 0.06f, CounterDepth + 0.1f), top);

            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1 : 1) * CounterLength / 2;
                Cylinder($"End_{i + 1}", counter, new Vector3(x, CounterHeight / 2, 0), new Vector3(CounterDepth, CounterHeight / 2, CounterDepth), body);
                Cylinder($"EndTop_{i + 1}", counter, new Vector3(x, CounterHeight + 0.03f, 0), new Vector3(CounterDepth + 0.1f, 0.03f, CounterDepth + 0.1f), top);
            }

            Box("NeonStrip", counter, new Vector3(0, 0.12f, -CounterDepth / 2 - 0.01f), new Vector3(CounterLength, 0.04f, 0.02f),
                GetMaterial("NeonWhite", new Color(0.9f, 0.95f, 1f), emissive: true));
        }

        // Stools along the counter. Every other stool has a background patron: decoration only, not a player.
        // The patrons are gray-box until M2.2 swaps in human models.
        private static void BuildBarStools(Transform parent)
        {
            Material seat = GetMaterial("StoolSeat", new Color(0.85f, 0.85f, 0.8f));
            Material leg = GetMaterial("StoolLeg", new Color(0.4f, 0.4f, 0.42f));
            Material skin = GetMaterial("PatronSkin", new Color(0.85f, 0.65f, 0.5f));
            Color[] shirtColors = { new Color(0.2f, 0.35f, 0.6f), new Color(0.5f, 0.2f, 0.25f), new Color(0.25f, 0.45f, 0.3f) };
            float spacing = (CounterLength - 1f) / (StoolCount - 1);

            for (int i = 0; i < StoolCount; i++)
            {
                Transform stool = Group($"Stool_{i + 1}", parent);
                stool.localPosition = new Vector3(-CounterLength / 2 + 0.5f + i * spacing, 0, StoolZ);
                Cylinder("Seat", stool, new Vector3(0, StoolHeight, 0), new Vector3(0.36f, 0.03f, 0.36f), seat);
                Cylinder("Leg", stool, new Vector3(0, StoolHeight / 2, 0), new Vector3(0.05f, StoolHeight / 2, 0.05f), leg);

                if (i % 2 != 0)
                {
                    continue;
                }

                int patron = i / 2;
                Transform patronRoot = Group($"BarPatron_{patron + 1}", stool);
                Capsule("Body", patronRoot, new Vector3(0, StoolHeight + 0.45f, 0), new Vector3(0.4f, 0.45f, 0.3f),
                    GetMaterial($"PatronShirt_{patron + 1}", shirtColors[patron]));
                Sphere("Head", patronRoot, new Vector3(0, StoolHeight + 1.05f, 0), Vector3.one * 0.25f, skin);
            }
        }

        // Behind the counter: a cabinet, a mirror on the wall, and shelves of bottles in front of the mirror.
        private static void BuildBackBar(Transform parent)
        {
            Transform backBar = Group("BackBar", parent);
            backBar.localPosition = new Vector3(0, 0, BackBarZ);
            Material wood = GetMaterial("BackBarWood", new Color(0.2f, 0.12f, 0.07f));
            Box("Cabinet", backBar, new Vector3(0, 0.45f, 0), new Vector3(BackBarWidth, 0.9f, 0.4f), wood);

            Material mirror = GetMaterial("Mirror", new Color(0.75f, 0.8f, 0.85f));
            mirror.SetFloat("_Smoothness", 1f);
            mirror.SetFloat("_Metallic", 1f);
            Box("Mirror", backBar, new Vector3(0, 1.55f, 0.12f), new Vector3(BackBarWidth, 1.2f, 0.02f), mirror);

            Material[] glass =
            {
                GetMaterial("BottleGreen", new Color(0.15f, 0.5f, 0.2f)),
                GetMaterial("BottleAmber", new Color(0.7f, 0.4f, 0.1f)),
                GetMaterial("BottleClear", new Color(0.75f, 0.85f, 0.9f)),
                GetMaterial("BottleRed", new Color(0.6f, 0.1f, 0.15f)),
                GetMaterial("BottleBlue", new Color(0.2f, 0.3f, 0.7f))
            };
            System.Random random = new System.Random(BottleSeed);
            for (int shelf = 0; shelf < BackBarShelves; shelf++)
            {
                float y = 1f + shelf * 0.45f;
                Box($"Shelf_{shelf + 1}", backBar, new Vector3(0, y, 0), new Vector3(BackBarWidth, 0.04f, 0.25f), wood);
                AddBottles(backBar, shelf + 1, y + 0.02f, glass, random);
            }
        }

        // Fills one shelf left to right with bottles of random width, height, color, and shape (tall or round).
        private static void AddBottles(Transform backBar, int shelf, float y, Material[] glass, System.Random random)
        {
            float x = -BackBarWidth / 2 + 0.15f;
            for (int n = 1; x < BackBarWidth / 2 - 0.2f; n++)
            {
                float width = 0.1f + (float)random.NextDouble() * 0.08f;
                bool round = random.NextDouble() < 0.25;
                float height = round ? width : 0.15f + (float)random.NextDouble() * 0.15f;
                Material material = glass[random.Next(glass.Length)];

                Transform bottle = Group($"Bottle_{shelf}_{n}", backBar);
                bottle.localPosition = new Vector3(x + width / 2, y, 0);
                if (round)
                {
                    Sphere("Body", bottle, new Vector3(0, height / 2, 0), Vector3.one * width, material);
                }
                else
                {
                    Cylinder("Body", bottle, new Vector3(0, height / 2, 0), new Vector3(width, height / 2, width), material);
                }

                Cylinder("Neck", bottle, new Vector3(0, height + 0.05f, 0), new Vector3(width * 0.35f, 0.05f, width * 0.35f), material);
                x += width + 0.05f;
            }
        }

        // Neon signs and framed pictures on the side walls, like the reference bar.
        private static void BuildWallDecorations(Transform parent)
        {
            float wallFace = RoomWidth / 2 - WallThickness / 2 - 0.02f;
            Box("Neon_Pink", parent, new Vector3(-wallFace, 2.3f, 2.5f), new Vector3(0.04f, 0.6f, 1f), GetMaterial("NeonPink", new Color(1f, 0.3f, 0.8f), emissive: true));
            Box("Neon_Yellow", parent, new Vector3(-wallFace, 2.3f, -3.2f), new Vector3(0.04f, 0.6f, 0.9f), GetMaterial("NeonYellow", new Color(1f, 0.85f, 0.2f), emissive: true));
            Box("Neon_Green", parent, new Vector3(wallFace, 2.4f, 3.2f), new Vector3(0.04f, 0.5f, 1.1f), GetMaterial("NeonGreen", new Color(0.3f, 1f, 0.6f), emissive: true));

            Material frame = GetMaterial("PictureFrame", new Color(0.08f, 0.08f, 0.08f));
            Material picture = GetMaterial("Picture", new Color(0.8f, 0.78f, 0.7f));
            float[] frameZ = { -3.2f, -1.8f, 1.8f };
            for (int i = 0; i < frameZ.Length; i++)
            {
                Box($"Frame_{i + 1}", parent, new Vector3(wallFace, 2.2f, frameZ[i]), new Vector3(0.04f, 0.6f, 0.45f), frame);
                Box($"Picture_{i + 1}", parent, new Vector3(wallFace - 0.02f, 2.2f, frameZ[i]), new Vector3(0.02f, 0.48f, 0.35f), picture);
            }
        }

        // The seat cameras are built with the chairs. These are the other views.
        private static void BuildCameras(Transform parent)
        {
            // Straight down at the table, below the light box. Player 1 is at the bottom of the screen.
            Transform overhead = Group(CameraDirector.Overhead, parent);
            overhead.localPosition = new Vector3(0, 2.1f, 0);
            overhead.localEulerAngles = new Vector3(90, 0, 0);
            AddCamera(overhead);

            // Blackout cuts here to watch the light box smash.
            Transform cutaway = Group("Cutaway_LightBox", parent);
            cutaway.localPosition = new Vector3(1.6f, 1.5f, -1.6f);
            cutaway.localRotation = Quaternion.LookRotation(new Vector3(0, 2.4f, 0) - cutaway.localPosition);
            AddCamera(cutaway);
        }

        private static void AddCamera(Transform target)
        {
            CinemachineCamera cinemachineCamera = target.gameObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Lens.FieldOfView = CameraFieldOfView;
        }

        private static Transform Group(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Cube, name, parent, position, scale, material, Vector3.zero);

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Sphere, name, parent, position, scale, material, Vector3.zero);

        // A Unity capsule is 2 units tall, so scale.y is half the height.
        private static GameObject Capsule(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Capsule, name, parent, position, scale, material, Vector3.zero);

        // A Unity cylinder is 2 units tall, so scale.y is half the height.
        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default) =>
            Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, rotation);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localEulerAngles = rotation;
            shape.transform.localScale = scale;
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            return shape;
        }

        // Loads the material asset, or creates it the first time. The color is set every run so tweaks here apply on rebuild.
        private static Material GetMaterial(string name, Color color, bool emissive = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
