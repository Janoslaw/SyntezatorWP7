using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;

namespace Grajek
{
    struct Klawisz // Struktura dla listy nagrywania
    {
        public int nr_klawisza;
        public float czas;
        public float oczekiwanie;
    }

    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont Font1;
        private Texture2D tekstura;
        private Vector2 pozycjaKlawiszy;
        private Vector2 rozmiarKlawiszy;
        private int aktualnyNrNuty = 0;
        private bool[] sprawdzKlawisz;
        private List<Vector2> listaKlawiszy = new List<Vector2>();
        private Synth synchronizator;
        private Rectangle nagrywanieButton = new Rectangle(10, 390, 180, 80);
        private Rectangle nagrywanieStopButton = new Rectangle(210, 390, 180, 80);
        private Rectangle odtwarzanieButton = new Rectangle(420, 390, 360, 80);

        private List<Klawisz> ListaNagrania; // Lista zapamiêtuje kolejno naciskane klawisze. Pos³u¿y do nagrywania

        private bool record = false; // Obs³u¿yæ ten przycisk !! Jako trigger true/false
        private bool playing = false;
        private float timer; // Timer do odliczenia jak d³ugo by³ naciœniêty przycisk
        private float timer_k2k; // Timer key to key, czyli czas od puszczenia przycisku do naciœcniêcia kolejnego. Nieobs³u¿one jeszcze
        
        private bool press = false; //pomocniczy
        private enum OscillatorTypes
        {
            Sine,
            Triangle,
            Square,
            Sawtooth
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

        protected override void Initialize()
        {
            base.Initialize();

            synchronizator = new Synth();
            synchronizator.Oscillator = Oscillator.Square;

            synchronizator.FadeInDuration = 20;
            synchronizator.FadeOutDuration = 20;

            ListaNagrania = new List<Klawisz>();
        }
      
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //rysujemy klawisze
            tekstura = new Texture2D(GraphicsDevice, 1, 1);
            tekstura.SetData(new[] { Color.White });
            sprawdzKlawisz = new bool[13];
            for (int i = 0; i < 13; i++)
            {
                sprawdzKlawisz[i] = false;
            }

            pozycjaKlawiszy = new Vector2(0, 0);
            rozmiarKlawiszy = new Vector2(800, 380);

            Font1 = Content.Load<SpriteFont>("SpriteFont1");
        }

        protected override void UnloadContent() { }
       
        protected override void Update(GameTime gameTime)
        {
            MouseState ms = Mouse.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();     

            wykryjNrAktualnegoKlawisza(ms);

            if ( ms.LeftButton == ButtonState.Pressed)
            {                
                if (aktualnyNrNuty != 15)
                {
                    synchronizator.NoteOn(aktualnyNrNuty);
                    nacisnietoKlawisz(aktualnyNrNuty);
                }
                               
                if (record == true)
                {
                    timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds; //Rozpoczêcie naliczania "czasu przyciœciêcia klawisza"
                    press = true; // do nagrywania
                }
            }
            else if (ms.LeftButton == ButtonState.Released)
            {
                
                synchronizator.NoteOff(aktualnyNrNuty); 
                resetujKlawisz(aktualnyNrNuty);

                if (nagrywanieButton.Contains(new Point(ms.X, ms.Y)) && record == false)
                {
                    ListaNagrania.Clear();
                    record = true;
                }
                if (nagrywanieStopButton.Contains(new Point(ms.X, ms.Y)) && record == true)
                {
                    record = false;
                }
                
                if (odtwarzanieButton.Contains(new Point(ms.X, ms.Y)))
                {
                    //Debug.WriteLine("X: " + ms.X + " Y: " + ms.Y);
                    Play();
                }

                if (record == true && press == true)
                {
                    //Lista kolejno naciskanych klawiszy razem z czasem naciskania; Nie mo¿na naciskaæ kilku na raz !!!
                    Klawisz kl = new Klawisz();
                    kl.nr_klawisza = aktualnyNrNuty;
                    kl.czas = timer;
                    kl.oczekiwanie = 0;// tu nale¿y wstawiæ i obs³u¿yæ timer_k2k.

                    ListaNagrania.Add(kl);
                    Debug.WriteLine("Klawisz nr: " + kl.nr_klawisza + "|Oczekiwanie: " + kl.oczekiwanie + "|Czas: " + kl.czas);
                }

                press = false; //rec
                timer = 0;     //rec
            }
            

            synchronizator.Update(gameTime);
            base.Update(gameTime);
        }

