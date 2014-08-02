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


using System.Diagnostics;

namespace crazycat
{


    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        GameObjects gameObjects = new GameObjects();

        string player = "player1";
        float player_speed = 2f;
        float player_damage = 2.5f;
        //bool dictLocked = false;
        double currentGametime = -1;
        double deltaGametime = -1; 
        int max_trees = 75;
        int map_size = 2000;
        double currentGametimeMsec = -1;
        double touchtimer1 = -1;
        double touchtimer2 = -1;
        Texture2D pixel;

        public class GameObjects
        {
            public Dictionary<string, objSprite> Sprites = new Dictionary<string, objSprite>();
            public void addSprite(objSprite sprite)
            {
                if (!Sprites.ContainsKey(sprite.name))
                {
                    Sprites.Add(sprite.name, sprite);
                    Debug.WriteLine("sprite added: " + sprite.name + " at " + sprite.position.X + "," + sprite.position.Y);
                }
                //else { Debug.WriteLine("sprite already exists, not adding new sprite"); }

                Debug.WriteLine("amount of sprite objects: " + Sprites.Count());
            }
            public bool removeSprite(objSprite sprite)
            {
                Sprites.Remove(sprite.name);
                return true;
            }


            public Dictionary<string, objSpawn> Spawns = new Dictionary<string, objSpawn>();
            public void addSpawn(objSpawn spawn)
            {
                if (!Spawns.ContainsKey(spawn.name)) { Spawns.Add(spawn.name, spawn); }
            }
            public void removeSpawn(objSpawn spawn)
            {
                Sprites.Remove(spawn.name);
            }
        }

        public class objSpawn
        {
            public bool respawn = false;
            int respawn_count = 1;
            public string name,sprite_name;//sprite_name is the name of the original sprite
            int x, y;
            public objSprite sprite; //used as a reference for the sprite obj information initially created for it
            Game1 theGame;
            
            public objSpawn(string _name, int _x, int _y, bool _r, int _rn, objSprite _sprite, Game1 _game)
            {
                name = _name;
                sprite_name = _sprite.name; // this is key, see above
                x = _x;
                y = _y;
                respawn = _r;
                if (_rn < 1) { _rn = 1; }
                respawn_count = _rn;
                theGame = _game;


                sprite = _sprite.Clone("spawn"); //make a clone to our own sprite obj, for later referencing during respawn

                sprite.name = "spawn";//reset specific values after clone
                sprite.spawner = name;
                Die(_sprite);//kill off original temp sprite
                theGame.gameObjects.addSprite(sprite); //this is just a reference sprite, it does not show up in the game

                Respawn();
            }

            public void debug(string what)
            {
                Debug.WriteLine(what);
            }
            public bool checkSpawn(objSprite obj)
            {
                if (obj.spawner == name)
                {
                    return true;
                }

                //Debug.WriteLine("not sprite " + name + "!="+sprite_name);
                return false;
            }

            public void Respawn() //spawn more sprite objects?
            {
                if (respawn_count < 1) { return; } //tracks how many spawned objects are currently in the game
                string newname = sprite_name;
                Random rand = new Random();
                if (sprite_name != theGame.player) { newname += rand.Next(); }//drum up a random namee based on this original sprites name
                
                int nx = rand.Next(x-120, x+120);
                int ny = rand.Next(y-90, y+90);
                objSprite _s = sprite.Clone(newname);
                _s.setPos(new Vector2(nx, ny));

                /*//find sprite obj attackers and fleers for other objects in game and update them, this may take a while to sort through
                foreach (var pair in theGame.gameObjects.Sprites)
                {
                    foreach (var obj in theGame.gameObjects.Sprites[pair.Key].myActions.AttackList)
                    {
                        if (obj.name == sprite_name)
                        {
                            obj.myActions.addAction(_s.myActions.AttackList,_s.
                        }
                    }

                }*/
                _s.spawner = name;
                theGame.gameObjects.addSprite(_s);
                _s.playAnim(_s.base_anim, true, false); //kick off animations

                respawn_count -= 1;
            }
            

            public void Die(objSprite obj)//don't use as primary method, see refresh.. this will kill off sprite
            {
                theGame.gameObjects.removeSprite(obj); //remove element
                if (respawn) { respawn_count += 1; } //are we a respawning spawn? then update count
            }
            

            public void Refresh(objSprite obj) //kills off sprite obj and potentially respawns a new one
            {
                //debug("refresh being called");
                //Die(obj);
                Respawn();
            }
        }

        public class Compass //headings are clockwise starting from north (0-7 integers)
        {
            public Dictionary<string, int> headings = new Dictionary<string, int>() //string headings
            {
                {"n",0},
                {"ne",1},
                {"e",2},
                {"se",3},
                {"s",4},
                {"sw",5},
                {"w",6},
                {"nw",7}
            };

            string heading;

            public void setHeading(string _heading)
            {
                if (headings.ContainsKey(_heading))
                {
                    heading = _heading;
                }
                else { Debug.WriteLine("no such heading "+_heading); }
            }
            public string getHeading()
            {
                return heading;
            }
        }

        public class objSprite
        {
            Game1 theGame;
            public bool attackable = true;
            public bool still = false;
            public string spawner = ""; //used by spawns to own their objects
            //public float x=0, y=0;//, mx=0,my=0;
            public Vector2 destination, position, direction, oldposition;
            public float speed = 4.0f,curr_speed=0;
            public int height=0, width=0;
            public float z = 0f;
            public Dictionary<string,Animation> Anims = new Dictionary<string,Animation>(); //create a list of animations
            public string name;
            public Compass compass = new Compass();
            public string base_anim; //what the sprite animation will default too (ie:standing)
            public bool delay=false;
            public bool move,attacking=false; //?
            //string move_type = "linear";
            string curr_anim;
            BoundingSphere bounds_sphere = new BoundingSphere();
            BoundingSphere bounds_sphere_sight = new BoundingSphere();
            Collision collider;
            public objActions myActions = new objActions();
            //float movetime=-1f;
            public float health = 100f;
            public float max_health = 100f;
            public float base_health = 100f;
            public float damage = 1.0f;
            public float attack_delay = .835f;
            public bool hidden = false;
            Color color = Color.White;
            public float scale = 1f;
            public float alpha = 1f;
            public float rotation = 0f;

