using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

using System.IO;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Info;


using Microsoft.Advertising.Mobile.Xna;

namespace WindowsPhoneGame2
{
    public class Puzzle
    {
        public string texture { set; get;  }
        public string artist { set; get;  }
        public string website { set; get;  }
        public Puzzle()
        {
            //System.Diagnostics.Debug.WriteLine(artist);
        }
    }

    public class Medals
    {
        public string name;
        public bool silver;
        public bool gold;
        public Medals(string _n, bool _silver, bool _gold)
        {
            name = _n;
            silver = _silver;
            gold = _gold;
            //System.Diagnostics.Debug.WriteLine("added new medals " + name);
        }
    }


    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        DrawableAd bannerAd,bannerAd2;

        public static readonly string applicationId = "";

        public static readonly string adUnitId = "88547";

            public int swidth = 800;
            public int sheight = 480;
            public int tsize_easy = 160;
            public int tsize_hard = 80;
            int total_puzzlers = 15; //total puzzles per group, total groups
            public int cyopID = 0; //this will be initialized to the spot in the array that is the cyop
            float loadingtime = 2;  // loading screen timer
            Texture2D[] tex;
            Texture2D border;
            Texture2D cyop_texture;
            Texture2D silver_medal,gold_medal,fireworks;//,loading;

            string[] puzzles;
            double gametimesec;
            Tiles puzzle_easy, puzzle_hard, puzzlers, puzzlergroups;

            bool easy, hard = false;
            bool medal_saved = false;

            //SpriteBatch ForegroundBatch;
            //SpriteFont CourierNew;
            Vector2 FontPos;
            float FontScale;
            //float FontRotation;
            string output="";

            Puzzle[,] puzzes; //includes puzzlergroups and puzzles in them
            List<Medals> medals = new List<Medals>(); //list of medals, name associates to puzzle's texture

            public void writeOut(string stuff, Vector2 pos, float scale)
            {
                output = stuff;
                FontPos = pos;
                FontScale = scale;
            }
            public void writeOutClear() { writeOut("", FontPos, 1.0f); }

            public double currentGametime(GameTime gameTime) { return gameTime.TotalGameTime.TotalSeconds; }
            public double lostGametime = -1; //used to check for blackholes
           
            void readPuzzles()
            {
                    XDocument loadedData = XDocument.Load(@"Content\puzzles.xml");

                    var data = from query in loadedData.Descendants("Item")
                               select new Puzzle()
                               {
                                    texture = (string)query.Element("texture"),
                                    artist = (string)query.Element("artist"),
                                    website = (string)query.Element("website")
                               };

                    
                    puzzes = new Puzzle[((data.Count() / total_puzzlers) + 1), total_puzzlers]; //create enough puzzle objects

                    //System.Diagnostics.Debug.WriteLine(puzzes.GetLength(0)-1);

                    int p = 0; //puzzlers
                    int pg = 0; //puzzler groups
                    foreach (var stuff in data)
                    {
                        puzzes[pg, p] = stuff;

                        if (p == (total_puzzlers-1)) { p = 0; pg++; }
                        else { p++; }
                    }

                    cyopID = puzzes.GetLength(0) - 1;
                    Puzzle mypuzz = new Puzzle();
                    mypuzz.texture = "mypuzzle";
                    mypuzz.artist = "Create Your Own Puzzle";
                    mypuzz.website = "http://juncoapps.com";
                    puzzes[cyopID, 0] = mypuzz;
            }



            void readMedals()
            {
                string file = "medals.csv";
                var myStore = IsolatedStorageFile.GetUserStoreForApplication();

                if (myStore.FileExists(file))
                {

                    IsolatedStorageFileStream stream = myStore.OpenFile(file, FileMode.Open);
                    StreamReader sr = new StreamReader(stream);
                    string line;
                    string[] medal = new string[3];
                    bool md1=false, md2=false;
                    while ((line = sr.ReadLine()) != null)
                    {
                        medal = line.Split(',');
                        //System.Diagnostics.Debug.WriteLine("loading medal: "+medal[0]+","+medal[1]+","+medal[2]);
                        if (medal[1] == "true") { md1 = true; }
                        if (medal[2] == "true") { md2 = true; }
                        medals.Add(new Medals(medal[0], md1,md2));
                        md1 = false;
                        md2 = false;
                    } 

                    sr.Close();

                    //System.Diagnostics.Debug.WriteLine("file " + file+" loaded");
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("no such file " + file);
                    
                }
            }

