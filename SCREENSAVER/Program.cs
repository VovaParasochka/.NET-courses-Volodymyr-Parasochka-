using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace SCREENSAVER
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // WINDOW Size
            Raylib.InitWindow(800, 800, "Screensaver");

            // These three Vector2 variables store the corners of the triangle
            Vector2 A = new Vector2(Raylib.GetScreenWidth() / 2f, 40f);           // Top point 
            Vector2 B = new Vector2(40f, Raylib.GetScreenHeight() / 2f);          // Left point
            Vector2 C = new Vector2(Raylib.GetScreenWidth() - 40f, Raylib.GetScreenHeight() * 0.75f); // Bottom-right point

            //  Movement setings
            float speed = 200f;           // How fast the triangle moves 
            float lineThickness = 5f;     // Thickness of the drawn lines 

            // Direction vectors for each corner (tells which way each point is moving)
            Vector2 Amove = new Vector2(1f, 1f);   // A moves right + down
            Vector2 Bmove = new Vector2(1f, -1f);  // B moves right + up
            Vector2 Cmove = new Vector2(-1f, 1f);  // C moves left + down

            //  amin game loop  
            // loop runs every frame until the user close the window
            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();

                // Clear the screen with black background 
                Raylib.ClearBackground(new Color(0, 0, 0, 255));

                // Draw three colored lines connecting the points
                Raylib.DrawLineEx(A, B, lineThickness, new Color(0, 255, 0, 255));     // Green line: A → B
                Raylib.DrawLineEx(B, C, lineThickness, new Color(255, 255, 0, 255));   // Yellow line: B → C
                Raylib.DrawLineEx(C, A, lineThickness, new Color(102, 191, 255, 255)); // Sky blue line: C → A

                // GetFrameTime() makes movement smooth on any computer
                A += Amove * speed * Raylib.GetFrameTime();
                B += Bmove * speed * Raylib.GetFrameTime();
                C += Cmove * speed * Raylib.GetFrameTime();

                // Get current screen size every frame 
                int screenW = Raylib.GetScreenWidth();
                int screenH = Raylib.GetScreenHeight();

                // Point A - bounce when hitting left/right or top/bottom wall
                if (A.X < 0) { A.X = 0; Amove.X *= -1f; }
                else if (A.X > screenW) { A.X = screenW; Amove.X *= -1f; }
                if (A.Y < 0) { A.Y = 0; Amove.Y *= -1f; }
                else if (A.Y > screenH) { A.Y = screenH; Amove.Y *= -1f; }

                // Point B - same bounce logic
                if (B.X < 0) { B.X = 0; Bmove.X *= -1f; }
                else if (B.X > screenW) { B.X = screenW; Bmove.X *= -1f; }
                if (B.Y < 0) { B.Y = 0; Bmove.Y *= -1f; }
                else if (B.Y > screenH) { B.Y = screenH; Bmove.Y *= -1f; }

                // Point C - same bounce logic
                if (C.X < 0) { C.X = 0; Cmove.X *= -1f; }
                else if (C.X > screenW) { C.X = screenW; Cmove.X *= -1f; }
                if (C.Y < 0) { C.Y = 0; Cmove.Y *= -1f; }
                else if (C.Y > screenH) { C.Y = screenH; Cmove.Y *= -1f; }

                // finish drawing
                Raylib.EndDrawing();
            }

            // Close the window when the loop ends
            Raylib.CloseWindow();
        }
    }
}