            public objSprite Clone(string _name) //performs a copy, used by spawn to hold on to the original sprite object
            {
                objSprite obj = new objSprite(position.X, position.Y, _name, theGame);
                //obj.Anims = Anims;
                foreach (var pair in Anims)
                {
                    obj.addAnim(Anims[pair.Key].name, Anims[pair.Key].folder, false); //well see to it for base_anim later
                }

                obj.collider = collider;

                foreach (var ent in myActions.FleeList)
                {
                    obj.myActions.addAction(obj.myActions.FleeList, ent);
                }
                foreach (var ent in myActions.AttackList)
                {
                    obj.myActions.addAction(obj.myActions.AttackList, ent);
                }
                obj.myActions.attack = myActions.attack;
                obj.myActions.running = myActions.running;
                obj.myActions.die = myActions.die;
                obj.myActions.hurt = myActions.hurt;
                obj.myActions.autochase = myActions.autochase;
                

                obj.compass = compass;
                obj.base_anim = base_anim; //check for base_anim and set it
                obj.base_health = base_health;
                obj.health = base_health;
                obj.max_health = max_health;
                obj.damage = damage;
                obj.attack_delay = attack_delay;
                obj.hidden = hidden;
                obj.bounds_sphere.Radius = bounds_sphere.Radius;
                obj.bounds_sphere_sight.Radius = bounds_sphere_sight.Radius;
                obj.height = height;
                obj.width = width;
                obj.speed = speed;
                obj.curr_anim = curr_anim;
                obj.resetParent(obj);
                obj.scale = scale;
                obj.color = color;
                obj.spawner = spawner;
                obj.alpha = alpha;
                //obj.position = position;
                return obj;// (objSprite)this.MemberwiseClone();
            }
            public objSprite(float _x, float _y, string _name, Game1 _game)
            {
                //x = _x;
                //y = _y;

                position = new Vector2(_x, _y);
                destination = new Vector2(_x, _y);
                direction = new Vector2(180f);//not needed here but hey
                oldposition = position;

                //width = 128;
                //height = 128;
                theGame = _game;
                name = _name;
                compass.setHeading("n"); //default heading

                
                bounds_sphere_sight.Radius = 96; //give it a default sight for now
                bounds_sphere.Radius = 36;//texture size can help figure this out automatically, see setbounds

                syncBounds();


                initCollider(false);
            }

            public void resetParent(objSprite sprite)//used by spawn, i wouldn't need this if i passed it every time :-\
            {
                Debug.WriteLine("resetting parent to: " + sprite.name + " from: " + name);
                foreach (var pair in Anims)
                {
                    Anims[pair.Key].theSprite = sprite;
                    Anims[pair.Key].theGame = sprite.theGame;
                }

                collider.theSprite = sprite;
                theGame = sprite.theGame;

               
            }

            public void setHealth(float _h,bool _s,bool _b,bool _m) //one must be true for this to operate
            {
                if (_b) { base_health = _h; } //need to set base health?
                if (_m) { max_health = _h; } //need to set max health?
                if (_s) { health = _h; } //need to set health?
            }
            
            public class objActions
            {
                public bool autochase = true;
                public objSprite chasing=null;
                objSprite attacking = null;
                double timer; //pause timer
                double pause;

                //public bool respawn = false;
                public string running="running", attack="attack", hurt="been_hit",die="tipping_over"; //animations to be played

                public List<objSprite> FleeList = new List<objSprite>();
                public List<objSprite> AttackList = new List<objSprite>();

                public List<objSprite> AttackersList = new List<objSprite>();
                public List<objSprite> ChasersList = new List<objSprite>();

                public bool addAction(List<objSprite> list, objSprite obj)
                {
                    if (!list.Contains(obj))
                    { 
                        list.Add(obj);
                        //if (list == FleeList) { running = anim; }
                        //else { attack = anim; }

                        //autochase = chase;
                        timer = obj.theGame.currentGametime;
                        return true;
                    }
                    else { Debug.WriteLine("obj already has action against, not adding obj to actions"); }
                    return false;
                }

                public void runActions(objSprite me, objSprite ent)
                {
                    if (ent.name == "spawn") { return; }
                    if (ent.still) { return; }
                    if (me.still) { return; }
                    if (ent.health < 0) { return; } //don't mess with dead objects
                    //if (me.health < 0) { return; } //im dead, stop performing actions then
                    if (me.name == me.theGame.player) { Attack(me, ent); } //player attacks everything attackable

                    

                    foreach (var obj in FleeList)
                    {
                        if (obj.name == me.theGame.gameObjects.Spawns[ent.spawner].sprite_name)
                        {
                            Flee(me, ent);
                        }
                    }
                    foreach (var obj in AttackList)
                    {
                        if (!ent.attackable) { return; }
                        if (obj.name == me.theGame.gameObjects.Spawns[ent.spawner].sprite_name)
                        {
                            Attack(me, ent);
                        }
                    }
                }

                void Flee(objSprite me, objSprite ent)
                {
                    if (me.myActions.checkPause(me)) { return; }

                    if (me.delay) { return; }
                    if (me.move) { return; }
                    me.attacking = false;
                    if (ent.hidden) { return; } //can't find it if it's now hidden

                    Random rand = new Random();
                    //use larger dist for difficulties

                    int x = (int) me.position.X+(me.width/2);
                    int y = (int) me.position.Y+(me.height/2);
                    int dist = 300;
                    me.moveTo(rand.Next(x - dist, x + dist), rand.Next(y - dist, y + dist), running); //kick off moveTo function, begin the animation sequence
                }

