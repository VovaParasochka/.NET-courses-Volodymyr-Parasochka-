using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;

namespace CannonGame
{
    // ================================================
    // TYKKIPELI PRO - Ultimate Artillery Battle
    // Fully commented version - every part explained!
    // ================================================

    // This class defines the different types of ammunition.
    // Each ammo has a name, shooting power, explosion radius and color.
    internal class AmmoType
    {
        public string Name { get; set; }
        public float Power { get; set; }
        public float ExplosionRadius { get; set; }
        public Raylib_cs.Color Color { get; set; }

        public AmmoType(string name, float power, float radius, Raylib_cs.Color color)
        {
            Name = name;
            Power = power;
            ExplosionRadius = radius;
            Color = color;
        }
    }

    // This represents a flying projectile (shell).
    // It keeps track of position, velocity, trail effect and whether it's still active.
    internal class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public AmmoType Type;
        public bool IsActive = true;
        public List<Vector2> Trail = new();   // Stores previous positions for the nice smoke trail

        public Bullet(Vector2 startPos, Vector2 direction, float power, AmmoType type)
        {
            Position = startPos;
            Velocity = direction * power;
            Type = type;
            Trail.Add(startPos);               // Start the trail at the cannon barrel
        }

        // Updates bullet movement every frame (gravity + wind)
        public void Update(float dt, float wind)
        {
            Velocity.Y += 380f * dt;           // Gravity pulls the bullet down
            Velocity.X += wind * dt * 0.8f;    // Wind makes the bullet drift realistically
            Position += Velocity * dt;

            // Keep the trail smooth and not too long
            Trail.Add(Position);
            if (Trail.Count > 12) Trail.RemoveAt(0);
        }
    }

    // Represents one player's cannon (red or blue).
    // Handles drawing the cannon body, rotating barrel and health bar.
    internal class Cannon
    {
        public Vector2 Position;
        public float Angle = -45f;      // Aiming angle in degrees
        public float Facing = 1f;       // 1 = facing right, -1 = facing left
        public int Health = 100;
        public Raylib_cs.Color BaseColor;

        public Cannon(Vector2 pos, Raylib_cs.Color color, float facing)
        {
            Position = pos;
            BaseColor = color;
            Facing = facing;
        }

        // Draws the tank-like cannon with a rotating barrel
        public void Draw()
        {
            // Shadow under the base for a bit of depth
            Raylib.DrawRectangle((int)Position.X - 25, (int)Position.Y - 15, 50, 22, new Raylib_cs.Color(40, 40, 40, 255));
            Raylib.DrawRectangle((int)Position.X - 22, (int)Position.Y - 12, 44, 18, BaseColor);

            // Calculate barrel end position based on angle and facing direction
            float rad = Angle * (MathF.PI / 180f);
            Vector2 barrelOffset = new Vector2(MathF.Cos(rad) * 40f * Facing, MathF.Sin(rad) * 40f);
            Vector2 barrelEnd = Position + barrelOffset;

            Raylib.DrawLineEx(Position, barrelEnd, 12f, new Raylib_cs.Color(70, 70, 70, 255));
            Raylib.DrawLineEx(Position, barrelEnd, 7f, new Raylib_cs.Color(100, 100, 100, 255)); // highlight
            Raylib.DrawCircleV(Position, 14f, BaseColor);
            Raylib.DrawCircleV(Position, 8f, new Raylib_cs.Color(30, 30, 30, 255));
        }

        // Draws a small health bar above the cannon
        public void DrawHealthBar()
        {
            float barWidth = 60f;
            float barX = Position.X - barWidth / 2;
            float barY = Position.Y - 45;
            Raylib.DrawRectangle((int)barX, (int)barY, (int)barWidth, 8, new Raylib_cs.Color(0, 0, 0, 180));
            Raylib.DrawRectangle((int)barX, (int)barY, (int)(barWidth * (Health / 100f)), 8, Raylib_cs.Color.Green);
        }
    }

    // Simple particle used for explosion effects
    internal class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float LifeTime;
        public Raylib_cs.Color Color;
        public float Size;
    }

    // Data structure for high score entries (saved to JSON)
    internal class HighScore
    {
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    // All possible screens/states of the game
    internal enum GameState { MainMenu, Options, HighScores, InGame, Paused, GameOver }
    internal enum GameMode { TwoPlayer, VsAI }
    internal enum Difficulty { Easy, Medium, Hard }

    internal class Program
    {
        // ====================== ASSETS ======================
        private static Texture2D backgroundTexture;
        private static Sound shootSound;
        private static Sound explosionSound;
        private static Sound hitSound;
        private static Music bgMusic;

        // ====================== GAME OBJECTS ======================
        private static List<Particle> particles = new();
        private static List<HighScore> highScores = new();
        private static Random rand = new Random();

        // ====================== GAME STATE ======================
        private static GameState currentState = GameState.MainMenu;
        private static GameMode gameMode = GameMode.TwoPlayer;
        private static Difficulty aiDifficulty = Difficulty.Medium;

        private static int selectedMenuIndex = 0;
        private static readonly List<string> mainMenuOptions = new()
        {
            "Play - 2 Players (Hotseat)",
            "Play vs AI",
            "High Scores",
            "Options",
            "Quit Game"
        };

        // ====================== TERRAIN & PLAYERS ======================
        private static float groundY = 520f;
        private static int terrainSegments = 60;
        private static float segmentWidth;
        private static float[] terrainHeight = new float[60];

        private static Cannon player1 = null!;
        private static Cannon player2 = null!;

        // ====================== CURRENT SHOT ======================
        private static Bullet? currentBullet = null;
        private static List<AmmoType> ammoTypes = new();

        private static int currentPlayer = 1;
        private static bool isShooting = false;
        private static float power = 420f;
        private static int selectedAmmoIndex = 0;
        private static float wind = 0f;

        private static string message = "";
        private static bool gameOver = false;
        private static float aiTimer = 0f;

        static void Main(string[] args)
        {
            Raylib.InitWindow(1000, 600, "Tykkipeli PRO - Ultimate Artillery Battle");
            Raylib.SetTargetFPS(60);

            LoadAssets();           // Load images and sounds
            LoadHighScores();       // Load previous high scores from JSON
            InitializeGameData();   // Create terrain, cannons and ammo types

            while (!Raylib.WindowShouldClose())
            {
                float dt = Raylib.GetFrameTime();
                Raylib.UpdateMusicStream(bgMusic);

                Update(dt);   // Handle all game logic
                Draw();       // Draw everything on screen
            }

            UnloadAssets();       // Clean up memory before closing
            Raylib.CloseWindow();
        }

        // Loads all textures, sounds and music
        private static void LoadAssets()
        {
            backgroundTexture = Raylib.LoadTexture("assets/background.png");
            shootSound = Raylib.LoadSound("assets/shoot.wav");
            explosionSound = Raylib.LoadSound("assets/explosion.wav");
            hitSound = Raylib.LoadSound("assets/hit.wav");
            bgMusic = Raylib.LoadMusicStream("assets/music.ogg");
            Raylib.SetMusicVolume(bgMusic, 0.6f);
            Raylib.PlayMusicStream(bgMusic);
        }

        private static void UnloadAssets()
        {
            Raylib.UnloadTexture(backgroundTexture);
            Raylib.UnloadSound(shootSound);
            Raylib.UnloadSound(explosionSound);
            Raylib.UnloadSound(hitSound);
            Raylib.UnloadMusicStream(bgMusic);
        }

        // Loads saved high scores from highscores.json
        private static void LoadHighScores()
        {
            string path = "highscores.json";
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    highScores = JsonSerializer.Deserialize<List<HighScore>>(json) ?? new List<HighScore>();
                }
                catch { /* ignore if file is broken */ }
            }
        }

        // Saves high scores (only top 10) to JSON file
        private static void SaveHighScores()
        {
            var topScores = highScores.OrderByDescending(h => h.Score).Take(10).ToList();
            string json = JsonSerializer.Serialize(topScores, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("highscores.json", json);
        }

        // Sets up terrain, ammo list and creates both cannons
        private static void InitializeGameData()
        {
            segmentWidth = 1000f / terrainSegments;

            // All available ammo types
            ammoTypes = new List<AmmoType>
            {
                new AmmoType("Normal Shell", 520f, 28f, new Raylib_cs.Color(255, 240, 100, 255)),
                new AmmoType("Heavy Bomb", 380f, 48f, new Raylib_cs.Color(255, 80, 80, 255)),
                new AmmoType("Fast Rocket", 680f, 18f, new Raylib_cs.Color(120, 255, 120, 255)),
                new AmmoType("Cluster Shell", 460f, 35f, new Raylib_cs.Color(180, 100, 255, 255))
            };

            player1 = new Cannon(new Vector2(0, 0), new Raylib_cs.Color(220, 40, 40, 255), 1f);   // Red cannon
            player2 = new Cannon(new Vector2(0, 0), new Raylib_cs.Color(40, 140, 255, 255), -1f); // Blue cannon

            GenerateNewTerrain();
            PlaceCannonsOnTerrain();
        }

        // Creates a new random hilly terrain every new round
        private static void GenerateNewTerrain()
        {
            float currentHeight = groundY - 90f;
            for (int i = 0; i < terrainSegments; i++)
            {
                currentHeight += rand.Next(-22, 23);
                currentHeight = Math.Clamp(currentHeight, groundY - 190f, groundY - 25f);
                terrainHeight[i] = currentHeight;
            }
        }

        // Places both cannons on top of the generated terrain
        private static void PlaceCannonsOnTerrain()
        {
            int p1Seg = 9;
            player1.Position = new Vector2(p1Seg * segmentWidth + segmentWidth / 2, terrainHeight[p1Seg] - 14);

            int p2Seg = terrainSegments - 10;
            player2.Position = new Vector2(p2Seg * segmentWidth + segmentWidth / 2, terrainHeight[p2Seg] - 14);
        }

        // Resets everything for a brand new game
        private static void StartNewGame()
        {
            gameOver = false;
            message = "";
            currentPlayer = 1;
            currentBullet = null;
            isShooting = false;
            power = 420f;
            selectedAmmoIndex = 0;
            particles.Clear();

            player1.Health = 100;
            player2.Health = 100;

            GenerateNewTerrain();
            PlaceCannonsOnTerrain();

            currentState = GameState.InGame;
        }

        // Main update function - decides what to update depending on current screen
        private static void Update(float dt)
        {
            switch (currentState)
            {
                case GameState.MainMenu: UpdateMainMenu(); break;
                case GameState.Options: UpdateOptions(); break;
                case GameState.HighScores: UpdateHighScores(); break;
                case GameState.InGame: UpdateInGame(dt); break;
                case GameState.Paused: UpdatePaused(); break;
                case GameState.GameOver: UpdateGameOver(); break;
            }
        }

        // ====================== MENU UPDATES ======================
        private static void UpdateMainMenu()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Down)) selectedMenuIndex = (selectedMenuIndex + 1) % mainMenuOptions.Count;
            if (Raylib.IsKeyPressed(KeyboardKey.Up)) selectedMenuIndex = (selectedMenuIndex - 1 + mainMenuOptions.Count) % mainMenuOptions.Count;

            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                switch (selectedMenuIndex)
                {
                    case 0: gameMode = GameMode.TwoPlayer; StartNewGame(); break;
                    case 1: gameMode = GameMode.VsAI; StartNewGame(); break;
                    case 2: currentState = GameState.HighScores; break;
                    case 3: currentState = GameState.Options; break;
                    case 4: Raylib.CloseWindow(); break;
                }
            }
        }

        private static void UpdateOptions()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Down))
                aiDifficulty = (Difficulty)(((int)aiDifficulty + 1) % 3);
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
                aiDifficulty = (Difficulty)(((int)aiDifficulty + 2) % 3);

            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                currentState = GameState.MainMenu;
        }

        private static void UpdateHighScores()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                currentState = GameState.MainMenu;
        }

        private static void UpdatePaused()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                currentState = GameState.InGame;
            if (Raylib.IsKeyPressed(KeyboardKey.Q))
                currentState = GameState.MainMenu;
        }

        private static void UpdateGameOver()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.R)) StartNewGame();
            if (Raylib.IsKeyPressed(KeyboardKey.M)) currentState = GameState.MainMenu;
        }

        // ====================== MAIN GAMEPLAY UPDATE ======================
        private static void UpdateInGame(float dt)
        {
            if (gameOver) return;

            Cannon activeCannon = currentPlayer == 1 ? player1 : player2;
            bool isHumanTurn = currentPlayer == 1 || gameMode == GameMode.TwoPlayer;

            // Human player controls
            if (isHumanTurn)
            {
                if (Raylib.IsKeyDown(KeyboardKey.Left)) activeCannon.Angle -= 80f * dt;
                if (Raylib.IsKeyDown(KeyboardKey.Right)) activeCannon.Angle += 80f * dt;
                activeCannon.Angle = Math.Clamp(activeCannon.Angle, -89f, -1f);

                if (Raylib.IsKeyDown(KeyboardKey.Up)) power += 220f * dt;
                if (Raylib.IsKeyDown(KeyboardKey.Down)) power -= 220f * dt;
                power = Math.Clamp(power, 200f, 820f);

                if (Raylib.IsKeyPressed(KeyboardKey.One)) selectedAmmoIndex = 0;
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) selectedAmmoIndex = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) selectedAmmoIndex = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) selectedAmmoIndex = 3;

                if (Raylib.IsKeyPressed(KeyboardKey.Space) && currentBullet == null)
                    FireShot(activeCannon);
            }
            else // AI turn
            {
                aiTimer -= dt;
                if (aiTimer <= 0f && currentBullet == null)
                    DoAIShot();
            }

            // Update flying bullet
            if (currentBullet != null && currentBullet.IsActive)
            {
                currentBullet.Update(dt, wind);

                // Out of bounds check
                if (currentBullet.Position.X < 0 || currentBullet.Position.X > 1000 || currentBullet.Position.Y > 610)
                    currentBullet.IsActive = false;

                // Terrain collision
                int seg = (int)(currentBullet.Position.X / segmentWidth);
                if (seg >= 0 && seg < terrainSegments && currentBullet.Position.Y > terrainHeight[seg])
                {
                    Explode(currentBullet.Position, currentBullet.Type.ExplosionRadius);
                    CheckCannonHit(player1, currentBullet);
                    CheckCannonHit(player2, currentBullet);
                    currentBullet.IsActive = false;
                }
            }

            // When bullet stops → next player's turn
            if (currentBullet != null && !currentBullet.IsActive)
            {
                currentBullet = null;
                isShooting = false;
                currentPlayer = currentPlayer == 1 ? 2 : 1;
                wind = rand.Next(-48, 49);   // New random wind every turn

                if (gameMode == GameMode.VsAI && currentPlayer == 2)
                    aiTimer = 1.4f + (float)rand.NextDouble() * 1.8f;
            }

            // Check for winner
            if (player1.Health <= 0 || player2.Health <= 0)
            {
                gameOver = true;
                currentState = GameState.GameOver;

                Cannon winner = player1.Health > 0 ? player1 : player2;
                string winnerName = gameMode == GameMode.VsAI
                    ? (winner == player1 ? "HUMAN (RED)" : "AI (BLUE)")
                    : (winner == player1 ? "RED PLAYER" : "BLUE PLAYER");

                int score = 600 + winner.Health * 7;
                highScores.Add(new HighScore { PlayerName = winnerName, Score = score });
                SaveHighScores();

                message = winner == player1 ? "RED WINS!" : "BLUE WINS!";
            }

            // Update explosion particles
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.LifeTime -= dt;
                if (p.LifeTime <= 0) { particles.RemoveAt(i); continue; }
                p.Position += p.Velocity * dt;
                p.Velocity.Y += 420f * dt;
                p.Velocity *= 0.96f;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                currentState = GameState.Paused;
        }

        private static void FireShot(Cannon cannon)
        {
            AmmoType ammo = ammoTypes[selectedAmmoIndex];
            float rad = cannon.Angle * (MathF.PI / 180f);
            Vector2 direction = new Vector2(MathF.Cos(rad) * cannon.Facing, MathF.Sin(rad));

            Vector2 barrelTip = cannon.Position + direction * 44f;
            currentBullet = new Bullet(barrelTip, direction, power, ammo);
            isShooting = true;

            Raylib.PlaySound(shootSound);
        }

        // AI makes a smart shot (difficulty affects accuracy)
        private static void DoAIShot()
        {
            Cannon aiCannon = player2;
            Cannon target = player1;

            float dx = target.Position.X - aiCannon.Position.X;
            float dy = target.Position.Y - aiCannon.Position.Y - 40f;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            float baseAngle = distance < 380 ? -28f : -52f;
            float errorMultiplier = aiDifficulty switch
            {
                Difficulty.Easy => 28f,
                Difficulty.Medium => 14f,
                Difficulty.Hard => 5f,
                _ => 14f
            };

            float angleError = (float)((rand.NextDouble() - 0.5) * errorMultiplier);
            aiCannon.Angle = Math.Clamp(baseAngle + angleError, -88f, -8f);

            power = 380f + (distance / 3.2f) + (float)((rand.NextDouble() - 0.5) * 110f);
            power = Math.Clamp(power, 260f, 780f);

            selectedAmmoIndex = distance > 520 ? 1 : (rand.Next(3) == 0 ? 3 : 0);

            FireShot(aiCannon);
        }

        private static void Explode(Vector2 pos, float radius)
        {
            Raylib.PlaySound(explosionSound);

            // Destroy terrain around explosion point
            int center = (int)(pos.X / segmentWidth);
            int range = (int)(radius / segmentWidth) + 3;
            for (int i = Math.Max(0, center - range); i < Math.Min(terrainSegments, center + range); i++)
            {
                float dist = Math.Abs(i * segmentWidth + segmentWidth / 2 - pos.X);
                if (dist < radius)
                {
                    float damage = (radius - dist) * 2.1f;
                    terrainHeight[i] += damage;
                    if (terrainHeight[i] > 585f) terrainHeight[i] = 585f;
                }
            }

            CreateExplosionParticles(pos, radius);
        }

        private static void CreateExplosionParticles(Vector2 pos, float radius)
        {
            int count = (int)(radius * 1.6f);
            for (int i = 0; i < count; i++)
            {
                float angle = (float)rand.NextDouble() * MathF.PI * 2f;
                float speed = 120f + (float)rand.NextDouble() * radius * 7f;

                particles.Add(new Particle
                {
                    Position = pos,
                    Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed - 80f),
                    LifeTime = 0.9f + (float)rand.NextDouble() * 0.8f,
                    Color = rand.Next(3) == 0 ? new Raylib_cs.Color(255, 200, 60, 255) : new Raylib_cs.Color(255, rand.Next(80, 220), 20, 255),
                    Size = 4f + (float)rand.NextDouble() * 9f
                });
            }
        }

        private static void CheckCannonHit(Cannon cannon, Bullet bullet)
        {
            Rectangle cannonRect = new Rectangle(cannon.Position.X - 28, cannon.Position.Y - 32, 56, 44);
            if (Raylib.CheckCollisionCircleRec(bullet.Position, bullet.Type.ExplosionRadius * 0.9f, cannonRect))
            {
                int damage = bullet.Type.Name.Contains("Heavy") ? 62 : 38;
                cannon.Health = Math.Max(0, cannon.Health - damage);
                Raylib.PlaySound(hitSound);
            }
        }

        // ====================== DRAWING ======================
        private static void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (currentState == GameState.MainMenu)
                DrawMainMenu();
            else if (currentState == GameState.Options)
                DrawOptions();
            else if (currentState == GameState.HighScores)
                DrawHighScores();
            else
                DrawGame();

            Raylib.EndDrawing();
        }

        private static void DrawMainMenu()
        {
            Raylib.DrawText("TYKKIPELI PRO", 210, 110, 68, new Raylib_cs.Color(255, 220, 60, 255));
            Raylib.DrawText("ARTILLERY BATTLE", 340, 175, 28, new Raylib_cs.Color(180, 220, 255, 255));

            for (int i = 0; i < mainMenuOptions.Count; i++)
            {
                Raylib_cs.Color col = i == selectedMenuIndex ? new Raylib_cs.Color(255, 240, 80, 255) : Raylib_cs.Color.White;
                Raylib.DrawText(mainMenuOptions[i], 320, 250 + i * 48, 32, col);
            }

            Raylib.DrawText("↑ ↓ = select   ENTER = confirm", 310, 510, 18, new Raylib_cs.Color(255, 255, 255, 120));
        }

        private static void DrawOptions()
        {
            Raylib.DrawText("OPTIONS", 380, 140, 52, Raylib_cs.Color.Yellow);
            Raylib.DrawText($"AI Difficulty: {aiDifficulty}", 310, 260, 34, Raylib_cs.Color.White);
            Raylib.DrawText("(Up/Down to change)", 360, 310, 22, new Raylib_cs.Color(200, 200, 200, 200));
            Raylib.DrawText("ESC or ENTER = back to menu", 300, 480, 20, Raylib_cs.Color.Gray);
        }

        private static void DrawHighScores()
        {
            Raylib.DrawText("HIGH SCORES", 310, 90, 48, new Raylib_cs.Color(255, 240, 100, 255));

            for (int i = 0; i < Math.Min(10, highScores.Count); i++)
            {
                var hs = highScores.OrderByDescending(h => h.Score).ElementAt(i);
                string line = $"{i + 1}. {hs.PlayerName} - {hs.Score} pts ({hs.Date.ToShortDateString()})";
                Raylib.DrawText(line, 180, 170 + i * 32, 24, Raylib_cs.Color.White);
            }

            Raylib.DrawText("ESC = back to menu", 370, 520, 20, Raylib_cs.Color.Gray);
        }

        private static void DrawGame()
        {
            // Background image
            Raylib.DrawTexture(backgroundTexture, 0, 0, Raylib_cs.Color.White);

            // Draw terrain
            for (int i = 0; i < terrainSegments; i++)
            {
                float x = i * segmentWidth;
                Raylib.DrawRectangle((int)x, (int)terrainHeight[i], (int)segmentWidth + 1, (int)(610 - terrainHeight[i]),
                    new Raylib_cs.Color(110, 95, 55, 255));
                Raylib.DrawLine((int)x, (int)terrainHeight[i], (int)(x + segmentWidth), (int)terrainHeight[i],
                    new Raylib_cs.Color(40, 160, 40, 255));
            }

            // Draw cannons and health bars
            player1.Draw();
            player2.Draw();
            player1.DrawHealthBar();
            player2.DrawHealthBar();

            // Draw flying bullet + trail
            if (currentBullet != null && currentBullet.IsActive)
            {
                // Bullet trail
                for (int i = 0; i < currentBullet.Trail.Count - 1; i++)
                {
                    float alpha = (i / (float)currentBullet.Trail.Count) * 180f;
                }
                // The bullet itself
                Raylib.DrawCircleV(currentBullet.Position, 9.5f, currentBullet.Type.Color);
            }

            // Draw explosion particles
            foreach (var p in particles)
            {
                byte alpha = (byte)(p.LifeTime * 220f);
                Raylib_cs.Color c = new Raylib_cs.Color(p.Color.R, p.Color.G, p.Color.B, alpha);
                Raylib.DrawCircleV(p.Position, p.Size * (p.LifeTime / 1.7f), c);
            }

            // HUD
            string turnText = currentPlayer == 1 ? "RED'S TURN" : "BLUE'S TURN";
            Raylib.DrawText(turnText, 25, 18, 32, currentPlayer == 1 ? new Raylib_cs.Color(255, 70, 70, 255) : new Raylib_cs.Color(70, 190, 255, 255));

            AmmoType curAmmo = ammoTypes[selectedAmmoIndex];
            Raylib.DrawText($"Ammo: {curAmmo.Name}   Power: {(int)power}", 25, 58, 24, new Raylib_cs.Color(255, 255, 160, 255));
            Raylib.DrawText($"WIND: {wind:F0}", 25, 92, 22, new Raylib_cs.Color(200, 240, 255, 255));

            Raylib.DrawText($"RED: {player1.Health} HP", 720, 18, 26, new Raylib_cs.Color(255, 80, 80, 255));
            Raylib.DrawText($"BLUE: {player2.Health} HP", 720, 52, 26, new Raylib_cs.Color(70, 180, 255, 255));

            if (!isShooting && !gameOver && (currentPlayer == 1 || gameMode == GameMode.TwoPlayer))
                Raylib.DrawText("← → Aim   ↑ ↓ Power   SPACE Fire   1-4 Ammo", 170, 545, 19, new Raylib_cs.Color(255, 255, 255, 160));

            // Game Over overlay
            if (gameOver)
            {
                Raylib.DrawRectangle(0, 0, 1000, 600, new Raylib_cs.Color(0, 0, 0, 160));
                Raylib.DrawText(message, 265, 210, 64, new Raylib_cs.Color(255, 240, 90, 255));
                Raylib.DrawText("R = Play Again    M = Main Menu", 290, 300, 28, Raylib_cs.Color.White);
            }

            // Pause screen
            if (currentState == GameState.Paused)
            {
                Raylib.DrawRectangle(0, 0, 1000, 600, new Raylib_cs.Color(0, 0, 0, 180));
                Raylib.DrawText("PAUSED", 390, 240, 52, Raylib_cs.Color.Yellow);
                Raylib.DrawText("ESC = Resume    Q = Quit to Menu", 310, 320, 26, Raylib_cs.Color.White);
            }
        }
    }
}