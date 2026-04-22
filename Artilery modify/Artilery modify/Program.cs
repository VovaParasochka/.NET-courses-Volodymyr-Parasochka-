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
        /* You don't need to say Raylib_cs.Color
         * because there is no ambiquity here.
         * There is also Color in System.Drawing but that is not used
         */
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

        /* Put game wide constants such as gravity to a
         * public static readonly Gravity = 380.0f
         */

        // Updates bullet movement every frame (gravity + wind)
        public void Update(float dt, float wind)
        {
            Velocity.Y += 380f * dt;           // Gravity pulls the bullet down
            Velocity.X += wind * dt * 0.8f;    // Wind makes the bullet drift realistically
            Position += Velocity * dt;

            /* This works but because of how lists are handled
             * it is silly: removing the first item one moves everything back
             * by one.
             * I think better way would be like this. But it does not really matter with just 12 items. But in general if you know the list is going to be always max 12 items, use an array.
             */
            const int trailPointAmount = 12;
            int trailIndex = 0;
            // Vector2 is a struct 
            Vector2[] trailPositions = new Vector2[trailPointAmount];
            for(int i = 0; i < trailPointAmount; i++)
            {
                // Hide unused points outside screen:
                trailPositions[i] = new Vector2(-1000, -1000);
            }
            trailPositions[trailIndex] = Position;
            trailIndex += 1;
            trailIndex = trailIndex % trailPointAmount; // Loops around to 0
            /* */

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
        /* If you lock the Cannon to facing a certain way, you need to 
         * change the code if you ever add more than 2 players */
        public float Facing = 1f;       // 1 = facing right, -1 = facing left
        public int Health = 100;
        public Raylib_cs.Color BaseColor;

        public Cannon(Vector2 pos, Raylib_cs.Color color, float facing)
        {
            Position = pos;
            BaseColor = color;
            Facing = facing;
        }

        /* This has so many magic numbers!
         * Also there is a standard way to rotate: use Matrix3x2
         * Raylib knows how to change between angles and radians
         * use Raylib.DEG2RAD and RAD2DEG, don't write the conversion
         * yourself.
         */
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

    /* No static >:( 
     * Does not serve any purpose and tons of typing.
     */
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
        /* I would calculate these numbers from the window size.
         * That way I wouldn't have to hunt for every number in code
         * and change them if I want to make the window bigger */
        private static float groundY = 520f;
        private static int terrainSegments = 60;
        private static float segmentWidth;
        private static float[] terrainHeight = new float[60];

        /* Why can players be null? */
        private static Cannon player1 = null!;
        private static Cannon player2 = null!;

        // ====================== CURRENT SHOT ======================
        private static Bullet? currentBullet = null;
        private static List<AmmoType> ammoTypes = new();

        /* When turns are alternating between players it is way easier
         * to put players in an array and use this as an index. That way
         * the code works with any number of players.
         */
        private static int currentPlayer = 1;
        private static bool isShooting = false;
        private static float power = 420f;
        private static int selectedAmmoIndex = 0;
        private static float wind = 0f;

        private static string message = "";
        private static bool gameOver = false;
        private static float aiTimer = 0f;

        /* Instead of Main, this should be Game.Run() or something similar */
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

        /* Put these in their own class that handles high scores
         */
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

        /* Since you know how to read JSON, read these values from
         * a file. That way you can easily add and remove ammo types
         * or let the designer do that.
         * This way this is just a big jumble of magic numbers.
         */
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

        /* Why is power set here again? If this is called at the start 
         * of the game, put all variable initialization in here and not in multiple places.
         * 
         * Resetting the players should be in the Cannon class.
         */
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
                /* GameMode could be a parameter to the StartNewGame()
                 * That would make it clearer and you could set up the AI
                 * player in there too.
                 * 
                 * Do not close the raylib window in the middle of update!
                 * Break out of the update/render loop first and then close.
                 */
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
            /* This is confusing: Down increases by 1 and Up increases by
             * 2 and then it also loops around.
             * This breaks immediately if one more difficulty option is added
             * but you don't get any error message about it.
             */
            if (Raylib.IsKeyPressed(KeyboardKey.Down))
                aiDifficulty = (Difficulty)(((int)aiDifficulty + 1) % 3);
            if (Raylib.IsKeyPressed(KeyboardKey.Up))
                aiDifficulty = (Difficulty)(((int)aiDifficulty + 2) % 3);

            /* By default pressing ESC closes the whole program. You 
             * have to manually set the closing button with
             * Raylib.SetExitKey() to use ESC for something else
             */
            if (Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                currentState = GameState.MainMenu;
        }

        /* The controls are confusing. 
         * ESC, Enter, Q are all used to go to MainMenu depending
         * the state of the game.
         * Use one button for one purpose.
         */

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
        /* This big function that does all kinds of things should be 
         * broken down to smaller functions that handle the different kinds
         * of things
         */
        private static void UpdateInGame(float dt)
        {
            if (gameOver) return;

            /* Here using array really helps. You could write
             * activeCannon = players[currentPlayer];
             * And setting the controller (human or AI )as a class variable of the Cannon you could
             * move the udpate logic to the Cannon class.
             * Now human also always needs to be player 1.
             */
            Cannon activeCannon = currentPlayer == 1 ? player1 : player2;
            bool isHumanTurn = currentPlayer == 1 || gameMode == GameMode.TwoPlayer;

            /* Just no. Move the keys and numbers to the Cannon class */
            // Human player controls
            if (isHumanTurn)
            {
                if (Raylib.IsKeyDown(KeyboardKey.Left)) activeCannon.Angle -= 80f * dt;
                if (Raylib.IsKeyDown(KeyboardKey.Right)) activeCannon.Angle += 80f * dt;
                /* It is confusing to have the angles as negative degrees.
                 * I would change so that the straight up or to the righ is 0 degrees and have the angle to be added to that
                 */
                activeCannon.Angle = Math.Clamp(activeCannon.Angle, -89f, -1f);

                if (Raylib.IsKeyDown(KeyboardKey.Up)) power += 220f * dt;
                if (Raylib.IsKeyDown(KeyboardKey.Down)) power -= 220f * dt;
                power = Math.Clamp(power, 200f, 820f);

                /* What if there is less or more than 4 ammo types?
                 * What if players have different types?
                 * 
                 * The KeyboardKey is also an enum so you could do something like this:
                 * for (int i = 0; i < ammoTypeAmount; i++)
                 * {
                 *  KeyboardKey keyname = (KeyboardKey) ((int)(KeyboardKey.One) + i);
                 *  if (Raylib.IsKeyPressed(keyname))
                 *  {
                 *      selectedAmmoindex = i;
                 *  }
                 */
                if (Raylib.IsKeyPressed(KeyboardKey.One)) selectedAmmoIndex = 0;
                if (Raylib.IsKeyPressed(KeyboardKey.Two)) selectedAmmoIndex = 1;
                if (Raylib.IsKeyPressed(KeyboardKey.Three)) selectedAmmoIndex = 2;
                if (Raylib.IsKeyPressed(KeyboardKey.Four)) selectedAmmoIndex = 3;

                if (Raylib.IsKeyPressed(KeyboardKey.Space) && currentBullet == null)
                    FireShot(activeCannon);
            }
            else // AI turn
            {
                /* Why is current bullet checked here?
                 * I would move the AI logic to its own function and inside
                 * the Cannon class
                 */
                aiTimer -= dt;
                if (aiTimer <= 0f && currentBullet == null)
                    DoAIShot();
            }

            /* Separate these checks to functions. That makes it easier
             * to read.
             */
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

            /* There is now two things that tell about the bullet
             * being active. Just have one.
             */
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
            /* This code combines all kinds of things together.
             * That makes it looks messy.
             * Why is there a boolean gameOver when there is also a state 
             * for it? Just have the state.
             */
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

            /* The gravity is here again as a magic number
             * If there is a ton of particles it is better to use
             * an object and a foreach
             */
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

            /* All the other keys changing states are together but this
             * is hidden here
             */
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                currentState = GameState.Paused;
        }

        /* Here again is code for degree/radian conversion and
         * rotation that is already done for you in the library.
         * A good programmer is lazy in a way that they avoid
         * doing same work multiple times.
         */
        private static void FireShot(Cannon cannon)
        {
            AmmoType ammo = ammoTypes[selectedAmmoIndex];
            float rad = cannon.Angle * (MathF.PI / 180f);
            Vector2 direction = new Vector2(MathF.Cos(rad) * cannon.Facing, MathF.Sin(rad));

            /* The barrel length is suddenly here as magic number.
             * Will you remember it if you make the barrels longer or shorter?
             * 
             * How is isShooting different from bullet being active? Try to have as few state related variables as possible.
             * If you have multiple you can accidentally create softlocks or impossible situations.
             */
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
            /* Why -40 ? Remove magic numbers or name them */
            float dy = target.Position.Y - aiCannon.Position.Y - 40f;
            /* There is a function for this! Don't calculate it yourself */
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            /* Can you explain how this code works or change it if
             * testing reveals that the AI is too good or too easy?
             * Will it break if the window is made bigger or smaller or
             * the terrain generation changes.
             */
            float baseAngle = distance < 380 ? -28f : -52f;
            float errorMultiplier = aiDifficulty switch
            {
                Difficulty.Easy => 28f,
                Difficulty.Medium => 14f,
                Difficulty.Hard => 5f,
				/* This is silly. An enum can only have the values that are defined in the enum. The value cannot be anything else.*/
                _ => 14f 
            };

            /* Why randomize a double and then convert it to float immediately?
             * Just use NextSingle()
             */
            float angleError = (float)((rand.NextDouble() - 0.5) * errorMultiplier);
            aiCannon.Angle = Math.Clamp(baseAngle + angleError, -88f, -8f);

            power = 380f + (distance / 3.2f) + (float)((rand.NextDouble() - 0.5) * 110f);
            power = Math.Clamp(power, 260f, 780f);

            /* What if designer changes the ammo types?*/
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

        /* Have the amount or the multiplier as parameter, that way the bullet type or
         * other factors can affect it too and it is no longer tied to window size.
         * There is so many magic numbers here that i don't even
         */
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

        /* Put this code in the cannon class*/
        private static void CheckCannonHit(Cannon cannon, Bullet bullet)
        {
            Rectangle cannonRect = new Rectangle(cannon.Position.X - 28, cannon.Position.Y - 32, 56, 44);
            if (Raylib.CheckCollisionCircleRec(bullet.Position, bullet.Type.ExplosionRadius * 0.9f, cannonRect))
            {
                /* The damage should be a variable of the bullet class.
                 * This code breaks if bullet name changes.
                 */
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

            /* Use a switch when selecting with enum */
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

		/* There is a library for these. use it or at least 
         * make helper functions. This is impossible to edit.
         * I made one such library here:
         * https://github.com/bc-peliohjelmointi/RayGuiCreator
         */
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

            /* Why is health bar drawing separate from drawing the cannon?*/
            // Draw cannons and health bars
            player1.Draw();
            player2.Draw();
            player1.DrawHealthBar();
            player2.DrawHealthBar();

            /* This code should be in Bullet class */
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
            /* This code should be in Particle class */
            // Draw explosion particles
            foreach (var p in particles)
            {
                byte alpha = (byte)(p.LifeTime * 220f);
                Raylib_cs.Color c = new Raylib_cs.Color(p.Color.R, p.Color.G, p.Color.B, alpha);
                Raylib.DrawCircleV(p.Position, p.Size * (p.LifeTime / 1.7f), c);
            }

            /* ARGGGH */
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