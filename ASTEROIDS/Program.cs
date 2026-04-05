using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace Asteroids
{
    internal class Player
    {
        public Vector2 Position;      // Where the ship is on the screen
        public Vector2 Velocity;      // Current speed and direction 
        public float Rotation;        // Which way the ship is pointing 
        public bool Thrusting;        // Are the engines firing right now?

        private readonly float speedLimit = 380f;   // Maximum speed the ship can go
        private readonly float thrustPower = 420f;  // How strong the engines are
        private readonly float rotationSpeed = 180f;// How fast you can turn

        public Player(Vector2 startPos)
        {
            Position = startPos;
            Velocity = new Vector2(0, 0);
            Rotation = -90f;           // Start pointing up, like the original Asteroids
            Thrusting = false;
        }

        // This runs every frame and handles all player movement
        public void Update(float dt)
        {
            // Rotate left or right with arrow keys
            if (Raylib.IsKeyDown(KeyboardKey.Left)) Rotation -= rotationSpeed * dt;
            if (Raylib.IsKeyDown(KeyboardKey.Right)) Rotation += rotationSpeed * dt;

            // Thrust forward when Up arrow is held
            Thrusting = Raylib.IsKeyDown(KeyboardKey.Up);

            if (Thrusting)
            {
                float rad = Rotation * (MathF.PI / 180f);
                Vector2 thrustVector = new Vector2(MathF.Cos(rad), MathF.Sin(rad));
                Velocity += thrustVector * thrustPower * dt;
            }

            // Limit top speed so you don't go too fast
            float len = Velocity.Length();
            if (len > speedLimit && len > 0)
            {
                Velocity = (Velocity / len) * speedLimit;
            }

            // Actually move the ship
            Position += Velocity * dt;

            // Make the ship wrap around the screen edges
            WrapAroundScreen();
        }

        private void WrapAroundScreen()
        {
            int w = Raylib.GetScreenWidth();
            int h = Raylib.GetScreenHeight();

            if (Position.X < -20) Position.X = w + 20;
            if (Position.X > w + 20) Position.X = -20;
            if (Position.Y < -20) Position.Y = h + 20;
            if (Position.Y > h + 20) Position.Y = -20;
        }

        public void Draw()
        {
            float rad = Rotation * (MathF.PI / 180f);

            // Draw the ship as a triangle pointing in the rotation direction
            Vector2 tip = Position + new Vector2(MathF.Cos(rad) * 18, MathF.Sin(rad) * 18);
            Vector2 left = Position + new Vector2(MathF.Cos(rad + 2.4f) * 14, MathF.Sin(rad + 2.4f) * 14);
            Vector2 right = Position + new Vector2(MathF.Cos(rad - 2.4f) * 14, MathF.Sin(rad - 2.4f) * 14);

            Raylib.DrawTriangle(tip, left, right, new Color(220, 240, 255, 255));

            // Draw the thrust flame when engines are on
            if (Thrusting)
            {
                Vector2 flameBase1 = Position + new Vector2(MathF.Cos(rad + 2.6f) * 9, MathF.Sin(rad + 2.6f) * 9);
                Vector2 flameBase2 = Position + new Vector2(MathF.Cos(rad - 2.6f) * 9, MathF.Sin(rad - 2.6f) * 9);
                Vector2 flameTip = Position + new Vector2(MathF.Cos(rad + MathF.PI) * 22, MathF.Sin(rad + MathF.PI) * 22);

                Raylib.DrawTriangle(flameBase1, flameBase2, flameTip, new Color(255, 160, 40, 255));
            }
        }
    }

    internal class Asteroid
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;           // 3 = large, 2 = medium, 1 = small
        public bool IsAlive = true;

        public Asteroid(Vector2 pos, Vector2 vel, float size)
        {
            Position = pos;
            Velocity = vel;
            Size = size;
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            WrapAroundScreen();
        }

        private void WrapAroundScreen()
        {
            int w = Raylib.GetScreenWidth();
            int h = Raylib.GetScreenHeight();

            if (Position.X < -30) Position.X = w + 30;
            if (Position.X > w + 30) Position.X = -30;
            if (Position.Y < -30) Position.Y = h + 30;
            if (Position.Y > h + 30) Position.Y = -30;
        }

        public void Draw()
        {
            float radius = Size * 18f;
            Raylib.DrawCircleV(Position, radius, new Color(200, 200, 220, 255));

            // Draw jagged edges so it looks like a real asteroid
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (MathF.PI * 2f / 8);
                Vector2 p1 = Position + new Vector2(MathF.Cos(angle) * radius * 0.9f, MathF.Sin(angle) * radius * 0.9f);
                Vector2 p2 = Position + new Vector2(MathF.Cos(angle + 0.8f) * radius * 1.15f, MathF.Sin(angle + 0.8f) * radius * 1.15f);
                Raylib.DrawLineV(p1, p2, new Color(220, 220, 240, 255));
            }
        }
    }

    internal class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsActive = true;

        public Bullet(Vector2 pos, Vector2 dir)
        {
            Position = pos;
            Velocity = dir * 620f;   // Player bullets are quite fast
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            WrapAroundScreen();
        }

        private void WrapAroundScreen()
        {
            int w = Raylib.GetScreenWidth();
            int h = Raylib.GetScreenHeight();
            if (Position.X < -10) Position.X = w + 10;
            if (Position.X > w + 10) Position.X = -10;
            if (Position.Y < -10) Position.Y = h + 10;
            if (Position.Y > h + 10) Position.Y = -10;
        }

        public void Draw()
        {
            Raylib.DrawCircleV(Position, 4f, new Color(255, 240, 100, 255));
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // setup
            Raylib.InitWindow(900, 700, "Asteroids");
            Raylib.SetTargetFPS(60);

            // Player
            Player player = new Player(new Vector2(450, 350));

            // list 
            List<Asteroid> asteroids = new List<Asteroid>();
            List<Bullet> bullets = new List<Bullet>();

            int score = 0;
            int level = 1;
            bool gameOver = false;
            string message = "";

            // Start the first level
            CreateLevel(asteroids, level);

            //Game loop 
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();

                if (!gameOver)
                {
                    // Update player movement and shooting
                    player.Update(dt);

                    // Shoot when player presses SPACE
                    if (Raylib.IsKeyPressed(KeyboardKey.Space))
                    {
                        float rad = player.Rotation * (MathF.PI / 180f);
                        Vector2 dir = new Vector2(MathF.Cos(rad), MathF.Sin(rad));
                        Vector2 spawnPos = player.Position + dir * 22f;
                        bullets.Add(new Bullet(spawnPos, dir));
                    }

                    // Update all bullets
                    for (int i = bullets.Count - 1; i >= 0; i--)
                    {
                        bullets[i].Update(dt);
                        if (!bullets[i].IsActive) bullets.RemoveAt(i);
                    }

                    // Update all asteroids
                    foreach (var ast in asteroids)
                        ast.Update(dt);

                    // Check for collisions 
                    CheckCollisions(player, asteroids, bullets, ref score);

                    // Start next level when all asteroids are destroyed
                    if (asteroids.Count == 0)
                    {
                        level++;
                        CreateLevel(asteroids, level);
                        player.Position = new Vector2(450, 350);
                        player.Velocity = new Vector2(0, 0);
                    }

                    // Check if player was hit
                    if (PlayerHitByAsteroidOrBullet(player, asteroids, bullets))
                    {
                        gameOver = true;
                        message = "GAME OVER - Press R to restart";
                    }
                }

                // Drawing
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(5, 5, 25, 255));

                // Background stars
                for (int i = 0; i < 80; i++)
                {
                    int x = (i * 23) % 900;
                    int y = (i * 17) % 700;
                    Raylib.DrawPixel(x, y, new Color(255, 255, 255, 90));
                }

                player.Draw();
                foreach (var ast in asteroids) ast.Draw();
                foreach (var b in bullets) b.Draw();

                // HUD
                Raylib.DrawText($"SCORE {score:0000}", 30, 20, 28, new Color(255, 255, 200, 255));
                Raylib.DrawText($"LEVEL {level}", 30, 55, 24, new Color(180, 255, 255, 255));

                if (gameOver)
                {
                    Raylib.DrawText(message, 220, 280, 48, new Color(255, 80, 80, 255));
                }

                Raylib.EndDrawing();

                // Restart the game
                if (gameOver && Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    player = new Player(new Vector2(450, 350));
                    asteroids.Clear();
                    bullets.Clear();
                    score = 0;
                    level = 1;
                    gameOver = false;
                    CreateLevel(asteroids, level);
                }
            }

            Raylib.CloseWindow();
        }

        // Creates a new level with more asteroids
        static void CreateLevel(List<Asteroid> asteroids, int level)
        {
            asteroids.Clear();
            int count = 4 + level * 2;
            Random rand = new Random();

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new Vector2(rand.Next(100, 800), rand.Next(100, 500));
                Vector2 vel = new Vector2((float)(rand.NextDouble() * 140 - 70), (float)(rand.NextDouble() * 140 - 70));
                float size = rand.Next(2, 4);   // 3 = large, 2 = medium, 1 = small
                asteroids.Add(new Asteroid(pos, vel, size));
            }
        }

        // Handles all collisions in the game
        static void CheckCollisions(Player player, List<Asteroid> asteroids, List<Bullet> bullets, ref int score)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                Bullet b = bullets[i];
                for (int j = asteroids.Count - 1; j >= 0; j--)
                {
                    Asteroid a = asteroids[j];
                    if (Raylib.CheckCollisionCircles(b.Position, 6f, a.Position, a.Size * 18f))
                    {
                        b.IsActive = false;

                        // Break big asteroids into smaller ones
                        if (a.Size > 1)
                        {
                            float newSize = a.Size - 1;
                            Vector2 v1 = new Vector2(-a.Velocity.Y, a.Velocity.X) * 0.8f;
                            Vector2 v2 = new Vector2(a.Velocity.Y, -a.Velocity.X) * 0.8f;

                            asteroids.Add(new Asteroid(a.Position, v1, newSize));
                            asteroids.Add(new Asteroid(a.Position, v2, newSize));
                        }

                        asteroids.RemoveAt(j);
                        score += (int)(a.Size * 100);
                        break;
                    }
                }
            }

            // Player crashing into asteroids
            for (int j = asteroids.Count - 1; j >= 0; j--)
            {
                if (Raylib.CheckCollisionCircles(player.Position, 18f, asteroids[j].Position, asteroids[j].Size * 18f))
                {
                    asteroids.RemoveAt(j);
                    return;
                }
            }
        }

        static bool PlayerHitByAsteroidOrBullet(Player player, List<Asteroid> asteroids, List<Bullet> bullets)
        {
            foreach (var a in asteroids)
                if (Raylib.CheckCollisionCircles(player.Position, 18f, a.Position, a.Size * 18f))
                    return true;

            return false;
        }
    }
}