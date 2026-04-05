using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace CannonGame
{
    internal class AmmoType
    {
        public string Name { get; set; }
        public float Power { get; set; }          // How strong the shot is
        public float ExplosionRadius { get; set; } // How big the boom is
        public Color Color { get; set; }

        public AmmoType(string name, float power, float radius, Color color)
        {
            Name = name;
            Power = power;
            ExplosionRadius = radius;
            Color = color;
        }
    }

    internal class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public AmmoType Type;
        public bool IsActive = true;

        public Bullet(Vector2 startPos, Vector2 direction, float power, AmmoType type)
        {
            Position = startPos;
            Velocity = direction * power;
            Type = type;
        }

        // Update the bullet's movement (gravity pulls it down)
        public void Update(float dt)
        {
            Velocity.Y += 380f * dt;           // Gravity
            Position += Velocity * dt;
        }
    }

    internal class Cannon
    {
        public Vector2 Position;           // Base position on the ground
        public float Angle = -45f;         // Angle in degrees 
        public int Health = 100;
        public Color Color;

        public Cannon(Vector2 pos, Color color)
        {
            Position = pos;
            Color = color;
        }

        // Draw the cannon body + rotating barrel
        public void Draw()
        {
            // Cannon base (the tank-like part)
            Raylib.DrawRectangle((int)Position.X - 22, (int)Position.Y - 12, 44, 18, Color);

            // Rotating barrel
            float rad = Angle * (MathF.PI / 180f);
            Vector2 barrelEnd = Position + new Vector2(MathF.Cos(rad) * 38f, MathF.Sin(rad) * 38f);

            Raylib.DrawLineEx(Position, barrelEnd, 11f, new Color(80, 80, 80, 255));
            Raylib.DrawCircleV(Position, 13f, Color);   // The hinge
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Setup
            Raylib.InitWindow(1000, 600, "Tykkipeli - Artillery Battle");
            Raylib.SetTargetFPS(60);

            // Connst
            float groundY = 520f;                    // Where the flat ground starts
            int terrainSegments = 50;                // How many pieces the ground is made of
            float segmentWidth = 1000f / terrainSegments;

            // Terrain
            // Simple hilly terrain using heights
            float[] terrainHeight = new float[terrainSegments];
            Random rand = new Random();
            float currentHeight = groundY - 80f;

            for (int i = 0; i < terrainSegments; i++)
            {
                currentHeight += rand.Next(-18, 19);           // Make small hills
                if (currentHeight > groundY - 30f) currentHeight = groundY - 30f;
                if (currentHeight < groundY - 180f) currentHeight = groundY - 180f;
                terrainHeight[i] = currentHeight;
            }

            // players
            Cannon player1 = new Cannon(new Vector2(180f, 0), new Color(220, 40, 40, 255));   // Red - left side
            Cannon player2 = new Cannon(new Vector2(820f, 0), new Color(40, 140, 255, 255));  // Blue - right side

            // Place cannons on the terrain
            int p1Segment = 8;
            player1.Position = new Vector2(p1Segment * segmentWidth + segmentWidth / 2, terrainHeight[p1Segment] - 12);

            int p2Segment = terrainSegments - 9;
            player2.Position = new Vector2(p2Segment * segmentWidth + segmentWidth / 2, terrainHeight[p2Segment] - 12);

            // ammos
            List<AmmoType> ammoTypes = new List<AmmoType>
            {
                new AmmoType("Normal Shell", 520f, 28f, new Color(255, 240, 100, 255)),
                new AmmoType("Heavy Bomb",  380f, 48f, new Color(255, 80, 80, 255)),
                new AmmoType("Fast Rocket", 680f, 18f, new Color(120, 255, 120, 255))
            };

            int selectedAmmoIndex = 0;

            // Game state 
            Bullet currentBullet = null;
            int currentPlayer = 1;           // 1P, P2
            bool isShooting = false;
            float power = 380f;              // Current shot power
            string message = "";
            bool gameOver = false;

            // Game loop
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                // Update
                if (!gameOver)
                {
                    Cannon activeCannon = (currentPlayer == 1) ? player1 : player2;

                    // Rotate cannon with arrow keys
                    if (Raylib.IsKeyDown(KeyboardKey.Left)) activeCannon.Angle -= 60f * dt;
                    if (Raylib.IsKeyDown(KeyboardKey.Right)) activeCannon.Angle += 60f * dt;

                    // Keep angle between -90 and 0 degrees
                    if (activeCannon.Angle < -89f) activeCannon.Angle = -89f;
                    if (activeCannon.Angle > -1f) activeCannon.Angle = -1f;

                    // Change power with Up/Down
                    if (Raylib.IsKeyDown(KeyboardKey.Up)) power += 180f * dt;
                    if (Raylib.IsKeyDown(KeyboardKey.Down)) power -= 180f * dt;
                    if (power < 200f) power = 200f;
                    if (power > 800f) power = 800f;

                    // Change ammo type with 1, 2, 3 keys
                    if (Raylib.IsKeyPressed(KeyboardKey.One)) selectedAmmoIndex = 0;
                    if (Raylib.IsKeyPressed(KeyboardKey.Two)) selectedAmmoIndex = 1;
                    if (Raylib.IsKeyPressed(KeyboardKey.Three)) selectedAmmoIndex = 2;

                    // Shoot
                    if (Raylib.IsKeyPressed(KeyboardKey.Space) && currentBullet == null)
                    {
                        AmmoType selectedAmmo = ammoTypes[selectedAmmoIndex];

                        float rad = activeCannon.Angle * (MathF.PI / 180f);
                        Vector2 direction = new Vector2(MathF.Cos(rad), MathF.Sin(rad));

                        Vector2 barrelTip = activeCannon.Position + direction * 42f;

                        currentBullet = new Bullet(barrelTip, direction, power, selectedAmmo);
                        isShooting = true;
                    }

                    // Update flying bullet
                    if (currentBullet != null && currentBullet.IsActive)
                    {
                        currentBullet.Update(dt);

                        // Check screen edges
                        if (currentBullet.Position.X < 0 || currentBullet.Position.X > 1000 ||
                            currentBullet.Position.Y > 600)
                        {
                            currentBullet.IsActive = false;
                        }

                        // Check terrain collision
                        int bulletSegment = (int)(currentBullet.Position.X / segmentWidth);
                        if (bulletSegment >= 0 && bulletSegment < terrainSegments)
                        {
                            if (currentBullet.Position.Y > terrainHeight[bulletSegment])
                            {
                                // Explosion
                                ExplodeTerrain(terrainHeight, currentBullet.Position, currentBullet.Type.ExplosionRadius, segmentWidth);

                                // Check if we hit a cannon
                                CheckCannonHit(player1, currentBullet);
                                CheckCannonHit(player2, currentBullet);

                                currentBullet.IsActive = false;
                            }
                        }
                    }

                    // When bullet stops, switch turn
                    if (currentBullet != null && !currentBullet.IsActive)
                    {
                        currentBullet = null;
                        isShooting = false;
                        currentPlayer = (currentPlayer == 1) ? 2 : 1;
                    }

                    // Check if someone won
                    if (player1.Health <= 0 || player2.Health <= 0)
                    {
                        gameOver = true;
                        message = player1.Health > 0 ? "RED PLAYER WINS!" : "BLUE PLAYER WINS!";
                    }
                }

                // dRAW
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(10, 20, 50, 255));   // Dark sky

                // Draw terrain
                for (int i = 0; i < terrainSegments; i++)
                {
                    float x = i * segmentWidth;
                    Raylib.DrawRectangle((int)x, (int)terrainHeight[i], (int)segmentWidth + 1, (int)(600 - terrainHeight[i]), new Color(120, 100, 60, 255));
                }

                // Draw cannons
                player1.Draw();
                player2.Draw();

                // Draw current bullet
                if (currentBullet != null && currentBullet.IsActive)
                {
                    Raylib.DrawCircleV(currentBullet.Position, 9f, currentBullet.Type.Color);
                }

                // HUD 
                // Current player info
                string turnText = currentPlayer == 1 ? "RED PLAYER'S TURN" : "BLUE PLAYER'S TURN";
                Raylib.DrawText(turnText, 20, 20, 28, currentPlayer == 1 ? new Color(255, 60, 60, 255) : new Color(80, 200, 255, 255));

                // Selected ammo
                AmmoType currentAmmo = ammoTypes[selectedAmmoIndex];
                Raylib.DrawText($"Ammo: {currentAmmo.Name}  (Power: {(int)power})", 20, 55, 22, new Color(255, 255, 180, 255));

                // Health
                Raylib.DrawText($"RED: {player1.Health} HP", 720, 20, 24, new Color(255, 80, 80, 255));
                Raylib.DrawText($"BLUE: {player2.Health} HP", 720, 50, 24, new Color(80, 180, 255, 255));

                // Instructions when not shooting
                if (!isShooting && !gameOver)
                {
                    Raylib.DrawText("LEFT/RIGHT = aim   UP/DOWN = power   SPACE = FIRE", 180, 560, 18, new Color(255, 255, 255, 140));
                    Raylib.DrawText("1, 2, 3 = change ammo", 340, 535, 18, new Color(255, 255, 255, 140));
                }

                if (gameOver)
                {
                    Raylib.DrawText(message, 280, 220, 58, new Color(255, 240, 80, 255));
                    Raylib.DrawText("Press R to play again", 340, 300, 26, new Color(255, 255, 255, 180));
                }

                Raylib.EndDrawing();

                // Restart
                if (gameOver && Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    // Reset everything
                    player1.Health = 100;
                    player2.Health = 100;
                    currentPlayer = 1;
                    currentBullet = null;
                    gameOver = false;
                    message = "";
                    // Reset terrain heights
                    for (int i = 0; i < terrainSegments; i++)
                        terrainHeight[i] = groundY - 80f + rand.Next(-30, 31);
                    player1.Position.Y = terrainHeight[p1Segment] - 12;
                    player2.Position.Y = terrainHeight[p2Segment] - 12;
                }
            }

            Raylib.CloseWindow();
        }

        // destroy terrain around explosion point
        static void ExplodeTerrain(float[] terrain, Vector2 impact, float radius, float segmentWidth)
        {
            int centerSegment = (int)(impact.X / segmentWidth);
            int range = (int)(radius / segmentWidth) + 2;

            for (int i = Math.Max(0, centerSegment - range); i < Math.Min(terrain.Length, centerSegment + range); i++)
            {
                float dist = Math.Abs(i * segmentWidth + segmentWidth / 2 - impact.X);
                if (dist < radius)
                {
                    float damage = (radius - dist) * 1.8f;
                    terrain[i] += damage;
                    if (terrain[i] > 580f) terrain[i] = 580f;   // Don't go below ground
                }
            }
        }

        // check if bullet hit a cannon
        static void CheckCannonHit(Cannon cannon, Bullet bullet)
        {
            Rectangle cannonRect = new Rectangle(cannon.Position.X - 25, cannon.Position.Y - 25, 50, 40);
            if (Raylib.CheckCollisionCircleRec(bullet.Position, bullet.Type.ExplosionRadius, cannonRect))
            {
                cannon.Health -= 45;
                if (cannon.Health < 0) cannon.Health = 0;
            }
        }
    }
}