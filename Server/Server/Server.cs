﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

using System.Collections;

namespace Game
{
    class Server
    {
        private static TcpListener listener;
        private static List<Player> activePlayers = new List<Player>();
        private static List<GameThread> games = new List<GameThread>();

        // private IPAddress local = IPAddress.Parse("127.0.0.1");

        private int port = 3445;                // Port is hard coded.
        private static LoginDatabase db;        
        protected static char delim = '%';
    

        public Server()
        {
            // Start the TCP listener and the server.
            // Initialize the login database and create a new database.

            db = new LoginDatabase();
           
            /*REMEMBER createNewdatabase() SHOULD ONLY BE USED TO CLEAR THE DB*/
           // db.createNewDatabase();
            db.connectToDatabase();
            db.createTable();
            
            //testing functions

            /*db.addNewPlayer("Chris", "passw1");
            db.addNewPlayer("Tom", "password");
            Console.WriteLine(db.attemptToLogin("Tom", "password"));*/
            db.printUsers();

            startListener();            // Declare a new listener
            startServer();              // Start the server! (doesn't actually do anything, haha.)
        }

        private void startListener()
        {
            // Declare a new listener with local 127.0.0.1 and port 54389

            listener = new TcpListener(port);
            listener.Start();

            // Create a new ListenerThread.

            ListenerThread lt = new ListenerThread();
            Thread listenThread = new Thread(new ThreadStart(lt.addPlayers ) );
            listenThread.Start();

            // Console.WriteLine("TCP Listener is running");
        }

        private void startServer()
        {
            // DOesn't acually do anything....
            Console.Write("Press Enter to Start the server: ");
            Console.ReadLine();             
        }

        private static int getIndexOfGameToJoin(Player p)
        {
            int numberGames = games.Count;
            String playerGame = p.getGame();

            Console.WriteLine("games capacity " + games.Count.ToString() );

            // check each existing game for a game type match AND room for an available player 
            // counting by g
            for (int g = 0; g < numberGames; g++)
            {

               String gameType = games[g].getGame().getGameType();

               int currentPlayers = games[g].getGame().getNumberPlayers();
               int maxPlayers = games[g].getGame().getMaxPlayers();

               if ((playerGame == gameType) && (currentPlayers < maxPlayers))
               {
                   return g;
               }
            }

            return -1;
        }

        private class ListenerThread
        { // start thread for TCP listener
            public void addPlayers()
            {
                while (true) // Only add players 
                {
                    Socket sock = listener.AcceptSocket();          // accept the sock

                    Console.WriteLine("   player socket has accepted the socket ");

                    NetworkStream nws = new NetworkStream(sock);    
                    StreamReader sr = new StreamReader(nws);
                    StreamWriter sw = new StreamWriter(nws);
                    sw.AutoFlush = true;

                    Player p = new Player(nws, sock, sr, sw);       // construct a player object   
                    activePlayers.Add(p);                           // Add it to the list of active players

                    int playerNumber = activePlayers.IndexOf(p);

                    Console.WriteLine("new player number " + playerNumber.ToString() );
                    // message 1
                    p.getPlayerWriter().WriteLine(playerNumber.ToString() );

                    // message 2
                    String startMessage = p.getPlayerReader().ReadLine();
                    String[] data = startMessage.Split(delim);

                    /*
                     * 
                     if( login was successful)
                     *  startMessage = "Login was successful.";
                     * 
                     else if ( new user was created)
                     *  startMessage = "New User created.";
                     * 
                     else // login attempt failed
                     *  startMessage = "Login attempt failed. Try again.";
                     * 
                     p.getPlayerWriter().WriteLine
                     * 
                     */

                    // username is data[0]
                    // password is data[1]
                    // gameType is data[2]

                    p.setUserName(data[0]);

                    string loginMessage = " ";

                    while(loginMessage[0] != 'W')
                    {
                        if (db.attemptToLogin(data[0], data[1]))
                        {
                            loginMessage = "Welcome back!";
                        }

                        else if (db.addNewPlayer(data[0], data[1]))
                        {
                            loginMessage = "New User created. We hope you enjoy our game!";
                        }

                        else
                        {
                            loginMessage = "Login attempt failed. Try again.";
                        }

                        // message 3
                        p.getPlayerWriter().WriteLine(loginMessage);

                        System.Threading.Thread.Sleep(500);
                    }

                    Console.WriteLine("message 3 sent");

                    p.setGame(data[2]);

                    int gameToJoin = getIndexOfGameToJoin(p);
                    String newPlayerNumber = "0";
                    String gameToPlay = "";

                    // there is no existing game of the type that the player wants to play 
                    // that also has room for an additional player
                    if (gameToJoin == -1)
                    {
                        newPlayerNumber = "1";
                        Game_Generic newGame = null;

                        if(p.getGame() == "generic")
                        {
                            newGame = new Game_Generic(8, 8);
                        }

                        else if (p.getGame() == "connectFour")
                        {
                            newGame = new Game_ConnectFour();
                        }

                        else if (p.getGame() == "othello")
                        {
                            newGame = new Game_Othello();
                        }

                        // games.Add(newGame);
                        newGame.addPlayer(p);
                        gameToPlay = p.getGame();

                        Console.WriteLine("starting a new game");

                        GameThread gt = new GameThread(newGame);
                        Thread gameThread = new Thread(new ThreadStart(gt.playGame) );
                        gameThread.Start();
                        gt.setThread(gameThread);
                        games.Add(gt);
                    }

                    // found a matching game type that needs an additional player
                    else
                    {
                        newPlayerNumber = games[gameToJoin].getGame().getNumberPlayers().ToString();
                        games[gameToJoin].getGame().addPlayer(p);
                    }

                    startMessage = newPlayerNumber;

                    // the REAL message 4
                    // start string is constructed to tell the client which game to start 
                    p.getPlayerWriter().WriteLine(startMessage);
                
                    // count by t
                    for (int t = 0; t < games.Count; t++)
                    {
                        if (!games[t].getGame().getGameState() )
                        {
                            games[t].getThread().Abort();
                        }
                    }
                
                }
            }
        } // end thread for TCP listener

        private class GameThread
        { // start GameThread
            private Game_Generic currentGame;
            private Thread thread;

            public GameThread(Game_Generic newGame)
            {
                currentGame = newGame;
            }

            public void playGame()
            {
                Console.WriteLine("starting game loop on a thread " + currentGame.getGameType() );

                currentGame.getLoop().gameLoop(currentGame);
            }

            public Game_Generic getGame()
            {
                return currentGame;
            }

            public Thread getThread()
            {
                return thread;
            }

            public void setThread(Thread t)
            {
                thread = t;
            }
        } // end GameThread
    }
}