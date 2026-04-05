using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace DVD
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Opens a new window 
            Raylib.InitWindow(800, 600, "DVD Logo");

            // text setting
            // The text we want to show and how it should look
            string text = "DVD";
            float fontSize = 80f;                    
            float spacing = 2f;                      
            Font font = Raylib.GetFontDefault();     

            // Measure the exact width and height of the text
            // This is needed so the whole "DVD" bounces correctly (not just the top-left corner)
            Vector2 textSize = Raylib.MeasureTextEx(font, text, fontSize, spacing);

            //  movemant setting
            // Starting position = center of the screen
            Vector2 position = new Vector2(
                (Raylib.GetScreenWidth() - textSize.X) / 2f,
                (Raylib.GetScreenHeight() - textSize.Y) / 2f
            );

            // Direction the text is moving (1,1) = right + down
            Vector2 direction = new Vector2(1f, 1f);

            float speed = 300f;

            // game loop
            // runs every frame until the window is closed
            while (!Raylib.WindowShouldClose())
            {
                // Move the text using direction, speed and frame time (smooth on any PC)
                position += direction * speed * Raylib.GetFrameTime();

                // Bounce ckeck
                // Get current screen size every frame
                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                float textW = textSize.X;
                float textH = textSize.Y;

                // Left wall
                if (position.X < 0)
                {
                    position.X = 0;
                    direction.X *= -1f;
                }

                // Right wall
                if (position.X + textW > screenW)
                {
                    position.X = screenW - textW;
                    direction.X *= -1f;
                }

                // Top wall
                if (position.Y < 0)
                {
                    position.Y = 0;
                    direction.Y *= -1f;
                }

                // Bottom wall
                if (position.Y + textH > screenH)
                {
                    position.Y = screenH - textH;
                    direction.Y *= -1f;
                }

                Raylib.BeginDrawing();

                // Clear screen with black background
                Raylib.ClearBackground(new Color(0, 0, 0, 255));

                // Draw the text "DVD" using the measured size and position
                Raylib.DrawTextEx(
                    font,
                    text,
                    position,
                    fontSize,
                    spacing,
                    new Color(255, 255, 0, 255)
                );

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
    }
}