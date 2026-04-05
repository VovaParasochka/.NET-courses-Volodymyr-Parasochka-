using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace LunarLander
{
    // SHIP CLASS - This handles everything about the player's spaceship
    internal class Ship
    {
        public Vector2 Position;        // Where the ship is on the screen
        public Vector2 Velocity;        // How fast it's moving 
        public float Fuel;              // How much fuel is left 
        public bool MainThrustOn;       
        public bool LeftThrustOn;       
        public bool RightThrustOn;      

        private readonly float shipSize = 14f;   // Makes the ship look nice and visible

        public Ship(Vector2 startPos)
        {
            Position = startPos;
            Velocity = new Vector2(0, 0);
            Fuel = 1000f;
            MainThrustOn = false;
            LeftThrustOn = false;
            RightThrustOn = false;
        }

        // This runs every frame and updates the ship's physics
        public void Update(float dt, float gravity, float mainThrustPower, float sideThrustPower, float fuelConsumption)
        {
            // Gravity always pulls the ship down
            Velocity.Y += gravity * dt;

            // Main engine 
            if (MainThrustOn && Fuel > 0)
            {
                Velocity.Y -= mainThrustPower * dt;     // Push the ship upward
                Fuel -= fuelConsumption * dt;
            }

            // Left and right side thrusters
            if (LeftThrustOn && Fuel > 0)
            {
                Velocity.X -= sideThrustPower * dt;
                Fuel -= fuelConsumption * 0.6f * dt;    // Side thrusters use a bit less fuel
            }
            if (RightThrustOn && Fuel > 0)
            {
                Velocity.X += sideThrustPower * dt;
                Fuel -= fuelConsumption * 0.6f * dt;
            }

            if (Fuel < 0) Fuel = 0;

            // Actually move the ship based on its current speed
            Position += Velocity * dt;
        }

        // Draws the ship and its flames 
        public void Draw()
        {
            // Main ship body- a cool triangle pointing upward
            Vector2 left = Position - new Vector2(shipSize, 0);
            Vector2 right = Position + new Vector2(shipSize, 0);
            Vector2 nose = Position - new Vector2(0, shipSize * 2.4f);

            Raylib.DrawTriangle(left, right, nose, new Color(120, 200, 255, 255));

            // Big main flame when thrusting up
            if (MainThrustOn && Fuel > 0)
            {
                Vector2 flameLeft = Position + new Vector2(-shipSize * 0.55f, shipSize * 0.8f);
                Vector2 flameRight = Position + new Vector2(shipSize * 0.55f, shipSize * 0.8f);
                Vector2 flameTip = Position + new Vector2(0, shipSize * 3.2f);

                Raylib.DrawTriangle(flameLeft, flameRight, flameTip, new Color(255, 160, 0, 255));
            }

            // Small side flames for left/right thrusters
            if (LeftThrustOn && Fuel > 0)
            {
                Vector2 basePos = Position + new Vector2(-shipSize * 0.9f, 4);
                Raylib.DrawTriangle(basePos, new Vector2(basePos.X, basePos.Y + 6),
                                  new Vector2(basePos.X - 12, basePos.Y + 2), new Color(255, 200, 80, 255));
            }
            if (RightThrustOn && Fuel > 0)
            {
                Vector2 basePos = Position + new Vector2(shipSize * 0.9f, 4);
                Raylib.DrawTriangle(basePos, new Vector2(basePos.X, basePos.Y + 6),
                                  new Vector2(basePos.X + 12, basePos.Y + 2), new Color(255, 200, 80, 255));
            }
        }
    }

    // Main gqm3
    internal class Program
    {
        static void Main(string[] args)
        {
            Raylib.InitWindow(800, 600, "Lunar Lander");
            Raylib.SetTargetFPS(60);

            // Game const
            float gravity = 220f;           // gravity
            float mainThrustPower = 580f;   // ain engine
            float sideThrustPower = 280f;   // side thrusters 
            float fuelConsumption = 32f;

            float groundY = 520f;           // Where the ground is

            // Ship
            Ship ship = new Ship(new Vector2(400f, 120f));

            float gameTime = 0f;
            int score = 0;
            bool gameOver = false;
            bool victory = false;
            string message = "";

            // Background stars
            List<Vector2> stars = new List<Vector2>();
            Random rand = new Random();
            for (int i = 0; i < 120; i++)
            {
                stars.Add(new Vector2(rand.Next(0, 800), rand.Next(0, 480)));
            }

            // Game loop
            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();
                gameTime += dt;

                // Game logic
                if (!gameOver)
                {
                    // Arrow keys control the ship 
                    ship.MainThrustOn = Raylib.IsKeyDown(KeyboardKey.Up);
                    ship.LeftThrustOn = Raylib.IsKeyDown(KeyboardKey.Left);
                    ship.RightThrustOn = Raylib.IsKeyDown(KeyboardKey.Right);

                    ship.Update(dt, gravity, mainThrustPower, sideThrustPower, fuelConsumption);

                    // Check if the ship has reached the ground
                    if (ship.Position.Y + 28f > groundY)
                    {
                        float verticalSpeed = Math.Abs(ship.Velocity.Y);
                        bool onPad = ship.Position.X > 280 && ship.Position.X < 520;   // Landing zone

                        if (onPad && verticalSpeed < 48f && Math.Abs(ship.Velocity.X) < 35f)
                        {
                            victory = true;
                            gameOver = true;
                            score += (int)(1000 * (ship.Fuel / 1000f));
                            message = "SAFE LANDING!";
                            ship.Velocity = new Vector2(0, 0);
                            ship.Position.Y = groundY - 28f;
                        }
                        else
                        {
                            victory = false;
                            gameOver = true;
                            message = "CRASHED!";
                            ship.Velocity = new Vector2(0, 0);
                        }
                    }
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(3, 3, 18, 255));

                // Stars in the background
                foreach (var star in stars)
                    Raylib.DrawPixel((int)star.X, (int)star.Y, new Color(255, 255, 255, 160));

                // Jagged mountain terrain 
                Raylib.DrawLine(0, 545, 85, 510, new Color(255, 255, 255, 255));
                Raylib.DrawLine(85, 510, 145, 480, new Color(255, 255, 255, 255));
                Raylib.DrawLine(145, 480, 210, 520, new Color(255, 255, 255, 255));
                Raylib.DrawLine(210, 520, 265, 505, new Color(255, 255, 255, 255));
                Raylib.DrawLine(265, 505, 315, 535, new Color(255, 255, 255, 255));
                Raylib.DrawLine(315, 535, 380, 490, new Color(255, 255, 255, 255));
                Raylib.DrawLine(380, 490, 455, 515, new Color(255, 255, 255, 255));
                Raylib.DrawLine(455, 515, 510, 480, new Color(255, 255, 255, 255));
                Raylib.DrawLine(510, 480, 575, 525, new Color(255, 255, 255, 255));
                Raylib.DrawLine(575, 525, 640, 500, new Color(255, 255, 255, 255));
                Raylib.DrawLine(640, 500, 710, 530, new Color(255, 255, 255, 255));
                Raylib.DrawLine(710, 530, 800, 545, new Color(255, 255, 255, 255));

                // Landing pads with multipliers
                Raylib.DrawRectangle(120, 518, 65, 8, new Color(80, 255, 120, 255));
                Raylib.DrawText("2X", 138, 498, 18, new Color(80, 255, 120, 255));

                Raylib.DrawRectangle(355, 518, 85, 8, new Color(255, 220, 60, 255));
                Raylib.DrawText("3X", 378, 498, 18, new Color(255, 220, 60, 255));

                Raylib.DrawRectangle(645, 518, 65, 8, new Color(80, 255, 120, 255));
                Raylib.DrawText("2X", 663, 498, 18, new Color(80, 255, 120, 255));

                // Draw the ship
                ship.Draw();

                // Hud
                Raylib.DrawText($"SCORE {score:0000}", 25, 18, 22, new Color(255, 255, 255, 255));
                Raylib.DrawText($"TIME {(int)gameTime / 60}:{(int)gameTime % 60:00}", 25, 42, 22, new Color(255, 255, 255, 255));
                Raylib.DrawText($"FUEL {(int)ship.Fuel:0000}", 25, 66, 22, new Color(255, 255, 255, 255));

                Raylib.DrawText("ALTITUDE", 580, 18, 18, new Color(255, 255, 255, 255));
                Raylib.DrawText($"{(int)(groundY - ship.Position.Y):0000}", 715, 18, 22, new Color(255, 255, 255, 255));

                Raylib.DrawText("HORIZONTAL SPEED", 580, 42, 18, new Color(255, 255, 255, 255));
                Raylib.DrawText($"{(int)Math.Abs(ship.Velocity.X):000}", 750, 42, 22, new Color(255, 255, 255, 255));

                Raylib.DrawText("VERTICAL SPEED", 580, 66, 18, new Color(255, 255, 255, 255));
                Raylib.DrawText($"{(int)Math.Abs(ship.Velocity.Y):000}", 750, 66, 22, new Color(255, 255, 255, 255));

                // Start screen text
                if (!gameOver)
                {
                    Raylib.DrawText("INSERT COINS", 310, 180, 28, new Color(255, 255, 255, 90));
                    Raylib.DrawText("CLICK TO PLAY", 295, 220, 26, new Color(255, 255, 255, 140));
                    Raylib.DrawText("ARROW KEYS TO MOVE", 255, 255, 22, new Color(255, 255, 255, 120));
                }

                // Game over message
                if (gameOver)
                {
                    Color msgColor = victory ? new Color(0, 255, 140, 255) : new Color(255, 70, 70, 255);
                    Raylib.DrawText(message, 400 - (message.Length * 13), 220, 52, msgColor);
                    Raylib.DrawText("PRESS R TO RESTART", 275, 290, 26, new Color(255, 255, 255, 160));
                }

                Raylib.EndDrawing();

                // Restart the game
                if (gameOver && Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    ship = new Ship(new Vector2(400f, 120f));
                    gameTime = 0f;
                    score = 0;
                    gameOver = false;
                    victory = false;
                }
            }

            Raylib.CloseWindow();
        }
    }
}