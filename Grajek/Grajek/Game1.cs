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
using System.ComponentModel;

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
        private BackgroundWorker worker = new BackgroundWorker();
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
        private Rectangle nagrywanieStopButton = new Rectangle(196, 390, 180, 80);
        private Rectangle odtwarzanieButton = new Rectangle(382, 390, 180, 80);
        private Rectangle edycjaButton = new Rectangle(610, 390, 180, 80);

        private Rectangle aButton = new Rectangle(10, 390, 144, 80);
        private Rectangle bButton = new Rectangle(160, 390, 144, 80);
        private Rectangle delButton = new Rectangle(310, 390, 144, 80);
        private Rectangle acButton = new Rectangle(460, 390, 144, 80);
        private Rectangle backButton = new Rectangle(640, 390, 144, 80);

        private List<Klawisz> ListaNagrania; // Lista zapamiêtuje kolejno naciskane klawisze. Pos³u¿y do nagrywania
        int j = 0;
        private bool record = false; // Obs³u¿yæ ten przycisk !! Jako trigger true/false
        private bool playing = false;
        private bool played = false;
        private bool recorded = false;
        private bool edit = false;
        private bool endedit = true;
        private float timer; // Timer do odliczenia jak d³ugo by³ naciœniêty przycisk

        private bool press = false; //pomocniczy
        private float timerStart =0;
        private float timerEnd = 0;
        
        
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

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

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
            // played = false;
            MouseState ms = Mouse.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            wykryjNrAktualnegoKlawisza(ms);

            if (ms.LeftButton == ButtonState.Pressed)
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
                
                played = true;
            }
            else if (ms.LeftButton == ButtonState.Released)
            {
                
                synchronizator.NoteOff(aktualnyNrNuty);
                resetujKlawisz(aktualnyNrNuty);
                
                if (nagrywanieButton.Contains(new Point(ms.X, ms.Y)) && record == false)
                {
                    recorded = true;
                    ListaNagrania.Clear();
                    record = true;
                    played = true;

                }
                if (nagrywanieStopButton.Contains(new Point(ms.X, ms.Y)))
                {
                    if (recorded)
                    {
                        playing = false;
                        recorded = false;
                    }

                    record = false;

                }

                if (odtwarzanieButton.Contains(new Point(ms.X, ms.Y)) && record == false && recorded == false)
                {
                    Play();
                    //recorded = true;
                    // playing = false;
                }

                if ( edycjaButton.Contains(new Point(ms.X, ms.Y)))
                {
                    edit = true;
                    endedit = false;
                }

                if (edit == true && endedit== false)
                {
                    int wskaznik = 0;
                    if (aButton.Contains(new Point(ms.X, ms.Y)))
                    {
                       // 1. Naciskam A
                       // 2. Biore z listyNagran nr klawisza
                       // 3. Odgrywam go poki nie puszcze A
                       // 4. Wpisuje w 1 miejsce listy, taki sam klawisz ze zmienionym czasem trwania
                        
                    }

                    if (bButton.Contains(new Point(ms.X, ms.Y)))
                    {
                        // To samo co w A, ale kolejny z kolei czyli 2 klawisz
                    }

                    if (delButton.Contains(new Point(ms.X, ms.Y)))
                    {
                        int a = ListaNagrania.Count;
                        int poz = -1;
                        if (a > 0)
                        {
                            for (int i = 0; i < a; i++)
                            {
                                if (ListaNagrania[i].nr_klawisza != 0)
                                    poz = i;
                            }
                            if (poz > -1) 
                                ListaNagrania.RemoveAt(poz);       
                        }
                    }

                    if (acButton.Contains(new Point(ms.X, ms.Y)))
                    {
                        ListaNagrania.Clear();
                    }

                    if (backButton.Contains(new Point(ms.X, ms.Y)))
                    {
                        //endedit = true; // tak jakby endedit == (edit = false)
                    }
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

            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                playing = true;
                // played = true;
            }

            if (!played)
                playing = false;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Worker nr: " + (j++));
            if (played)
            {
                for (int i = 0; i < ListaNagrania.Count(); i++)
                {
                    System.Threading.Thread.Sleep((int)ListaNagrania[i].oczekiwanie); // Czas oczekiwanie miêdzy naciœnieciem kolejnych klawiszy.
                    synchronizator.NoteOn(ListaNagrania[i].nr_klawisza); // zaczyna graæ
                    nacisnietoKlawisz(ListaNagrania[i].nr_klawisza);
                    System.Threading.Thread.Sleep((int)ListaNagrania[i].czas); // czeka "czas" - powinien przez ca³y ten czas graæ.
                    synchronizator.NoteOff(ListaNagrania[i].nr_klawisza); //koñczy graæ
                    resetujKlawisz(ListaNagrania[i].nr_klawisza);
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            played = false;
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
            {
                sprawdzKlawisz[nrNuty] = true;
            }
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
            if (edit == false || (endedit == true && edit == true))
            {
                if (record == true) spriteBatch.Draw(tekstura, nagrywanieButton, null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 1);
                if (record == false) spriteBatch.Draw(tekstura, nagrywanieButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "Record", new Vector2(55, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, nagrywanieStopButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "Stop", new Vector2(255, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                if (playing == true) spriteBatch.Draw(tekstura, odtwarzanieButton, null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 1);
                if (playing == false) spriteBatch.Draw(tekstura, odtwarzanieButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "Play", new Vector2(445, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, edycjaButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "EDIT", new Vector2(670, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }
            if ( edit == true && endedit == false )
            {
                spriteBatch.Draw(tekstura, aButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "A", new Vector2(75, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, bButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "B", new Vector2(225, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, delButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "Del", new Vector2(360, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, acButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "AC", new Vector2(515, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

                spriteBatch.Draw(tekstura, backButton, null, Color.Snow, 0, Vector2.Zero, SpriteEffects.None, 1);
                spriteBatch.DrawString(Font1, "BACK", new Vector2(680, 410), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