                public void Chase(objSprite me, objSprite ent)
                {
                    if (ent.health < 0) { return; }
                    if (ent.still) { return; }

                    if (ent.hidden)
                    {
                        if (ent.myActions.ChasersList.Contains(me)) { chasing.myActions.ChasersList.Remove(me); }
                        return;
                    } //can't find it if it's now hidden

                    //deac attacking
                    me.attacking = false;
                    if (attacking != null) { if (attacking.myActions.AttackersList.Contains(me)) { attacking.myActions.AttackersList.Remove(me); } }
                    attacking = null;

                    me.hidden = false;

                    //update chaser info
                    if (chasing != null) { if (ent != chasing) { chasing.myActions.ChasersList.Remove(me); } }//remove old info
                    chasing = ent; //update to new chased ent
                    if (!ent.myActions.ChasersList.Contains(me)) { ent.myActions.ChasersList.Add(me); } //this if is likely unnecessary

                    me.moveTo(ent.position.X + (ent.width / 2), ent.position.Y+(ent.height / 2), running); //kick off moveTo function, begin the animation sequence
                    float dist = Vector2.Distance(me.position, ent.position);
                    if (dist < me.bounds_sphere.Radius) { Attack(me, ent); }
                    else if (dist > (me.bounds_sphere_sight.Radius*2))//too great? we lost them?
                    {
                        if (ent.myActions.ChasersList.Contains(me)) { chasing.myActions.ChasersList.Remove(me); }
                        chasing = null;
                    }
                    else //still chasing?
                    {
                        //if (ent.name == me.theGame.player) { Debug.WriteLine(me.name + " chasing player! "+dist); } //its stuck here
                    }
                }

                void Attack(objSprite me, objSprite ent)
                {
                    if (!ent.attackable){return;}
                    if (me.myActions.checkPause(me)) { return; }
                    
                    if (me.attacking) { return; } //wait for attacking to finish

                    if (Vector2.Distance(me.position, ent.position) < me.bounds_sphere.Radius) //close enough to attack? //this should probably be in collider
                    {
                        if (me.name == me.theGame.player) 
                        { 
                            me.speed = me.theGame.player_speed;
                            if (chasing != null) { me.damage = me.theGame.player_damage * 2; Debug.WriteLine("crit hit!"); }
                        }

                        //update attack info
                        if (attacking != null) { if (ent != attacking) { attacking.myActions.AttackersList.Remove(me); } } //remove old attack info
                        attacking = ent;
                        if (!ent.myActions.AttackersList.Contains(me)) { ent.myActions.AttackersList.Add(me); }

                        //deac chaser info
                        if (chasing != null) { if (ent.myActions.ChasersList.Contains(me)) { ent.myActions.ChasersList.Remove(me); } }
                        chasing = null; 

                        me.hidden = false;
                        ent.hidden = false;

                        me.curr_anim = attack;
                        Vector2 dir = ent.position - me.position;
                        me.setDirection(dir);

                        me.move = false; //no movement during an attack
                        me.attacking = true;
                        
                        Debug.WriteLine(me.name + " hit " + ent.name + " health at: "+ent.health);

                        ent.health -= me.damage;


                        if (ent.health < 0) { ent.myActions.Die(ent); me.attacking = false; attacking = null; }
                        else
                        {
                            ent.playAnim(hurt, true, false);
                            if (ent.name != me.theGame.player) { ent.move = false; }
                            if (ent.health / ent.max_health < .5) { 
                                ent.color = Color.Wheat;
                                if (ent.name != ent.theGame.player) { ent.speed -= (ent.speed * .25f); } //reduce speed by a percentage
                            }
                            if (ent.health / ent.max_health < .25) 
                            {
                                ent.color = Color.DarkGray;
                                if (ent.name != ent.theGame.player) { ent.speed -= (ent.speed * .5f); } //additional speed reduction
                            }
                            ent.myActions.setPause(0.825f, me.theGame.currentGametime); //stun the other guy
                        }
                        me.myActions.setPause(me.attack_delay, me.theGame.currentGametime); //attack delay for next attack
                        if (me.name == me.theGame.player) { me.damage = me.theGame.player_damage; } //reset damage
                        //need to kick off damage and scurry gfx here
                    }
                    else //get over to it!
                    {
                        //if (ent.myActions.AttackersList.Contains(me)) { ent.myActions.AttackersList.Remove(me); attacking = null; }
                        if (autochase) { Chase(me, ent); }
                    }
                }

                bool checkPause(objSprite me)
                {
                    if (me.theGame.currentGametime - timer < pause) { return true; }
                    return false;
                }
                void setPause(double _t, double _p) { timer = _t; pause = _p; }

                public void Idle(objSprite me)
                {
                    if (me.myActions.chasing != null) { Chase(me, chasing); return; }

                    me.attacking = false;


                    if (me.myActions.checkPause(me)) { return; }
                    if (me.still) { return; }
                    //if (me.health < 0) { return; }
                    if (me.delay) { return; }
                    if (me.Anims.ContainsKey(attack)) {
                        if (me.Anims[attack].play) { me.attacking = true; return; } //still attacking?
                        else
                        {
                            if (attacking != null)
                            {
                                if (me.myActions.attacking.myActions.AttackersList.Contains(me))
                                {
                                    me.myActions.attacking.myActions.AttackersList.Remove(me);
                                }
                                attacking = null;
                            }
                        }
                    }

                    if (me.move) { return; } //not idle during movement
                    if (me.name == me.theGame.player) { me.playAnim(me.base_anim, false, false); return; }

                    if (me.health / me.max_health > .5) { me.color = Color.White; }
                    if (me.health / me.max_health > .25) { me.color = Color.Wheat; }

                    Random rand = new Random();
                    int ri = rand.Next(0, 50);
                    if (ri == 0)
                    {
                        ri = rand.Next(-75, 75);
                        int ri2 = rand.Next(-75, 75);
                        ri += (int)me.position.X + (me.width / 2);
                        ri2 += (int)me.position.Y + (me.height / 2);
                        me.moveTo(ri, ri2, running); //kick off moveTo function, begin the animation sequence
                    }
                    else { me.playAnim(me.base_anim, false, false); }
                }

                public void Die(objSprite me)
                {
                    //me.playAnimOnce(die); //this plays the death anim and then stops the obj sprite
                    me.playAnim(die,true,false);
                    me.attackable = false;
                    me.color = Color.DarkSlateGray;
                    //to add or remove entities we must do this way outside the iteration, see initial iteration down at Update Game
                }


                public void setFleeAnim(string _flee)
                {
                    running = _flee;
                }

                public void setHurtAnim(string _anim)
                {
                    hurt = _anim;
                }

                public void setDieAnim(string _anim)
                {
                    die = _anim;
                }

                public void setAttackAnim(string _attack)
                {
                    attack = _attack;
                }
            }

