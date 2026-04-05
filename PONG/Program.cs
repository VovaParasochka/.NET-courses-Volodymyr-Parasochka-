using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace Pong
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Raylib.InitWindow(800, 600, "Pong");

            float paddleWidth = 20f;
            float paddleHeight = 120f;
            float paddleSpeed = 450f;

            // Player 1 
            Vector2 paddle1Pos = new Vector2(50f, Raylib.GetScreenHeight() / 2f - paddleHeight / 2f);
            int score1 = 0;
            int score1X = 200;
            int scoreY = 40;

            // Player 2
            Vector2 paddle2Pos = new Vector2(Raylib.GetScreenWidth() - 50f - paddleWidth,
                                           Raylib.GetScreenHeight() / 2f - paddleHeight / 2f);
            int score2 = 0;
            int score2X = 600;

            // Ball
            Vector2 ballPos = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            Vector2 ballDirection = new Vector2(1f, 0.4f);
            float ballSpeed = 420f;

            // Game loop 
            while (!Raylib.WindowShouldClose())
            {
                if (Raylib.IsKeyDown(KeyboardKey.W)) paddle1Pos.Y -= paddleSpeed * Raylib.GetFrameTime();
                if (Raylib.IsKeyDown(KeyboardKey.S)) paddle1Pos.Y += paddleSpeed * Raylib.GetFrameTime();

                if (Raylib.IsKeyDown(KeyboardKey.Up)) paddle2Pos.Y -= paddleSpeed * Raylib.GetFrameTime();
                if (Raylib.IsKeyDown(KeyboardKey.Down)) paddle2Pos.Y += paddleSpeed * Raylib.GetFrameTime();

                // Keep paddles inside screen
                if (paddle1Pos.Y < 0) paddle1Pos.Y = 0;
                if (paddle1Pos.Y > Raylib.GetScreenHeight() - paddleHeight)
                    paddle1Pos.Y = Raylib.GetScreenHeight() - paddleHeight;

                if (paddle2Pos.Y < 0) paddle2Pos.Y = 0;
                if (paddle2Pos.Y > Raylib.GetScreenHeight() - paddleHeight)
                    paddle2Pos.Y = Raylib.GetScreenHeight() - paddleHeight;

                ballPos += ballDirection * ballSpeed * Raylib.GetFrameTime();

                //colIision
                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                // Top and bottom walls
                if (ballPos.Y < 0)
                {
                    ballPos.Y = 0;
                    ballDirection.Y *= -1f;
                }
                if (ballPos.Y > screenH)
                {
                    ballPos.Y = screenH;
                    ballDirection.Y *= -1f;
                }

                // Paddle 1 collision
                if (ballPos.X >= paddle1Pos.X &&
                    ballPos.X <= paddle1Pos.X + paddleWidth &&
                    ballPos.Y >= paddle1Pos.Y &&
                    ballPos.Y <= paddle1Pos.Y + paddleHeight)
                {
                    ballDirection.X *= -1f;
                    ballPos.X = paddle1Pos.X + paddleWidth + 2f;
                }

                // Paddle 2 collision
                if (ballPos.X >= paddle2Pos.X &&
                    ballPos.X <= paddle2Pos.X + paddleWidth &&
                    ballPos.Y >= paddle2Pos.Y &&
                    ballPos.Y <= paddle2Pos.Y + paddleHeight)
                {
                    ballDirection.X *= -1f;
                    ballPos.X = paddle2Pos.X - 2f;
                }

                // Scoring
                if (ballPos.X < 0)
                {
                    score2++;
                    ballPos = new Vector2(screenW / 2f, screenH / 2f);
                    ballDirection = new Vector2(1f, 0.4f);
                }
                else if (ballPos.X > screenW)
                {
                    score1++;
                    ballPos = new Vector2(screenW / 2f, screenH / 2f);
                    ballDirection = new Vector2(-1f, -0.4f);
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(15, 23, 42, 255));

                // Middle net
                Raylib.DrawLine(screenW / 2, 0, screenW / 2, screenH, new Color(255, 255, 255, 80));

                // Paddles
                Raylib.DrawRectangleV(paddle1Pos, new Vector2(paddleWidth, paddleHeight),
                                    new Color(0, 120, 255, 255));     // blue
                Raylib.DrawRectangleV(paddle2Pos, new Vector2(paddleWidth, paddleHeight),
                                    new Color(255, 165, 0, 255));     // orange

                // Ball
                Raylib.DrawCircleV(ballPos, 12f, new Color(255, 255, 255, 255));

                // Scores 
                Raylib.DrawText(score1.ToString(), score1X, scoreY, 80, new Color(255, 255, 255, 255));
                Raylib.DrawText(score2.ToString(), score2X, scoreY, 80, new Color(255, 255, 255, 255));

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}