            void writeMedals()
            {
                string file = "medals.csv";
                string md1 = "false", md2 = "false";
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (Stream stream = storage.CreateFile(file))
                    {
                        StreamWriter sw = new StreamWriter(stream);

                        foreach (var medal in medals)
                        {
                            if (medal.silver) { md1 = "true"; }
                            if (medal.gold) { md2 = "true"; }
                            //System.Diagnostics.Debug.WriteLine("saving medal: " + medal.name);
                            sw.WriteLine(medal.name + "," + md1 + "," + md2);
                            md1 = "false";
                            md2 = "false";
                        }

                        sw.Close();
                    }
                }
            }



            public class Item
            {
                float x, y = 0;
                int width, height = 0;
                public Texture2D texture;
                bool has_texture=false;
                Game1 theGame;
                public bool play=false;
                string text;
                float tx, ty = 0;
                //int rx, ry = 0;
                float tscale;
                float fader = -1.0f; // fader alpha value
                float alpha=1.0f;

                public bool Touch(float px, float py)
                {
                    if ((px >= x) && (px <= (x + width)) &&
                        (py >= y) && (py <= (y + height)))
                    {
                        return true;
                    }

                    return false;
                }

                public void setSize(int _w, int _h) { width = _w; height = _h; }

                public void loadTexture(string tex)
                {
                    try
                    {
                        texture = theGame.Content.Load<Texture2D>(tex);
                        width = texture.Width;
                        height = texture.Height;
                        has_texture = true;
                    }
                    catch { /*System.Diagnostics.Debug.WriteLine("cannot load texture " + tex);*/ }
                }

                public void Display()
                {
                    if (play)
                    {
                        if (has_texture) { theGame.spriteBatch.Draw(texture, new Vector2(x, y), Color.White * alpha); }

                        if (text != null) { theGame.writeOut(text,new Vector2(tx,ty),tscale); }
                    }
                }

                public void setPos(float nx, float ny)
                {
                    x = nx;
                    y = ny;
                }
                public void setText(string _text,float _x,float _y,float _scale)
                {
                    text = _text;
                    tx = _x;
                    ty = _y;
                    tscale = _scale;
                }

                public void Play(bool _play)
                {
                    play = _play;
                    if (!_play) { theGame.writeOutClear(); } //reset text output to nothing
                }

                public void PlayFade(float time)
                {
                    if (time > -1.0f) { fader = time + (float)theGame.gametimesec; this.Play(true); } //manually updating fader?

                    if (fader > -1.0f)
                    {
                        alpha = Math.Abs(((float)theGame.gametimesec - fader) / 5);
                        //theGame.spriteBatch.Draw(theGame.loading, new Vector2(0, 0), Color.White * alpha);
                        if (alpha < .02f) { fader = -1; }
                    }
                    else { this.Play(false); } //kill itself
                }

                public Item(Game1 game) { theGame = game; }
            }

                Item Easy;
                Item Hard;
                Item Info;
                Item Hint;
                Item Back;
                Item PicInfo;
                Item PicSave; //save puzzle pic to phone
                Item About;
                Item AboutInfo;
                Item POTW; //puzzle of the week
                Item CYOP; //create your own puzzle
                Item Logo;
                Item Loading; //loading page
                Item Banner; //used to prevent app clicks on banner

        class Tiles
        {
            Game1 theGame;
            int swidth = 800;
            int sheight = 480;
            int tsize;// = 80;//160;
            int tnum;
            Rectangle[] rect;
            Vector2[] position;
            int tile1 = -1; //tile1 to be swapped with tile2
            int tile2 = -1;
            public int load_puzzle = 0; //used to load in the counted puzzle
            bool puzzle_finished = false;
            public float show_photo = -1.0f; //show photo fader alpha value
            public float fx_fireworks = -1.0f; //fireworks fader alpha value
            public bool play = false; //are we currently playing this tile arrangement to the screen?
            

            public void loadPuzzlers() //load puzzlers based on puzzle group
            {
                if (load_puzzle == (theGame.cyopID)) //is this the cyop?
                {
                    theGame.CYOP.Play(false);
                    theGame.About.Play(false);
                    theGame.Logo.Play(false);
                    this.Stop();
                    theGame.puzzle_easy.load_puzzle = load_puzzle;
                    theGame.puzzle_hard.load_puzzle = load_puzzle;
                    theGame.Easy.Play(true);
                    theGame.Hard.Play(true);
                    theGame.puzzlers.load_puzzle = -1; //use this as a switch so we can back out directly to main screen later

                    //theGame.bannerAd2.Visible = true;
                }
                else
                {
                    for (int n = 0; n < theGame.puzzes.GetLength(1); n++)
                    {
                        theGame.tex[n] = theGame.Content.Load<Texture2D>(theGame.puzzes[load_puzzle, n].texture);
                        theGame.tex[n].Name = theGame.puzzes[load_puzzle, n].texture;
                    }
                }
            }

            public void loadPuzzlergroups() //load puzzlergroups (using a random selection for tile thumbnail)
            {
                Texture2D texture;
                Random rand = new Random();
                for (int n = 0; n < theGame.puzzes.GetLength(0); n++)
                {
                    if (n == theGame.cyopID) //last puzzler is cyop, handle it differently
                    {
                        texture = theGame.loadIsolatedTexture(theGame.puzzes[n, 0].texture);
                        if (texture != null) { theGame.tex[n] = texture; }
                        else { theGame.tex[n] = theGame.Content.Load<Texture2D>(theGame.puzzes[n, 0].texture); }
                        //System.Diagnostics.Debug.WriteLine("reloading mypuzzle texture");
                    }
                    else
                    {
                        theGame.tex[n] = theGame.Content.Load<Texture2D>(theGame.puzzes[n, rand.Next(0, theGame.puzzes.GetLength(1))].texture);
                    }
                }
            }

            bool checkPuzzle() //check tile placement
            {
                int n = 0;
                for (int c = 0; c < swidth / tsize; c++)
                {
                    for (int r = 0; r < sheight / tsize; r++)
                    {
                        if (position[n] != new Vector2((tsize*c), (tsize*r))) //any misplaced tiles?
                        {
                            return false;
                        }
                        n++;
                    }
                }
                return true;
            }

            bool checkTile(int tile) //check tile placement
            {
                int n = 0;
                for (int c = 0; c < swidth / tsize; c++)
                {
                    for (int r = 0; r < sheight / tsize; r++)
                    {
                        if (n == tile)
                        {
                            if (position[n] != new Vector2((tsize * c), (tsize * r))) //any misplaced tiles?
                            {
                                return false;
                            }
                        }
                        n++;
                    }
                }
                return true;
            }

            public void scrambleTiles()
            {
                Random rand = new Random();
                int rnum = rand.Next(0, tnum);
                for (int n = 0; n < tnum; n++)
                {
                    int nr = rand.Next(n, tnum);

                    Vector2 t1 = position[n];
                    Vector2 t2 = position[nr];

                    position[n] = t2;
                    position[nr] = t1;
                }
            }

            public void touchTiles(float px, float py)
            {
                        //check touch to see if it coincides with tile placement

                        for (int k = 0; k < tnum; k++) // check thru the tiles
                        {

                            if ((px >= position[k].X) && (px <= (position[k].X + tsize)) &&
                                (py >= position[k].Y) && (py <= (position[k].Y + tsize)))
                            {
                                if (tile1 == k) { tile1 = -1; break; } //uncheck a tile

                                if (tile1 >= 0) { tile2 = k; } //tile1 set, set tile2
                                else //set tile2
                                {
                                    if (theGame.puzzlers.play)
                                    {
                                        this.Stop();
                                        theGame.puzzle_easy.load_puzzle = k;
                                        theGame.puzzle_hard.load_puzzle = k;
                                        theGame.Easy.Play(true);
                                        theGame.Hard.Play(true);
                                        break;
                                    }
                                    else if (theGame.puzzlergroups.play)
                                    {
                                        this.Stop();
                                        /*if (k == theGame.puzzes.GetLength(0) - 1)
                                        {
                                            theGame.puzzle_easy.load_puzzle = k;
                                            theGame.puzzle_hard.load_puzzle = k;
                                            theGame.Easy.Play(true);
                                            theGame.Hard.Play(true);
                                            theGame.puzzlers.load_puzzle = -1;
                                        }*/
                                        //else {
                                        theGame.puzzlers.Play(k, false);// }
                                        break;
                                    }
                                    else if (theGame.puzzle_easy.play || theGame.puzzle_hard.play)
                                    {
                                        if (!puzzle_finished) //stop doing this when puzzle is complete
                                        {
                                            tile1 = k;
                                            break;
                                        }
                                    }
                                }



                                if ((tile1 >= 0) && (tile2 >= 0))
                                {
                                    Vector2 t1 = position[tile1];
                                    Vector2 t2 = position[tile2];


                                    position[tile1] = t2;
                                    position[tile2] = t1;

                                    tile1 = -1;
                                    tile2 = -1;

                                    puzzle_finished = checkPuzzle();
                                    
                                    break;
                                }
                            }
                        }//end check thru tiles
            }

            public void drawPuzzle()
            {
                if (theGame.puzzle_easy.play || theGame.puzzle_hard.play) //actual game puzzle is loaded?
                {
                    for (int t = 0; t < tnum; t++) // tiles
                    {
                        if (tile1 == t) { theGame.spriteBatch.Draw(theGame.tex[load_puzzle], position[t], rect[t], Color.SteelBlue); continue; }
                       
                        if (checkTile(t)) //is tile in the correct location?
                        {
                            theGame.spriteBatch.Draw(theGame.tex[load_puzzle], position[t], rect[t], Color.White); 
                        }
                        else
                        {
                            theGame.spriteBatch.Draw(theGame.tex[load_puzzle], position[t], rect[t], Color.Coral);
                            theGame.spriteBatch.Draw(theGame.border, position[t], new Rectangle(0,0,tsize,tsize), Color.Coral);
                        }
                    }

                    if (show_photo > -1.0f)
                    {
                        float alpha = Math.Abs(((float)theGame.gametimesec - show_photo) / 5);
                        theGame.spriteBatch.Draw(theGame.tex[load_puzzle], new Vector2(0, 0), Color.White * alpha);
                        if (alpha < .02f) { show_photo = -1; }
                    }

                    if (puzzle_finished)
                    {
                        

                        if (fx_fireworks > -1.0f)
                        {
                            float alpha = Math.Abs(((float)theGame.gametimesec - fx_fireworks) / 5);
                            theGame.spriteBatch.Draw(theGame.fireworks, new Vector2(0, 0), Color.White * alpha);
                            if (alpha < .02f) { fx_fireworks = -1; }
                        }

                         //check difficulty level, display the winner splash
                        if (theGame.puzzle_hard.play)
                        {
                            //theGame.spriteBatch.Draw(theGame.fireworks, new Vector2(0, 0), null, Color.White);
                            theGame.spriteBatch.Draw(theGame.gold_medal, new Vector2(560, 0), null, Color.White, 0f, Vector2.Zero, 2.5f, SpriteEffects.None, 1.0f);
                        }
                        else
                        {
                            //theGame.spriteBatch.Draw(theGame.fireworks, new Vector2(0, 0), null, Color.White);
                            theGame.spriteBatch.Draw(theGame.silver_medal, new Vector2(560, 0), null, Color.White, 0f, Vector2.Zero, 2.5f, SpriteEffects.None, 1.0f);
                        }

                        //theGame.writeOut("Congratulations!",new Vector2(320,80),2.5f);
                        if (!theGame.medal_saved)
                        {
                            bool found = false;
                            bool md1 = false, md2 = false;

                            //check difficulty level
                            if (theGame.puzzle_hard.play)
                            {
                                md2 = true; //award a gold
                            }
                            else //easy game
                            {
                                md1 = true; //award a silver
                            }

                            foreach (var medal in theGame.medals)
                            {
                                //System.Diagnostics.Debug.WriteLine("texture name "+ theGame.tex[load_puzzle].Name);
                                if (medal.name == theGame.tex[load_puzzle].Name)
                                { //found a puzzle with an existing medal?

                                    //check existing medals
                                    if (medal.silver) { md1 = true; }
                                    if (medal.gold) { md2 = true; }

                                    theGame.medals.Add(new Medals(theGame.tex[load_puzzle].Name, md1, md2)); 
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                theGame.medals.Add(new Medals(theGame.tex[load_puzzle].Name, md1,md2)); 
                            }

                            theGame.medal_saved = true;
                            theGame.writeMedals();
                        }
                    }//end if puzzlefinished
                    else
                    {
                        fx_fireworks = 5 + (float)theGame.gametimesec;
                    }
                }
                else if (theGame.puzzlers.play) //puzzlers being shown?
                {
                    for (int n = 0; n < theGame.total_puzzlers; n++) // puzzlers
                    {
                        theGame.spriteBatch.Draw(theGame.tex[n], position[n], new Rectangle(160, 0, 480, 480), Color.White, 0f, Vector2.Zero, .33f, SpriteEffects.None, 0f);//.20f scale
                        foreach (var medal in theGame.medals)
                        {
                            if (medal.name == theGame.tex[n].Name) //have a medal?
                            {
                                if (medal.gold)
                                {
                                    theGame.spriteBatch.Draw(theGame.gold_medal, position[n], null, Color.White); 
                                }
                                if (medal.silver)
                                {
                                    theGame.spriteBatch.Draw(theGame.silver_medal, position[n], null, Color.White);
                                }
                            }
                        }
                    }
                }
                else if (theGame.puzzlergroups.play) //puzzlergroups being shown?
                {
                    for (int n = 0; n < theGame.puzzes.GetLength(0); n++) // puzzlergroups
                    {
                        theGame.spriteBatch.Draw(theGame.tex[n], position[n], new Rectangle(160, 0, 480, 480), Color.White, 0f, Vector2.Zero, .33f, SpriteEffects.None, 0f);//.20f scale
                        if (n == theGame.cyopID) 
                        { 
                            theGame.spriteBatch.Draw(theGame.border, position[n], null, Color.BlueViolet); 
                            theGame.spriteBatch.Draw(theGame.cyop_texture, position[n], null, Color.White); 
                        }
                    }
                }
                else //in menus?
                {
                    //
                }
            }
        
            public void Play(int puzzle, bool scramble) 
            {
                play = true;

                if (puzzle >= 0) 
                {
                    if (theGame.puzzlergroups.play)
                    {

                        
                    }
                    if (theGame.puzzlers.play)
                    {
                        if (puzzle < theGame.puzzes.GetLength(0)) { load_puzzle = puzzle; }
                        else { this.Stop(); theGame.puzzlergroups.Play(-1,false); return; }
                    }
                    else { load_puzzle = puzzle; }
                   
                }

                if (theGame.puzzlergroups.play) { theGame.puzzlergroups.loadPuzzlergroups(); }
                else if (theGame.puzzlers.play) { theGame.puzzlers.loadPuzzlers(); }

                if (scramble) { scrambleTiles(); }
                
            }
            public void Stop()
            {
                puzzle_finished = false;
                theGame.writeOutClear();
                play = false;
            }

            public Tiles(int size, Game1 myGame) //consutructor for class
            {
                tsize = size;
                theGame = myGame;
                tnum = (swidth / tsize) * (sheight / tsize);
                rect = new Rectangle[tnum];
                position = new Vector2[tnum];

                //builds puzzle tiles and bounds
                int n = 0;
                for (int c = 0; c < swidth / tsize; c++)
                {
                    for (int r = 0; r < sheight / tsize; r++)
                    {
                        position[n] = new Vector2((tsize * c), (tsize * r));
                        //phone screen coordinates are rotated into portrait
                        //coordinates start in top right of screen (portrait) and is rotated 90 degrees clockwise

                        rect[n] = new Rectangle((c * tsize), (r * tsize), (tsize), (tsize));
                        n++;
                    }

                }
            }
        }
        
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks(333333);

            // Extend battery life under lock.
            InactiveSleepTime = TimeSpan.FromSeconds(1);
        }



        private void bannerAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Ad error: " + e.Error.Message);
        }
        private void bannerAd_AdRefreshed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Ad received successfully");
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {

            // Create an actual ad for display.
            CreateAd();

            //show loading at launch
            Loading = new Item(this);
            Loading.setPos(250, 90);
            Loading.loadTexture("loading");
            Loading.PlayFade(loadingtime+1); //play a loading screen


            puzzle_easy = new Tiles(tsize_easy, this);
            puzzle_hard = new Tiles(tsize_hard, this);
            puzzlers = new Tiles(tsize_easy, this);
            puzzlergroups = new Tiles(tsize_easy, this);

            //difficulty menus
            Easy = new Item(this);
            Hard = new Item(this);
            Easy.setPos(200,140);
            Hard.setPos(420,140);

            //puzzle menus
            Info = new Item(this);
            Hint = new Item(this);
            Back = new Item(this);
            Info.setPos(640, 0);
            Hint.setPos(640, 160);
            Back.setPos(640, 320);

            //picture info
            PicInfo = new Item(this);
            PicInfo.setPos(160, 160);
            PicSave = new Item(this);
            PicSave.setPos(160, 320);

            //main menu options
            POTW = new Item(this);
            POTW.setPos(480, 0);
            CYOP = new Item(this);
            CYOP.setPos(640, 160);
            About = new Item(this);
            About.setPos(640, 0);
            AboutInfo = new Item(this);
            AboutInfo.setPos(0, 0);
            Logo = new Item(this);
            Logo.setPos(320, 0);

            Banner = new Item(this);
            Banner.setPos(320,320);


            puzzles = new string[total_puzzlers];
            tex = new Texture2D[total_puzzlers];

            readPuzzles();
            readMedals();


            base.Initialize();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Viewport viewport = graphics.GraphicsDevice.Viewport;

                /*CourierNew = Content.Load<SpriteFont>("Courier New");
                ForegroundBatch = new SpriteBatch(graphics.GraphicsDevice);
                FontPos = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
                FontRotation = 0;*/



            border = Content.Load<Texture2D>("border");
            cyop_texture = Content.Load<Texture2D>("cyop");
            gold_medal = Content.Load<Texture2D>("gold_medal");
            silver_medal = Content.Load<Texture2D>("silver_medal");
            fireworks = Content.Load<Texture2D>("fireworks");
            //loading = Content.Load<Texture2D>("loading");

            Easy.loadTexture("puzzle_ex_easy");
            Hard.loadTexture("puzzle_ex_hard");
            Info.loadTexture("info");
            Hint.loadTexture("search");
            Back.loadTexture("rewind");
            PicInfo.loadTexture("websearch");
            PicSave.loadTexture("info");
            //POTW.loadTexture("mypuzzle");
            CYOP.loadTexture("film_roll");
            About.loadTexture("info");
            AboutInfo.loadTexture("info");
            Logo.loadTexture("logo");
            
            Banner.setSize(480, 160);
            puzzlergroups.Play(0, false); //kick off game at this point (puzzlergroups showing)

            CYOP.Play(true);
            POTW.Play(true);
            Logo.Play(true);
            Banner.Play(true);


        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        void CreatePuzzle(object sender, PhotoResult e)
        {
            string file = "mypuzzle";

            if (e.TaskResult == TaskResult.OK)
            {
                //System.Diagnostics.Debug.WriteLine(e.ChosenPhoto.Length.ToString());

                // Create a virtual store and file stream. Check for duplicate tempJPEG files.
                var myStore = IsolatedStorageFile.GetUserStoreForApplication();
                if (myStore.FileExists(file))
                {
                    //System.Diagnostics.Debug.WriteLine("deleting existing file");
                    myStore.DeleteFile(file);
                }
                //System.Diagnostics.Debug.WriteLine(myStore.);
                IsolatedStorageFileStream myFileStream = myStore.CreateFile(file);

                // Create a new WriteableBitmap object and set it to the JPEG stream.
                BitmapImage bitmap = new BitmapImage();
                bitmap.CreateOptions = BitmapCreateOptions.None;
                bitmap.SetSource(e.ChosenPhoto);
                WriteableBitmap wb = new WriteableBitmap(bitmap);
                WriteableBitmap wbTarget = new WriteableBitmap(bitmap.PixelHeight, bitmap.PixelWidth);

                // Encode the WriteableBitmap object to a JPEG stream.
                if (bitmap.PixelHeight > bitmap.PixelWidth) //need to rotate the image then
                {
                    for (int x = 0; x < wb.PixelWidth; x++)
                    {
                        for (int y = 0; y < wb.PixelHeight; y++)
                        {
                            //wbTarget.Pixels[(wb.PixelHeight - y - 1) + x * wbTarget.PixelWidth] = wb.Pixels[x + y * wb.PixelWidth]; //90
                            wbTarget.Pixels[y + (wb.PixelWidth - x - 1) * wbTarget.PixelWidth] = wb.Pixels[x + y * wb.PixelWidth];//-90/270
                        }
                    }
                    wb = wbTarget;
                    //System.Diagnostics.Debug.WriteLine("rotating image");
                }

                wb.SaveJpeg(myFileStream, swidth, sheight, 0, 95);

                //texture = Texture2D.FromStream(this.GraphicsDevice, myFileStream);
                myFileStream.Close();

                tex[(puzzes.GetLength(0) - 1)] = loadIsolatedTexture(file);
                puzzlergroups.Play(0, false);//reload the puzzlergroups
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                puzzlergroups.Play(-1, false);
                //System.Diagnostics.Debug.WriteLine(e.TaskResult);
            } //exiting before choosing a photo?
            
        }

        Texture2D loadIsolatedTexture (string file)
        {
            Texture2D texture;
            // Create a virtual store and file stream
            var myStore = IsolatedStorageFile.GetUserStoreForApplication();



                if (myStore.FileExists(file))
                {
                    IsolatedStorageFileStream myFileStream = myStore.OpenFile(file, FileMode.Open);
                    try
                    {
                        texture = Texture2D.FromStream(this.GraphicsDevice, myFileStream);
                    }
                    catch
                    {
                        //System.Diagnostics.Debug.WriteLine("unable to load texture");
                        texture = null;
                    }

                    myFileStream.Close();
                }
                else { texture = null; }

                return texture;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            //if hitting back, do stuff related to the current screen that is being played
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) 
            {
                if (Loading.play) { } //do nothing in loading screen
                else
                {
                    if (puzzle_easy.play || puzzle_hard.play)
                    {
                        if (!Info.play) //all 3 play at the same time
                        {
                            Info.Play(true);
                            Hint.Play(true);
                            Back.Play(true);

                            bannerAd2.Visible = true;
                        }
                        else
                        {
                            Info.Play(false);
                            Hint.Play(false);
                            Back.Play(false);

                            bannerAd2.Visible = false;
                        }
                    }
                    else if (PicInfo.play)
                    {
                        PicInfo.Play(false);
                        puzzle_easy.show_photo = -1;
                        if (easy) { puzzle_easy.play = true; }
                        puzzle_hard.show_photo = -1;
                        if (hard) { puzzle_hard.play = true; }
                    }
                    else if (puzzlers.play) //inside a puzzle group looking at puzzlers?
                    {
                        puzzlers.Stop();
                        puzzlergroups.Play(-1, false);
                    }
                    else if (puzzlergroups.play) { this.Exit(); }
                    else if (PicInfo.play) { puzzle_easy.play = true; puzzle_hard.play = true; }
                    else if (Easy.play || Hard.play)//in a choose difficulty menu?
                    {
                        if (puzzlers.load_puzzle < 0) { puzzlergroups.Play(-1, false); } //this if from CYOP needing to back way out
                        else { puzzlers.Play(-1, false); }
                        Easy.Play(false);
                        Hard.Play(false);
                        easy = false;
                        hard = false;

                        bannerAd2.Visible = false;
                    }
                    else if (AboutInfo.play)
                    {
                        puzzlergroups.Play(-1, false); /*backout to main screen..turn on*/
                        AboutInfo.Play(false); //turn me off
                        CYOP.Play(true);
                        About.Play(true);
                        writeOutClear();
                    }
                }
            } //done pressing back on phone



            //grab current gametime
            gametimesec = currentGametime(gameTime);



            // Process touch events
            TouchCollection touchCollection = TouchPanel.GetState();
            foreach (TouchLocation tl in touchCollection)
            {
                if ((tl.State == TouchLocationState.Pressed))//|| (tl.State == TouchLocationState.Moved))
                {
                    float px = tl.Position.X; //touch x
                    float py = tl.Position.Y; //touch y

                    //process touch here
                    if (Loading.play) { break;  }//in loading screen? don't process touch!
                    else
                    {
                        if (Easy.play) //easy and hard difficulty chooser menu should play at same time
                        {
                            if (Easy.Touch(px, py))
                            {
                                puzzle_easy.Play(puzzle_easy.load_puzzle, true);
                                puzzle_easy.show_photo = 5 + (float)gametimesec;
                                Easy.Play(false);
                                Hard.Play(false);
                                bannerAd2.Visible = false;
                                easy = true;
                            }
                            else if (Hard.Touch(px, py))
                            {
                                puzzle_hard.Play(puzzle_hard.load_puzzle, true);
                                puzzle_hard.show_photo = 5 + (float)gametimesec;
                                Hard.Play(false);
                                Easy.Play(false);
                                bannerAd2.Visible = false;
                                hard = true;
                            }

                            //bannerAd2.Visible = true;
                            break;
                        }
                        else if (PicInfo.play)
                        {//dilapidated
                            /*
                            if (PicInfo.Touch(px, py))
                            {
                                WebBrowserTask task = new WebBrowserTask();
                                task.Uri = new Uri(puzzes[puzzlers.load_puzzle, puzzle_easy.load_puzzle].website, UriKind.Absolute);
                            

                                puzzle_easy.show_photo = -1;
                                puzzle_hard.show_photo = -1;
                                PicInfo.Play(false);
                                if (easy) { puzzle_easy.play = true; }//reshow puzzle (browser will then hide it)
                                if (hard) { puzzle_hard.play = true; }

                                writeOut("Loading website, please wait...", FontPos, FontScale); //this should only show if there is serious slowness.

                                try
                                {
                                    task.Show(); //show browser, we we come back to puzzle it will be showing, not picinfo
                                }
                                catch
                                {
                                    //report this error..
                                }
                                writeOutClear(); //clear our text on screen
                            }*/
                        }
                        else if (Info.play)//info,hint,and back should play at same time
                        {
                            if (Info.Touch(px, py))
                            {
                                if (puzzlers.load_puzzle < 0) //cyop handle this differently
                                {
                                    break;
                                }
                                /*else
                                {
                                    //PicInfo.setText(puzzes[puzzlers.load_puzzle, puzzle_easy.load_puzzle].artist, 160, 140, 1.0f);// + "\n"+ 
                    
                                }*/
                                // puzzes[puzzlers.load_puzzle,puzzle_easy.load_puzzle].website);


                                if (puzzle_easy.play) { puzzle_easy.play = false; } //don't stop puzzle just hide it
                                if (puzzle_easy.play) { puzzle_hard.play = false; }
                                //PicInfo.Play(true);

                                Info.Play(false);
                                Hint.Play(false);
                                Back.Play(false);


                                bannerAd2.Visible = false;

                                WebBrowserTask task = new WebBrowserTask();
                                task.Uri = new Uri(puzzes[puzzlers.load_puzzle, puzzle_easy.load_puzzle].website, UriKind.Absolute);


                                puzzle_easy.show_photo = -1;
                                puzzle_hard.show_photo = -1;
                                //PicInfo.Play(false);
                                if (easy) { puzzle_easy.play = true; }//reshow puzzle (browser will then hide it)
                                if (hard) { puzzle_hard.play = true; }

                                //writeOut("Loading website, please wait...", FontPos, FontScale); //this should only show if there is serious slowness.

                                try
                                {
                                    task.Show(); //show browser, we we come back to puzzle it will be showing, not picinfo
                                }
                                catch
                                {
                                    //report this error..
                                }
                            }
                            else if (Hint.Touch(px, py))
                            {
                                if (puzzle_easy.play) { puzzle_easy.show_photo = 5 + (float)gametimesec; }
                                else if (puzzle_hard.play) { puzzle_hard.show_photo = 5 + (float)gametimesec; }
                                Info.Play(false);
                                Hint.Play(false);
                                Back.Play(false);


                                bannerAd2.Visible = false;
                            }
                            else if (Back.Touch(px, py))
                            {

                                Loading.PlayFade(loadingtime);
                               // System.Diagnostics.Debug.WriteLine("backing out fader");


                                if (puzzlers.load_puzzle < 0) { puzzlergroups.Play(-1, false); } //this if from CYOP needing to back way out
                                else { puzzlers.Play(-1, false); }

                                puzzle_easy.Stop();
                                puzzle_hard.Stop();
                                Info.Play(false);
                                Hint.Play(false);
                                Back.Play(false);
                                medal_saved = false;
                                bannerAd2.Visible = false;

                            }
                            else // pressing anything but these will close menu
                            {
                                Info.Play(false);
                                Hint.Play(false);
                                Back.Play(false);


                                bannerAd2.Visible = false;
                            }
                            break;
                        } //end in-game menu
                        else
                        {
                            if (puzzle_easy.play) { puzzle_easy.touchTiles(px, py); }
                            else if (puzzle_hard.play) { puzzle_hard.touchTiles(px, py); }
                            else if (puzzlers.play) { Loading.PlayFade(loadingtime); puzzlers.touchTiles(px, py); }
                            else if (puzzlergroups.play)
                            {
                                if (CYOP.Touch(px, py))
                                {
                                    PhotoChooserTask pickphoto = new PhotoChooserTask();

                                    About.Play(false);
                                    Logo.Play(false);
                                    puzzlergroups.Stop(); //stop this screen

                                    try
                                    {
                                        pickphoto.Show();
                                    }
                                    catch (System.InvalidOperationException ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine(ex);
                                        puzzlergroups.Play(-1, false);
                                        break;
                                    }
                                    pickphoto.Completed += new EventHandler<PhotoResult>(CreatePuzzle);
                                }
                                else if (About.Touch(px, py))
                                {
                                    //AboutInfo.Play(true);
                                    puzzlergroups.Stop(); //stop this screen
                                    CYOP.Play(false);
                                    About.Play(false);
                                    Logo.Play(false);
                                    //string stuff = "Image Puzzler. This software is brought to you by Junco, LLC. JuncoApps.com";
                                    //writeOut(stuff, new Vector2(400, 160), 1.5f);

                                    WebBrowserTask task = new WebBrowserTask();
                                    string devicename = DeviceStatus.DeviceName;
                                    long devicemem = DeviceStatus.ApplicationCurrentMemoryUsage;


                                    //get device info and convert to hex string
                                    object DeviceUniqueID;
                                    byte[] DeviceIDbyte = null;

                                    if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out DeviceUniqueID))
                                    {
                                        DeviceIDbyte = (byte[])DeviceUniqueID;
                                    }

                                    string DeviceID = Convert.ToBase64String(DeviceIDbyte);
                                    string post = DeviceID + "," + devicename + "," + devicemem;
                                    string hex = "";
                                    foreach (char c in post)
                                    {
                                        int tmp = c;
                                        hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
                                    }
                                    //end convert


                                    task.Uri = new Uri("http://www.juncoapps.com/animalscrambler?d=" + hex, UriKind.Absolute);

                                    try
                                    {
                                        task.Show(); //show browser, we we come back to puzzle it will be showing, not picinfo
                                    }
                                    catch
                                    {
                                    }

                                    puzzlergroups.Play(-1, false);
                                    CYOP.Play(true);
                                    About.Play(true);
                                    Logo.Play(true);
                                }
                                else if (Logo.Touch(px, py))
                                {
                                    //puzzlers.Play(15, true); //scramble up the puzzles for visual fun
                                    break;
                                }
                                else if (Banner.Touch(px, py))
                                {
                                    break;
                                }
                                else
                                {
                                    Loading.PlayFade(loadingtime);
                                    Update(gameTime);
                                    BeginDraw(); //force redraw of screen this is important, instead of waiting around for it to occur
                                    Draw(gameTime); //force redraw of screen this is important, instead of waiting around for it to occur
                                    EndDraw();
                                    //System.Diagnostics.Debug.WriteLine("puzzlergroups fader");

                                    puzzlergroups.touchTiles(px, py);
                                }
                            }
                        }
                    }

                }//end if pressed
            } //end touch foreach
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            
            if (AdGameComponent.Initialized) {AdGameComponent.Current.Draw(gameTime);}
            
            spriteBatch.Begin();

            if (Loading.play) { Loading.PlayFade(-1); Loading.Display();} //loading takes precedence
            else
            {
                if (puzzle_easy.play) { puzzle_easy.drawPuzzle(); bannerAd.Visible = false; }
                else if (puzzle_hard.play) { puzzle_hard.drawPuzzle(); bannerAd.Visible = false; }
                else if (puzzlers.play) { puzzlers.drawPuzzle(); CYOP.Play(false); About.Play(false); bannerAd.Visible = false; Logo.Play(false); Banner.Play(false); }
                else if (puzzlergroups.play) { puzzlergroups.drawPuzzle(); CYOP.Play(true); About.Play(true); bannerAd.Visible = true; Logo.Play(true); Banner.Play(true); }
                //else if (AboutInfo.play) { System.Diagnostics.Debug.WriteLine("about info"); }
                /*else //possible blackhole?
                {
                    if (lostGametime < 0) { lostGametime = gametimesec; }
                    else if (gametimesec - lostGametime > 2.5) { lostGametime=-1; puzzlergroups.Play(0,false); }
                    //System.Diagnostics.Debug.WriteLine(gametimesec - lostGametime);
                }*/

                Easy.Display();
                Hard.Display();
                Info.Display();
                Hint.Display();
                Back.Display();
                PicInfo.Display();
                CYOP.Display();
                POTW.Display();
                About.Display();
                AboutInfo.Display();
                Logo.Display();
                Banner.Display();
            }

            AdGameComponent.Current.Draw(gameTime);

            spriteBatch.End();

            /*ForegroundBatch.Begin();

            // Find the center of the string
            Vector2 FontOrigin = CourierNew.MeasureString(output) / 2;
            // Draw the string
            ForegroundBatch.DrawString(CourierNew, output, FontPos, Color.AntiqueWhite,
                FontRotation, FontOrigin, FontScale, SpriteEffects.None, 10f);

            ForegroundBatch.End();*/

            base.Draw(gameTime);
        }//end draw

        private void CreateAd()
        {
            int width = 480;
            int height = 80;
            AdGameComponent.Initialize(this, applicationId);
            AdGameComponent.Current.CountryOrRegion = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
            Components.Add(AdGameComponent.Current);
            bannerAd = AdGameComponent.Current.CreateAd(adUnitId, new Rectangle(320, 360, width, height), true);

            bannerAd2 = AdGameComponent.Current.CreateAd(adUnitId, new Rectangle(160, 360, width, height), true);
            bannerAd2.Visible = false;

            // Add handlers for events (optional).
            //bannerAd.ErrorOccurred += new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(bannerAd_ErrorOccurred);
            //bannerAd.AdRefreshed += new EventHandler(bannerAd_AdRefreshed);
        }

    }
}