            /*public void setBounds(string anim_name, bool resize) //auto set bounds and size
            {
                int size=0;
                try
                {
                    size = Anims[anim_name].ntex[0].Width;
                }
                catch { setBounds(anim_name, resize); } //recursively try and make this work, if called correctly this will finish properlys

                setBounds(size, resize);
                setSight(size);
                //Debug.WriteLine(name + " finished setting bounds to "+size);
            }*/
            public void setBounds(int size, bool resize) //manually set bounds and size
            {
                bounds_sphere.Radius = size / 3; //3 works as a good number..
                setSight(size/3);
                if (resize)
                {
                    width = size;
                    height = size;
                }
            }

            public void syncBounds()
            {
                bounds_sphere.Center.X = position.X +((width * scale) / 2)-(bounds_sphere.Radius/2);
                bounds_sphere.Center.Y = position.Y + ((height * scale) / 2) - (bounds_sphere.Radius/2);

                bounds_sphere_sight.Center.X = position.X + (width / 2) - (bounds_sphere_sight.Radius / 2);
                bounds_sphere_sight.Center.Y = position.Y + (height / 2) - (bounds_sphere_sight.Radius / 2);
            }

            /*public void setPos(float _x, float _y)
            {
                x = _x-(width/2);
                y = _y-(height/2);
                syncBounds();
            }*/
            public void setPos(Vector2 pos)
            {
                //x = pos.X - (width / 2);
                //y = pos.Y - (height / 2);
                position = pos;
                syncBounds();
            }
            /*
            public void setPos(float _x, float _y, string heading)
            {
                x = _x - (width / 2);
                y = _y - (height / 2);
                syncBounds();

                if (heading != null) { compass.setHeading(heading); }
            }*/

            public void setZ(float _z)
            {
                z = _z; //sets depth for drawing
            }

            public void setSight(int size)
            {
                bounds_sphere_sight.Radius = size;
            }


            public void Sight(bool delay, Game1.GameObjects objects)
            {
                //if (delay) { return; } // wondering if delaying these functions is necessary
                foreach (var pair in objects.Sprites)
                {
                    //Debug.WriteLine(objects.Sprites[pair.Key].name);
                    if (objects.Sprites[pair.Key] == this) { continue; } // don't sight yourself
                    if (bounds_sphere_sight.Radius < 1) { continue; } //no sight set? skip me
                    if (bounds_sphere_sight.Intersects(objects.Sprites[pair.Key].bounds_sphere))
                    {
                        //Debug.WriteLine(objects.Sprites[pair.Key].name + " sighted by " + name);
                        if (!objects.Sprites[pair.Key].hidden) { myActions.runActions(this, objects.Sprites[pair.Key]); }
                    }
                }
            }


            public void initCollider(bool impassible)
            {
                collider = new Collision(this,impassible);
            }
            public objSprite checkCollider(bool delay, Game1.GameObjects objects, GameTime Time)
            {
                if (collider != null) { return collider.Check(delay, objects, Time); } //if i have a collider
                return null;
            }

            public objSprite checkCollider(float cx, float cy, Game1.GameObjects objects)
            {
                if (collider != null) { return collider.Check(cx, cy, objects); } //if i have a collider
                return null;
            }

            class Collision
            { //todo: add rectangular checks for non-spherical objects
                public objSprite theSprite;
                bool impassible = false; //used for static objects (wall,tree)
                //float weight; //one day this will be helpful

                public Collision(objSprite sprite, bool _impassible) { theSprite = sprite; impassible = _impassible; }

                public objSprite Check(float cx, float cy, Game1.GameObjects objects)
                {
                    BoundingSphere sphere = new BoundingSphere();
                    sphere.Center.X = cx;
                    sphere.Center.Y = cy;
                    sphere.Radius = 36f;

                    foreach (var pair in objects.Sprites)
                    {
                        if (objects.Sprites[pair.Key].collider == null) { continue; } //does the other sprite even have a collider?
                        if (objects.Sprites[pair.Key] == theSprite) { continue; } // don't collide with yourself
                        if (sphere.Intersects(objects.Sprites[pair.Key].bounds_sphere)) //collision happened?
                        {
                            return objects.Sprites[pair.Key];
                        }
                    }
                    return null;
                }

                public objSprite Check(bool delay, Game1.GameObjects objects, GameTime Time)
                {
                    //if (delay) { return; } //don't check during a delay/lull
                    if (theSprite.bounds_sphere.Radius == 0) { return null; } // do i not have a big enough collision box? skip me //dilapidated?
                    if (impassible) { return null; } //don't bother checking from myself if I'm impassible static object

                    if (theSprite.name == theSprite.theGame.player) { theSprite.hidden = false; }
                    objSprite ent = null;
                    foreach (var pair in objects.Sprites)
                    {
                        if (objects.Sprites[pair.Key].collider == null) { continue; } //does the other sprite even have a collider?
                        if (objects.Sprites[pair.Key] == theSprite) { continue; } // don't collide with yourself
                        if (theSprite.bounds_sphere.Intersects(objects.Sprites[pair.Key].bounds_sphere)) //collision happened?
                        {
                            theSprite.myActions.runActions(theSprite, objects.Sprites[pair.Key]);
                            ent = objects.Sprites[pair.Key];

                            if (theSprite.name == theSprite.theGame.player) //the player can hide in foliage
                            {
                                
                                if (objects.Sprites[pair.Key].still)
                                {
                                    if ((theSprite.myActions.AttackersList.Count() < 1) && (theSprite.myActions.ChasersList.Count() < 1))
                                    {
                                        if ((!theSprite.attacking) && (theSprite.myActions.chasing == null)) { theSprite.hidden = true; }
                                        break;
                                    }
                                } //should use a foliage/hider marker instead
                                else
                                {
                                    //objects.Sprites[pair.Key].speed = theSprite.theGame.player_speed;
                                    theSprite.hidden = false;
                                    break;
                                }
                            }

                            if (objects.Sprites[pair.Key].collider.impassible)  //we just ran in to a static object
                            {
                                float cx = theSprite.position.X;
                                theSprite.position.X = theSprite.oldposition.X;
                                theSprite.syncBounds();

                                if (theSprite.bounds_sphere.Intersects(objects.Sprites[pair.Key].bounds_sphere))//test for old x
                                {
                                    theSprite.position.X = cx;
                                    float cy = theSprite.position.Y;
                                    theSprite.position.Y = theSprite.oldposition.Y;
                                    theSprite.syncBounds();

                                    if (theSprite.bounds_sphere.Intersects(objects.Sprites[pair.Key].bounds_sphere))//test for old y
                                    {
                                        theSprite.position = theSprite.oldposition; //reset to old position if both x and y fail
                                        theSprite.syncBounds();
                                    }
                                }

                                break;
                            }//end impassible
                            else
                            {
                                break;

                            }

                            
                        }
                    }

                    //if (ent != null) { theSprite.curr_speed = theSprite.speed / 2; if (theSprite.name == theSprite.theGame.player) { Debug.WriteLine(theSprite.name + " hit " + ent.name); } }
                    //else { 
                    theSprite.curr_speed = theSprite.speed;
                    // }

                    return ent;
                }
            }