        private void Play() // Odgrywanie zapamiêtanej piosenki
        {
            playing = true;
            
            for (int i = 0; i < ListaNagrania.Count(); i++) 
            {
                System.Threading.Thread.Sleep((int)ListaNagrania[i].oczekiwanie) ; // Czas oczekiwanie miêdzy naciœnieciem kolejnych klawiszy.
                synchronizator.NoteOn(ListaNagrania[i].nr_klawisza); // zaczyna graæ
                nacisnietoKlawisz(ListaNagrania[i].nr_klawisza);
                System.Threading.Thread.Sleep((int)ListaNagrania[i].czas); // czeka "czas" - powinien przez ca³y ten czas graæ.
                synchronizator.NoteOff(ListaNagrania[i].nr_klawisza); //koñczy graæ
                resetujKlawisz(ListaNagrania[i].nr_klawisza);

                // Trzeba zrobiæ przycisk "record" oraz "play" w record nale¿y czyœciæ ListeNagarania, oraz ustawiaæ "bool record" na true albo false triggerem.
            }

            playing = false;
        }

        protected void wykryjNrAktualnegoKlawisza(MouseState poz)
        {
            if (poz.X < 130 && poz.X > 70 && poz.Y < 190)
                aktualnyNrNuty = 1;
            else if (poz.X < 100 && poz.Y < 360)
                aktualnyNrNuty = 0;
            else if (poz.X < 230 && poz.X > 170 && poz.Y < 190)
                aktualnyNrNuty = 3;
            else if (poz.X < 201 && poz.Y < 360)
                aktualnyNrNuty = 2;
            else if (poz.X < 430 && poz.X > 370 && poz.Y < 190)
                aktualnyNrNuty = 6;
            else if (poz.X < 301 && poz.Y < 360)
                aktualnyNrNuty = 4;
            else if (poz.X < 401 && poz.Y < 360)
                aktualnyNrNuty = 5;
            else if (poz.X < 530 && poz.X > 470 && poz.Y < 190)
                aktualnyNrNuty = 8;
            else if (poz.X < 501 && poz.Y < 360)
                aktualnyNrNuty = 7;
            else if (poz.X < 630 && poz.X > 570 && poz.Y < 190)
                aktualnyNrNuty = 10;
            else if (poz.X < 601 && poz.Y < 360)
                aktualnyNrNuty = 9;
            else if (poz.X < 701 && poz.Y < 360)
                aktualnyNrNuty = 11;
            else if (poz.X < 800 && poz.Y < 360)
                aktualnyNrNuty = 12;
            else
                aktualnyNrNuty = 15;
        }

        protected void nacisnietoKlawisz(int nrNuty)
        {
            if (nrNuty == 15)
            {
                for (int i = 0; i < 13; i++)
                {
                    sprawdzKlawisz[i] = false;
                }
            }
            else if (sprawdzKlawisz.Length > nrNuty)
                sprawdzKlawisz[nrNuty] = true;
        }

        protected void resetujKlawisz(int nrNuty)
        {
            if (sprawdzKlawisz.Length > nrNuty)
                sprawdzKlawisz[nrNuty] = false;
        }
       
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            int szerokoscBialego = (int)(rozmiarKlawiszy.X / 8);
            int szerokoscCzarnego = (int)(szerokoscBialego / 1.6f);

