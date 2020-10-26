using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace MonteCarloSchedule
{
    class Program
    {
        static void Main(string[] args)
        {
            Set BestSet = BuildSet();

            if (args.Length > 0) //If there is a set file specified, start with that as the best set
                BestSet = JsonSerializer.Deserialize<Set>(File.ReadAllText(args[0]));

            //Run as many in parallel as your processors support
            Parallel.For(0, 10, (int j) =>
            {
                int counter = 0;
                while (counter < 1000000)
                {
                    counter++;

                    if (counter % 10000 == 0)
                        Console.WriteLine($"{j}/{counter} - {BestSet.EvalText()}");

                    var set = BuildSet();
                    if (//set.Min3s > 0 && set.Max3s < 5 &&
                        set.PlayedWithMin >= BestSet.PlayedWithMin &&
                        set.PlayedWithSum > BestSet.PlayedWithSum
                       )
                        BestSet = set;
                }
            });

            BestSet.Display();
            File.WriteAllText($"BestSet{DateTime.Now.ToString("yyyyMMddHHmmss")}.json", JsonSerializer.Serialize(BestSet));
        }

        static Set BuildSet()
        {
            Set currentSet = new Set();
            currentSet.Months = GetNewMonthList();
            currentSet.Players = GetNewPlayerList();

            foreach (var m in currentSet.Months)
            {
                var players = GetNewPlayerList().Select(p => p.Name).ToList();

                //Get the list of players that don't have 2  3 player games yet
                //var needs3player = currentSet.Players.Where(p => p.PlayerCount3s < 2).Select(p => p.Name).ToList();
                //players = players.Except(needs3player).ToList(); //get the list of players without those players

                players.Shuffle();
                //needs3player.Shuffle();
                //put the needs 3 player group at the end to make them more likley to get a three player game
                //players.AddRange(needs3player);

                for (int i = 0; i < 7; i++) //Build 7 games
                {
                    var playercount = 4; // (i == 5 || i == 6) ? 3 : 4;  //The Last 2 games will be 3 player games
                    var game = new Game() { Players = players.GetRange(0, playercount) }; //Get the next set of players for this game

                    //Look to swap an players for the maximum number of different players played
                    game.Players.ToList().ForEach(p =>
                    {
                        if (game.Players.Contains(p))  //player hasn't been removed from the game
                        { 
                            var others = game.Players.FindAll(a => a != p);
                            var mp = currentSet.Players.FirstOrDefault(mp => mp.Name == p); //player from the set

                            //swap the players this player has played more than once, if there are unplayed players
                            foreach (var mpp in others.Select(o => (name: o, playCount: mp.PlayedWith.Count(c => c == o))).OrderBy(o => o.playCount).Where(o => o.playCount > 1))
                            {
                                var nextUnplayed = players.Where(a => !game.Players.Contains(a)).FirstOrDefault(a => !mp.PlayedWith.Contains(a));
                                if (nextUnplayed != null)
                                {
                                    game.Players.Remove(mpp.name);
                                    game.Players.Add(nextUnplayed);
                                    others = game.Players.FindAll(a => a != p); //Update Others
                                }
                            }

                            //If there still are any unplayed players, replace the most played player with the first unplayed player
                            var firstUnplayed = players.Where(a => !game.Players.Contains(a)).FirstOrDefault(a => !mp.PlayedWith.Contains(a));
                            if (firstUnplayed != null) //There is an unplayed player
                            {
                                var mostPlayedWith = others.Select(o => (name: o, playCount: mp.PlayedWith.Count(c => c == o))).OrderBy(o => o.playCount).First();
                                if (mostPlayedWith.playCount > 0) //The player this player has most played with is more than once
                                {
                                    //swap the players
                                    game.Players.Remove(mostPlayedWith.name);
                                    game.Players.Add(firstUnplayed);
                                }
                            }

                           
                        }
                    });

                    m.Games.Add(game);
                    //remove players added to this set from future games
                    players.RemoveAll(p => game.Players.Contains(p));

                    game.Players.ForEach(p => //Add game to each player's info
                    {
                        var mp = currentSet.Players.FirstOrDefault(mp => mp.Name == p);
                        if (playercount == 3)
                            mp.PlayerCount3s++;
                        else if (playercount == 4)
                            mp.PlayerCount4s++;

                        mp.PlayedWith.AddRange(game.Players.Where(a => a != mp.Name));
                    });
                }
            }

            return currentSet;
        }

        static List<Player> GetNewPlayerList() =>
             new List<Player> {
                new Player {Name = "Rich"},
                new Player {Name = "Joe"},
                new Player {Name = "Andrew"},
                new Player {Name = "Ralph"},
                new Player {Name = "Nick"},
                new Player {Name = "Ian"},
                new Player {Name = "Elliot"},
                new Player {Name = "Amanda"},
                new Player {Name = "Brett"},
                new Player {Name = "Schoppie"},
                new Player {Name = "Jeff"},
                new Player {Name = "Pierce"},
                new Player {Name = "Gabe"},
                new Player {Name = "Melanie"},
                new Player {Name = "Kieran"},
                new Player {Name = "Tony"},
                new Player {Name = "Alex F."},
                new Player {Name = "Marty"},
                new Player {Name = "Mike"},
                new Player {Name = "Kathleen"},
                new Player {Name = "Jeremy"},
                new Player {Name = "Jimmy"},
                new Player {Name = "Lindsay"},
                new Player {Name = "Jordan F"},
                new Player {Name = "Danielle"},
                new Player {Name = "Jordan B"},
                new Player {Name = "Matt"},
                new Player {Name = "Kayleigh"}
            };

        static List<Month> GetNewMonthList() =>
            new List<Month> {
                new Month {Game = "Mapmaker", Name = "Nov"},
                new Month {Game = "Seasons", Name = "Dec"},
                new Month {Game = "Kingdom Builder", Name = "Jan"},
                new Month {Game = "Potion Explosion", Name = "Feb"},
                new Month {Game = "Carcassonne", Name = "Mar"},
                new Month {Game = "Puerto Rico", Name = "Apr"},
                new Month {Game = "Colt Express", Name = "May"},
                new Month {Game = "Race For the Galaxy", Name = "Jun"},
                new Month {Game = "Libertalia", Name = "Jul"}
            };

        static List<(Player, Player, int)> PlayerPairsPlayed() => new List<(Player, Player, int)>();
    }
}