            public void setDirection(Vector2 dir) //sets direction and plays animation
            {
                dir.Normalize();

                //set heading here
                string ns, ew;
                if (dir.X > 0) { ew = "e"; }
                else { ew = "w"; }

                if (dir.Y > 0) { ns = "s"; }
                else { ns = "n"; }

                float a = .05f;
                float ia = .95f;
                //east is 1, west is -1, south is 1, north is -1
                if (Math.Abs(dir.X) < a) { ew = ""; } //allow for nsew direct pointing
                else if (Math.Abs(dir.Y) > ia) { ew = ""; }

                if (Math.Abs(dir.Y) < a) { ns = ""; }
                else if (Math.Abs(dir.X) > ia) { ns = ""; }

                compass.setHeading(ns + ew);

                playAnim(curr_anim, true, false);
            }

            public void Move(GameTime Time)
            {
                //if (delay) { return; }
                if (!move) { return; } //not even moving? exit function
                if (attacking) { move = false; return; } //stop for an attack

                /*if (name == theGame.player)
                {
                    if (hidden) { speed = theGame.player_speed / 2; }
                    else { speed = theGame.player_speed; }
                }*/

                direction = destination - position;
                oldposition = position; //save current position as previous position

                position += direction * (curr_speed * (float)Time.ElapsedGameTime.TotalSeconds);

                //x =  position.X;
                //y = position.Y;
                syncBounds();
                
                

                if (Vector2.Distance(position,destination)<(bounds_sphere.Radius/3)) //close enough? stop moving
                { 
                    move = false;
                    //position = Vector2.Zero; position.X = oldposition.X; position.Y = oldposition.Y;

                    //end our movement frame on a good note:
                    playAnim(base_anim, false, false); //stop on base animation, first frame

                    //if (name == theGame.player) { Debug.WriteLine((float)Time.TotalGameTime.TotalMilliseconds - movetime); }
                    //movetime = -1;
                }


                setDirection(direction); //sets direction and plays animation
            }


            public void moveTo(float _x, float _y, string _anim)
            { //this function preps the Move function

                float dx = _x - (width / 2);
                float dy = _y - (height / 2);

                if (dx < 0) { dx = 40; }
                if (dy < 0) { dy = 40; }

                if (dx > theGame.map_size) { dx = theGame.map_size - 40; }
                if (dy > theGame.map_size) { dy = theGame.map_size - 40; }

                //keep within screen
                /*if ((dx) > 800 + (width / 2)) { dx = 800 - (width / 2); }
                if ((dx) < 0 - (width / 2)) { dx = 0 - (width / 2); }
                if ((dy) > 480 + (height / 2)) { dy = 480 - (height / 2); }
                if ((dy) < 0 - (height / 2)) { dy = 0 - (height / 2); }*/

                destination = new Vector2(dx, dy);

                curr_anim = _anim;
                move = true; //kicks off trigger 
            }

            /*public void moveTo(Vector2 _dest, string _anim)
            {
                destination = _dest;
                curr_anim = _anim;
                move = true; //kicks off trigger
            }*/

            public void addAnim(string name, string folder, bool _base)
            {
                if (!Anims.ContainsKey(name)) //don't already have an animation listed the same do we?
                {
                    Animation anim = new Animation(name, folder, theGame, this);
                    Anims.Add(name, anim);
                    if (_base) { base_anim = name; curr_anim = name; }


                    width = anim.ntex[0].Width;
                    height = anim.ntex[0].Height;
                    //setBounds(name, true); //reset the bounds and size of the sprite according to texture
                }
                else { Debug.WriteLine("animation already exists, not adding animation"); }
            }

            public void addStill(string tex, string folder)//used to add static, non animated objects (ie tree)
            {
                addAnim(tex, folder, true);
                attackable = false;
                still = true;
                playAnim(tex, false, true);
            }

            /*
             * play_frames=true and loop=true, continue playing frames, looping it
             * play_frames=false and loop=true, freeze on first frame, static image
             * play_frames=true and loop=false, play the animation then pause it
             * for playing an animation and stopping it completely, set destruct to true
             */
            public void playAnim(string anim_name, bool play_frames, bool loop) 
            {
                if (anim_name == "") { return; }
                if (anim_name == null) { return; }
                //Debug.WriteLine(anim_name);
                if (Anims.ContainsKey(anim_name)) //do we even have this animation?
                {
                    Anims[anim_name].loop = loop;
                    foreach (var val in Anims)
                    {
                        if (Anims[val.Key].name != anim_name) { Anims[val.Key].Stop(); } // stop all other animations
                    }
                    Anims[anim_name].Play(compass.getHeading(), play_frames);
                }
                //else { Debug.WriteLine("unknown animation "+anim_name+" attempted to be played on " + name); }
            }

            public void playAnimOnce(string anim_name)
            {
                if (Anims.ContainsKey(anim_name)) { Anims[anim_name].destruct = true; }
                playAnim(anim_name, true,false);
            }

            public void Pause(string _anim) //pauses on the first texture of the animation specified
            {
                //Play(_anim,false);
                //play = false;
            }

            public void delayAnim(bool delay)
            {
                foreach (var pair in Anims)
                {
                    if (Anims[pair.Key].play) //is the animation even up and running?
                    {
                        if (delay) { Anims[pair.Key].Delay(true); }
                        else { Anims[pair.Key].Delay(false); }
                    }
                }
            }