            for (int i = 0; i < 13; i++)
            {
                int m = (i / 12);
                int n = i % 12;

                // White Note
                if (n == 0 || n == 2 || n == 4 || n == 5 || n == 7 || n == 9 || n == 11)
                {
                    if (n == 2)
                    {
                        n = 1;
                    }
                    else if (n == 4)
                    {
                        n = 2;
                    }
                    else if (n == 5)
                    {
                        n = 3;
                    }
                    else if (n == 7)
                    {
                        n = 4;
                    }
                    else if (n == 9)
                    {
                        n = 5;
                    }
                    else if (n == 11)
                    {
                        n = 6;
                    }
                    Vector2 klawisz = new Vector2(pozycjaKlawiszy.X + (n + m * 7) * szerokoscBialego + 1, pozycjaKlawiszy.Y + 1);

                    listaKlawiszy.Add(klawisz);


                    spriteBatch.Draw(tekstura, new Rectangle((int)System.Math.Round(klawisz.X - 1),
                        (int)System.Math.Round(klawisz.Y - 1), szerokoscBialego, (int)System.Math.Round(rozmiarKlawiszy.Y)), null, Color.Black,
                        0.0f, Vector2.Zero, SpriteEffects.None, 1.0f);

                    spriteBatch.Draw(tekstura, new Rectangle((int)System.Math.Round(klawisz.X),
                        (int)System.Math.Round(klawisz.Y), szerokoscBialego - 2, (int)System.Math.Round(rozmiarKlawiszy.Y) + 2), null,
                        sprawdzKlawisz[i] ? Color.Aqua : Color.White, 0.0f, Vector2.Zero, SpriteEffects.None, 0.9f);
                }
                // Black Key
                else
                {
                    if (n == 1)
                    {
                        n = 0;
                    }
                    else if (n == 3)
                    {
                        n = 1;
                    }
                    else if (n == 6)
                    {
                        n = 3;
                    }
                    else if (n == 8)
                    {
                        n = 4;
                    }
                    else if (n == 10)
                    {
                        n = 5;
                    }

                    Vector2 klawisz = new Vector2(pozycjaKlawiszy.X + (n + m * 7) * szerokoscBialego + szerokoscBialego - szerokoscCzarnego / 2 + 1, pozycjaKlawiszy.Y + 1);

                    listaKlawiszy.Add(klawisz);

                    spriteBatch.Draw(tekstura, new Rectangle((int)System.Math.Round(klawisz.X - 1),
                        (int)System.Math.Round(pozycjaKlawiszy.Y), szerokoscCzarnego, (int)System.Math.Round(rozmiarKlawiszy.Y / 2)), null, Color.Black,
                        0.0f, Vector2.Zero, SpriteEffects.None, 0.8f);

                    spriteBatch.Draw(tekstura, new Rectangle((int)System.Math.Round(klawisz.X),
                       (int)System.Math.Round(pozycjaKlawiszy.Y) + 1, szerokoscCzarnego - 2, (int)System.Math.Round(rozmiarKlawiszy.Y / 2 - 2)), null,
                        sprawdzKlawisz[i] ? Color.Aqua : Color.Black, 0.0f, Vector2.Zero, SpriteEffects.None, 0.7f);
                }
            }

            if (record == true) spriteBatch.Draw(tekstura, nagrywanieButton, null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 1);
            if (record == false) spriteBatch.Draw(tekstura, nagrywanieButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
            spriteBatch.DrawString(Font1, "Record", new Vector2(55, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            spriteBatch.Draw(tekstura, nagrywanieStopButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
            spriteBatch.DrawString(Font1, "Stop", new Vector2(270, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            if (playing == true) spriteBatch.Draw(tekstura, odtwarzanieButton, null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 1);
            if (playing == false) spriteBatch.Draw(tekstura, odtwarzanieButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
            spriteBatch.DrawString(Font1, "Play", new Vector2(570, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None,0);
            
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
