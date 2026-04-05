using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace Tanks
{
    internal class Program
    {
        static void DrawTank(Vector2 position, Vector2 tankSize, Vector2 direction, Vector2 turretSize, Color color)
        {
            // Tank hull 
            Vector2 hullTopLeft = position - tankSize / 2.0f;
            Raylib.DrawRectangleV(hullTopLeft, tankSize, color);

            // Tank turret 
            Vector2 turretOffset = direction * (tankSize.X / 2.0f + turretSize.X / 2.0f);
            Vector2 turretPos = position + turretOffset;
            Vector2 turretTopLeft = turretPos - turretSize / 2.0f;
            Raylib.DrawRectangleV(turretTopLeft, turretSize, new Color(0, 0, 0, 255));   
        }

        static void Main(string[] args)
        {
            Raylib.InitWindow(800, 600, "Tanks!");

            // Game const
            float tankSizeValue = 48f;
            Vector2 tankSize = new Vector2(tankSizeValue, tankSizeValue);
            Vector2 turretSize = new Vector2(32f, 12f);
            float tankSpeed = 220f;
            float bulletSpeed = 580f;
            float shootInterval = 0.8f;

            // P1 col-pos 
            Vector2 p1StartPos = new Vector2(140f, 160f);
            Vector2 p1Pos = p1StartPos;
            Vector2 p1Dir = new Vector2(1f, 0f);
            float p1LastShootTime = 0f;
            int p1Score = 0;

            // P2 col-pos
            Vector2 p2StartPos = new Vector2(660f, 160f);
            Vector2 p2Pos = p2StartPos;
            Vector2 p2Dir = new Vector2(-1f, 0f);
            float p2LastShootTime = 0f;
            int p2Score = 0;

            // Bullets 
            Vector2 bullet1Pos = new Vector2(0, 0);
            Vector2 bullet1Dir = new Vector2(0, 0);
            bool bullet1Active = false;

            Vector2 bullet2Pos = new Vector2(0, 0);
            Vector2 bullet2Dir = new Vector2(0, 0);
            bool bullet2Active = false;

            // Walls 
            List<Rectangle> walls = new List<Rectangle>();
            walls.Add(new Rectangle(340, 80, 35, 260));
            walls.Add(new Rectangle(40, 460, 240, 35));
            walls.Add(new Rectangle(520, 460, 240, 35));

            // Gmae loop 
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();
                float currentTime = (float)Raylib.GetTime();

                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                // P1 controls 
                Vector2 oldP1Pos = p1Pos;

                if (Raylib.IsKeyDown(KeyboardKey.D)) p1Dir = new Vector2(1f, 0f);
                if (Raylib.IsKeyDown(KeyboardKey.A)) p1Dir = new Vector2(-1f, 0f);
                if (Raylib.IsKeyDown(KeyboardKey.W)) p1Dir = new Vector2(0f, -1f);
                if (Raylib.IsKeyDown(KeyboardKey.S)) p1Dir = new Vector2(0f, 1f);

                p1Pos += p1Dir * tankSpeed * dt;

                if (Raylib.IsKeyPressed(KeyboardKey.Space) &&
                    currentTime - p1LastShootTime > shootInterval &&
                    !bullet1Active)
                {
                    bullet1Pos = p1Pos + p1Dir * (tankSizeValue / 2f + 10f);
                    bullet1Dir = p1Dir;
                    bullet1Active = true;
                    p1LastShootTime = currentTime;
                }

                // P2 u´pdate 
                Vector2 oldP2Pos = p2Pos;

                if (Raylib.IsKeyDown(KeyboardKey.Right)) p2Dir = new Vector2(1f, 0f);
                if (Raylib.IsKeyDown(KeyboardKey.Left)) p2Dir = new Vector2(-1f, 0f);
                if (Raylib.IsKeyDown(KeyboardKey.Up)) p2Dir = new Vector2(0f, -1f);
                if (Raylib.IsKeyDown(KeyboardKey.Down)) p2Dir = new Vector2(0f, 1f);

                p2Pos += p2Dir * tankSpeed * dt;

                if (Raylib.IsKeyPressed(KeyboardKey.Enter) &&
                    currentTime - p2LastShootTime > shootInterval &&
                    !bullet2Active)
                {
                    bullet2Pos = p2Pos + p2Dir * (tankSizeValue / 2f + 10f);
                    bullet2Dir = p2Dir;
                    bullet2Active = true;
                    p2LastShootTime = currentTime;
                }

                // Bullet update 
                if (bullet1Active)
                {
                    bullet1Pos += bullet1Dir * bulletSpeed * dt;
                    if (bullet1Pos.X < 0 || bullet1Pos.X > screenW || bullet1Pos.Y < 0 || bullet1Pos.Y > screenH)
                        bullet1Active = false;
                }

                if (bullet2Active)
                {
                    bullet2Pos += bullet2Dir * bulletSpeed * dt;
                    if (bullet2Pos.X < 0 || bullet2Pos.X > screenW || bullet2Pos.Y < 0 || bullet2Pos.Y > screenH)
                        bullet2Active = false;
                }

                // collision tanks 
                Rectangle p1Rect = new Rectangle(p1Pos.X - tankSizeValue / 2f, p1Pos.Y - tankSizeValue / 2f, tankSizeValue, tankSizeValue);
                Rectangle p2Rect = new Rectangle(p2Pos.X - tankSizeValue / 2f, p2Pos.Y - tankSizeValue / 2f, tankSizeValue, tankSizeValue);

                bool p1Collided = false;
                foreach (var wall in walls)
                    if (Raylib.CheckCollisionRecs(p1Rect, wall)) p1Collided = true;
                if (Raylib.CheckCollisionRecs(p1Rect, p2Rect)) p1Collided = true;
                if (p1Collided) p1Pos = oldP1Pos;

                bool p2Collided = false;
                foreach (var wall in walls)
                    if (Raylib.CheckCollisionRecs(p2Rect, wall)) p2Collided = true;
                if (Raylib.CheckCollisionRecs(p2Rect, p1Rect)) p2Collided = true;
                if (p2Collided) p2Pos = oldP2Pos;

                // Collision wall-bullet 
                if (bullet1Active)
                {
                    foreach (var wall in walls)
                    {
                        if (Raylib.CheckCollisionCircleRec(bullet1Pos, 8f, wall))
                        {
                            bullet1Active = false;
                            break;
                        }
                    }
                }

                if (bullet2Active)
                {
                    foreach (var wall in walls)
                    {
                        if (Raylib.CheckCollisionCircleRec(bullet2Pos, 8f, wall))
                        {
                            bullet2Active = false;
                            break;
                        }
                    }
                }

                // Collision bullet-tanks 
                if (bullet1Active && Raylib.CheckCollisionCircleRec(bullet1Pos, 8f, p2Rect))
                {
                    p1Score++;
                    bullet1Active = false;
                    p1Pos = p1StartPos;
                    p2Pos = p2StartPos;
                    p1Dir = new Vector2(1f, 0f);
                    p2Dir = new Vector2(-1f, 0f);
                }

                if (bullet2Active && Raylib.CheckCollisionCircleRec(bullet2Pos, 8f, p1Rect))
                {
                    p2Score++;
                    bullet2Active = false;
                    p1Pos = p1StartPos;
                    p2Pos = p2StartPos;
                    p1Dir = new Vector2(1f, 0f);
                    p2Dir = new Vector2(-1f, 0f);
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(0, 110, 0, 255));

                // Walls
                foreach (var wall in walls)
                    Raylib.DrawRectangleRec(wall, new Color(140, 140, 140, 255));

                // Tanks
                DrawTank(p1Pos, tankSize, p1Dir, turretSize, new Color(220, 20, 20, 255));   // Red P1
                DrawTank(p2Pos, tankSize, p2Dir, turretSize, new Color(0, 220, 220, 255));   // Cyan P2

                // Bullets
                if (bullet1Active)
                    Raylib.DrawCircleV(bullet1Pos, 8f, new Color(255, 240, 0, 255));
                if (bullet2Active)
                    Raylib.DrawCircleV(bullet2Pos, 8f, new Color(255, 240, 0, 255));

                // Scores
                Raylib.DrawText($"P1 Score: {p1Score}", 30, 20, 32, new Color(255, 30, 30, 255));
                Raylib.DrawText($"P2 Score: {p2Score}", 510, 20, 32, new Color(0, 255, 255, 255));

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}