            public void drawAnim()
            {
                foreach (var pair in Anims)
                {
                    //Debug.WriteLine("drawing: "+Anims[pair.Key].name +" of "+name);
                    Anims[pair.Key].Draw();
                }
            }

            public void Stop() //stops on the base-animation,first frame
            {
                foreach (var val in Anims)
                {
                    Anims[val.Key].Stop(); // stop all animations
                }
            }

            public class Animation
            {
                public List<Texture2D> ntex = new List<Texture2D>();
                public List<Texture2D> netex = new List<Texture2D>();
                public List<Texture2D> nwtex = new List<Texture2D>();
                public List<Texture2D> etex = new List<Texture2D>();
                public List<Texture2D> wtex = new List<Texture2D>();
                public List<Texture2D> stex = new List<Texture2D>();
                public List<Texture2D> setex = new List<Texture2D>();
                public List<Texture2D> swtex = new List<Texture2D>();

                /* 
                 * 
                 * texture sprites are 6-10 frames per compass direction per animation
                 * example: animation_name with compass_direction x frame number: running_e0007
                 * 
                */
                
                public string folder;
                public string name;
                public Game1 theGame;
                string heading;
                public bool play; //whether or not to play animation, false stops animation on first frame
                public bool visible; //whether or not to draw animation
                int frame=0; //consider a way to save the animation state
                bool delay=false;
                public bool loop=false; //looping this animation?
                public bool destruct; //set to true the animation will Stop after a single loop
                public objSprite theSprite;
                public bool base_anim;
                

                public Dictionary<string, List<Texture2D>> headings = new Dictionary<string, List<Texture2D>>();
                /*{
                    {"n",ntex}
                };*/



                public void Play(string _heading, bool play_frames)
                {
                    if (headings.ContainsKey(_heading)) //likely don't need to check here, but what the hay
                    {
                        heading = _heading;
                        play = play_frames;
                        visible = true;
                    }
                    else
                    {
                        Debug.WriteLine("no such heading "+heading);
                    }
                }

                public void Stop()
                {
                    visible = false;
                    play = false;
                }
                public void Pause(bool _pause)
                {
                    if (_pause) { play = false; theSprite.Pause(theSprite.base_anim); }
                    else { play = true; }
                }
                public void Delay(bool _delay)
                {
                    delay = _delay;
                }

                public void Visible()
                {
                    visible = !visible;
                }


                public void Draw()
                {
                    
                    if (visible)
                    {
                        if (play)
                        {
                            if (!delay)  //update frame to current iteration
                            {
                                if (frame < headings[heading].Count - 1)
                                {
                                    frame++;
                                } //loop frames to start of animation
                                else
                                {
                                    if (!loop) { Pause(true); } //not looping anim? pause it

                                    if (theSprite.health >= 0) { frame = 0; }
                                }
                            }

                        }


                        if ((Math.Abs(theSprite.position.X-theGame.gameObjects.Sprites[theGame.player].position.X) < 900) &&
                            (Math.Abs(theSprite.position.Y-theGame.gameObjects.Sprites[theGame.player].position.Y)< 480)) //within screen, show it
                        {
                            Vector2 Origin = theSprite.position + new Vector2(336, 176);//get our offset position
                            Origin -= theGame.gameObjects.Sprites[theGame.player].position; //recalc it in regards to player position

                            Vector2 Bounds = new Vector2(theSprite.bounds_sphere.Center.X, theSprite.bounds_sphere.Center.Y) + new Vector2(336, 176);
                            Bounds -= theGame.gameObjects.Sprites[theGame.player].position;

                            if (theSprite.still)
                            {
                                theGame.spriteBatch.Draw(ntex[0], Origin, null, theSprite.color * theSprite.alpha, theSprite.rotation, new Vector2((theSprite.width / 2) - (theSprite.bounds_sphere.Radius), (theSprite.height / 2) - -(theSprite.bounds_sphere.Radius)), theSprite.scale, SpriteEffects.None, theSprite.z); //}
                            }
                            else //animated sprite?
                            {
                                //if (theSprite.hidden) { theSprite.color = Color.DeepPink; }
                                
                                theGame.spriteBatch.Draw(headings[heading][frame], Origin, null, theSprite.color * theSprite.alpha, 0f, Vector2.Zero, theSprite.scale, SpriteEffects.None, theSprite.z); //}
                            }
                            //check origin
                            //theGame.spriteBatch.Draw(theGame.pixel, Bounds, null, Color.Tomato, 0f, Vector2.Zero, theSprite.bounds_sphere.Radius, SpriteEffects.None, theSprite.z); //}
                            
                        }

                        if (!play && destruct) { theSprite.Stop(); }// } //shutdown entire obj sprite
                    }
                }

                public Animation(string _name, string _folder, Game1 _game, objSprite _sprite)
                {
                    //create association keys for the different texture sets
                    headings.Add("n", ntex);
                    headings.Add("ne", netex);
                    headings.Add("e", etex);
                    headings.Add("se", setex);
                    headings.Add("s", stex);
                    headings.Add("sw", swtex);
                    headings.Add("w", wtex);
                    headings.Add("nw", nwtex);

                    name = _name;
                    folder = _folder;
                    theGame = _game;
                    theSprite = _sprite;

                    for (int i = 0; i < 60; i++) //60 (arbitrary) animation frames max for each compass direction
                    {
                        try
                        {   //pull in textures and sort based on common strings
                            //format texture count
                            string n = "" + i;
                            if (i < 10) { n = "000" + i; }
                            else if (i < 100) { n = "00" + i; }
                            else if (i < 1000) { n = "0" + i; }

                            //Debug.WriteLine("adding textures (#"+i+") for " + name + " animation in " + folder + " folder");
                            ntex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "n" + n));
                            netex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "ne"  + n));
                            nwtex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "nw" + n));
                            etex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "e" + n));
                            wtex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "w" + n));
                            stex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "s" + n));
                            setex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "se" + n));
                            swtex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name + "_" + "sw" + n));
                        }
                        catch
                        {
                            //Debug.WriteLine("found all textures for " + name + " animation in " + folder + " folder");
                            try
                            { //try and set single static texture
                                if (ntex.Count() < 1) { ntex.Add(theGame.Content.Load<Texture2D>(folder + "/" + name)); Debug.WriteLine("added static tex " + name); break; } 
                            }
                            catch { break; }
                            break;
                        }
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

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            gameObjects.addSprite(new objSprite(336, 176, player, this));
            gameObjects.addSprite(new objSprite(200, 300, "rat", this));
            gameObjects.addSprite(new objSprite(400, 200, "mouse", this));
            gameObjects.Sprites["rat"].scale = .825f;
            gameObjects.addSprite(new objSprite(400, 200, "wolf", this));

            Random rand = new Random();
            for (int n = 1; n <= max_trees; n++)
            {
                gameObjects.addSprite(new objSprite(rand.Next(0, map_size), rand.Next(0, map_size), "tree" + n, this));
            }

            /*gameObjects.Sprites[player].initCollider(false); //setup colliders, this should be automatic
            gameObjects.Sprites["rat"].initCollider(false);
            gameObjects.Sprites["mouse"].initCollider(false);
            gameObjects.Sprites["wolf"].initCollider(false);*/

            gameObjects.Sprites["mouse"].setSight(150); //set up sight radius
            gameObjects.Sprites["wolf"].setSight(185); //set up sight radius
            
            gameObjects.Sprites["mouse"].speed = 3f;
            gameObjects.Sprites["rat"].speed = 1.8f;
            gameObjects.Sprites[player].speed = player_speed;
            gameObjects.Sprites["wolf"].speed = 2.4f;

            gameObjects.Sprites[player].setSight(0);
            gameObjects.Sprites["rat"].setHealth(15f, true, true, true);
            gameObjects.Sprites["mouse"].setHealth(5f,true,true,true);
            gameObjects.Sprites[player].setHealth(75f, true, true, true);

            gameObjects.Sprites["rat"].damage = 1.5f;
            gameObjects.Sprites["wolf"].damage = 2f;
            gameObjects.Sprites[player].damage = player_damage;
            gameObjects.Sprites[player].myActions.autochase = false;

            gameObjects.Sprites["mouse"].setZ(.02f);
            gameObjects.Sprites[player].setZ(.03f);
            gameObjects.Sprites["rat"].setZ(.05f);
            gameObjects.Sprites["wolf"].setZ(.05f);

            for (int n = 1; n <= max_trees; n++)
            {
                gameObjects.Sprites["tree"+n].setZ(.06f);
                gameObjects.Sprites["tree" + n].alpha = .72f;
                //gameObjects.Sprites["tree" + n].scale = 1.5f;
                //gameObjects.Sprites["tree" + n].rotation = rand.Next(0, 360);//rotation of syncbounds not working
            }

            pixel = Content.Load<Texture2D>("sprites/pixel");

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

            gameObjects.Sprites[player].addAnim("attack", "sprites/lion", true);
            gameObjects.Sprites[player].addAnim("running", "sprites/lion", false); 
            gameObjects.Sprites[player].addAnim("been_hit", "sprites/lion", false);
            gameObjects.Sprites[player].addAnim("tipping_over", "sprites/lion", false);

            gameObjects.Sprites["rat"].addAnim("attack", "sprites/rat", true);
            gameObjects.Sprites["rat"].addAnim("running", "sprites/rat", false);
            gameObjects.Sprites["rat"].addAnim("tipping_over", "sprites/rat", false);

            gameObjects.Sprites["wolf"].addAnim("attack", "sprites/wolf", true);
            gameObjects.Sprites["wolf"].addAnim("running", "sprites/wolf", false);
            gameObjects.Sprites["wolf"].addAnim("tipping_over", "sprites/wolf", false);

            gameObjects.Sprites["mouse"].addAnim("running", "sprites/mouse", true);
            gameObjects.Sprites["mouse"].addAnim("been_hit", "sprites/mouse", false);


            gameObjects.Sprites["rat"].myActions.addAction(gameObjects.Sprites["rat"].myActions.AttackList, gameObjects.Sprites[player]);
            gameObjects.Sprites["rat"].myActions.addAction(gameObjects.Sprites["rat"].myActions.FleeList, gameObjects.Sprites["wolf"]);

            gameObjects.Sprites["mouse"].myActions.addAction(gameObjects.Sprites["mouse"].myActions.FleeList, gameObjects.Sprites[player]);
            gameObjects.Sprites["mouse"].myActions.addAction(gameObjects.Sprites["mouse"].myActions.FleeList, gameObjects.Sprites["wolf"]);

            gameObjects.Sprites[player].myActions.addAction(gameObjects.Sprites[player].myActions.AttackList, gameObjects.Sprites["mouse"]);
            gameObjects.Sprites[player].myActions.addAction(gameObjects.Sprites[player].myActions.AttackList, gameObjects.Sprites["rat"]);
            gameObjects.Sprites[player].myActions.addAction(gameObjects.Sprites[player].myActions.AttackList, gameObjects.Sprites["wolf"]);

            gameObjects.Sprites["wolf"].myActions.addAction(gameObjects.Sprites["wolf"].myActions.AttackList, gameObjects.Sprites["mouse"]);
            gameObjects.Sprites["wolf"].myActions.addAction(gameObjects.Sprites["wolf"].myActions.AttackList, gameObjects.Sprites["rat"]);
            gameObjects.Sprites["wolf"].myActions.addAction(gameObjects.Sprites["wolf"].myActions.AttackList, gameObjects.Sprites[player]);

            gameObjects.Sprites[player].playAnim("running", true, false);
            //gameObjects.Sprites["rat"].playAnim("running", true, false);
            //gameObjects.Sprites["mouse"].playAnim("running", true, false);
            //gameObjects.Sprites["wolf"].playAnim("running", true, false);

            gameObjects.addSpawn(new objSpawn("mice", 50, 50, true, 5, gameObjects.Sprites["mouse"], this));
            gameObjects.addSpawn(new objSpawn("rats", 500, 250, true, 2, gameObjects.Sprites["rat"], this));
            gameObjects.addSpawn(new objSpawn("wolf", 500, 250, false, 1, gameObjects.Sprites["wolf"], this));
            gameObjects.addSpawn(new objSpawn(player, 336, 176, false, 1, gameObjects.Sprites[player], this));

            for (int n = 1; n <= max_trees; n++)
            {
                Random rand = new Random();

                gameObjects.Sprites["tree"+n].addStill("bush"+rand.Next(1,4), "sprites/trees");
                gameObjects.Sprites["tree" + n].syncBounds(); //important to have this here
            }
            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            string pushpop = "temp";

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //create a delay to wait on
            currentGametime = gameTime.TotalGameTime.TotalSeconds;
            currentGametimeMsec = gameTime.TotalGameTime.TotalMilliseconds;
            if (deltaGametime < 0) { deltaGametime = currentGametime; }
            bool delay;
            if (currentGametime - deltaGametime > .0833)
            {
                delay = false;
                deltaGametime = currentGametime;
            }
            else
            {
                delay = true;
                gameObjects.Spawns["mice"].Respawn();
                gameObjects.Spawns["rats"].Respawn();
                gameObjects.Spawns["wolf"].Respawn();
                gameObjects.Spawns[player].Respawn();
            }

            // perform functions on all sprites
            foreach (var pair in gameObjects.Sprites)
            {
                if (gameObjects.Sprites[pair.Key].name == "spawn") { continue; } //this is for spawn really
                if (gameObjects.Sprites[pair.Key].health < 0) { pushpop = gameObjects.Sprites[pair.Key].name; continue; }

                gameObjects.Sprites[pair.Key].delayAnim(delay);

                if (!gameObjects.Sprites[pair.Key].still)
                {
                    gameObjects.Sprites[pair.Key].Move(gameTime);
                    gameObjects.Sprites[pair.Key].Sight(delay, gameObjects); //check sight on all other bounds in game, this could be slow!
                    gameObjects.Sprites[pair.Key].checkCollider(delay, gameObjects, gameTime); //check all collisions
                    gameObjects.Sprites[pair.Key].myActions.Idle(gameObjects.Sprites[pair.Key]);
                }
            }

            if (pushpop != "temp")
            {
                //Debug.WriteLine("pushpop called");
                foreach (var pair in gameObjects.Spawns)
                {
                    if (gameObjects.Spawns[pair.Key].checkSpawn(gameObjects.Sprites[pushpop])) //found the spawn?
                    {
                        gameObjects.Spawns[pair.Key].Refresh(gameObjects.Sprites[pushpop]);
                    }
                }
            }



            // Process touch events
            TouchCollection touchCollection = TouchPanel.GetState();
            foreach (TouchLocation tl in touchCollection)
            {
                float px = tl.Position.X; //touch x
                float py = tl.Position.Y; //touch y

                


                float sx = gameObjects.Sprites[player].position.X;
                float sy = gameObjects.Sprites[player].position.Y;

                sx += (gameObjects.Sprites[player].width / 2);
                sy += (gameObjects.Sprites[player].height / 2);

                if (px > 400) { px = sx + (px - 400); }
                else { px = sx + (px - 400); }

                if (py > 240) { py = sy + (py - 240); }
                else { py = sy + (py - 240); }



                if ((tl.State == TouchLocationState.Pressed) && (tl.State != TouchLocationState.Moved)) //pressed and not holding
                {
                    gameObjects.Sprites[player].myActions.chasing = null;
                    gameObjects.Sprites[player].speed = player_speed;
                    if (touchtimer1 > 0) //touched already?
                    {
                        Debug.WriteLine(touchtimer1 + " timer1");
                        touchtimer2 = currentGametimeMsec; //update touch2
                        //Debug.WriteLine("tap two at: " + touchtimer2 + ":" + (touchtimer2 - touchtimer1));
                        if (touchtimer2 - touchtimer1 < 400) //within certain time?
                        {
                            //Debug.WriteLine("doubletap good! " + (touchtimer2 - touchtimer1));
                            Debug.WriteLine(Vector2.Distance(gameObjects.Sprites[player].position, new Vector2(px, py)));
                            if (Vector2.Distance(gameObjects.Sprites[player].position, new Vector2(px, py)) < 220) //within pounce?
                            {
                                

                                objSprite ent = gameObjects.Sprites[player].checkCollider(px, py, gameObjects);
                                if (ent != null)
                                {
                                    gameObjects.Sprites[player].speed = player_speed*2; //pounce speed
                                    Debug.WriteLine("pouncing " + ent.name);
                                    //gameObjects.Sprites[player].moveTo(ent.position.X, ent.position.Y, "running");
                                    gameObjects.Sprites[player].myActions.Chase(gameObjects.Sprites[player], ent);
                                    touchtimer1 = -1; //reset
                                }
                                else { Debug.WriteLine("nuttin there to pounce"); }

                            }
                        }
                        else { touchtimer1 = touchtimer2; gameObjects.Sprites[player].moveTo(px, py, "running"); Debug.WriteLine(touchtimer1 + " timer1 swapped to timer2"); } //swap timers


                        //touchtimer1 = -1; //reset
                    }
                    else
                    {
                        touchtimer1 = currentGametimeMsec;
                        Debug.WriteLine(touchtimer1 + " timer1 reset to new gametime");
                    }
                }
                else
                {
                    if ((tl.State == TouchLocationState.Pressed) || (tl.State == TouchLocationState.Moved)) //pressed or pressed and holding
                    {
                        gameObjects.Sprites[player].myActions.chasing = null;
                        gameObjects.Sprites[player].speed = player_speed;
                        //objSprite obj = gameObjects.Sprites[player].checkCollider(delay, gameObjects, gameTime);
                        if (!gameObjects.Sprites[player].attacking)
                        {
                            //Debug.WriteLine(px + "," + py);
                            gameObjects.Sprites[player].moveTo(px, py, "running");
                        }

                        touchtimer1 = currentGametimeMsec;
                    }
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Tan);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.FrontToBack,null);//SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.SaveState

            // draw all sprites & animations, this 'may' be VERY slow using lists within a dictionary
            foreach (var pair in gameObjects.Sprites)
            {
                gameObjects.Sprites[pair.Key].drawAnim();
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}


/*
notable math:

public Vector2 AngleToV2(float angle, float length)
{
    Vector2 direction = Vector2.Zero;
    direction.X = (float)Math.Cos(angle) * length;
    direction.Y = (float)Math.Sin(angle) * length;
    return direction;
}

public float V2ToAngle(Vector2 direction)
{
    return (float)Math.Atan2(direction.Y, direction.X);
} 
 
*/