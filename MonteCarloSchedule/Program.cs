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
            Parallel.For(0, 8, (int j) =>
            {
                int counter = 0;
                int bestSumCycle = 0;
                while (counter < 10000)
                {
                    counter++;

                    if (counter % 1000 == 0)
                    {
                        Console.WriteLine($"{j}/{counter} - {BestSet.EvalText()}, Best Sum This cycle {bestSumCycle}");
                        bestSumCycle = 0;
                    }

                    var set = BuildSet();
                    bestSumCycle = Math.Max(bestSumCycle, set.PlayedWithSum);
                    if  (set.PlayedWithSum > BestSet.PlayedWithSum ||
                        (
                            set.PlayedWithMax - set.PlayedWithMin < BestSet.PlayedWithMax - BestSet.PlayedWithMin
                            && set.PlayedWithSum >= BestSet.PlayedWithSum
                        )
                       )
                    {
                        BestSet = set;
                        File.WriteAllText($"BestSet{DateTime.Now.ToString($"yyyyMMddHHmmss-{j}")}.json", JsonSerializer.Serialize(BestSet));
                    }
                }
            });

            BestSet.Display();
            BestSet.Players.ForEach(p => Console.WriteLine($"{p.Name} - {string.Join(", ", GetNewPlayerList().Select(pl => pl.Name).Where(pl => pl != p.Name).Except(p.PlayedWith))}"));
        }

        static void AddPlayersToGame(string player, Game game, List<string> players, Set currentSet)
        {
            if (game.Players.Count == 4) return;
            
            game.Players.Add(player);
            
            var mp = currentSet.Players.FirstOrDefault(mp => mp.Name == player); 

            var unplayed = players.Except(game.Players).ToList().Except(game.Players.SelectMany(pl => currentSet.Players.FirstOrDefault(mp => mp.Name == pl).PlayedWith));
        
            if (unplayed.Count() == 0) unplayed = players.Except(mp.PlayedWith).Except(game.Players);
            
            var nextunplayed = unplayed.FirstOrDefault() ?? players.First();
            
            AddPlayersToGame(nextunplayed, game, players.Where(pl => pl != player).ToList(), currentSet);
        }



        static Set BuildSet()
        {
            Set currentSet = new Set();
            currentSet.Months = GetNewMonthList();
            currentSet.Players = GetNewPlayerList();

            foreach (var m in currentSet.Months)
            {
                var players = GetNewPlayerList().Select(p => p.Name).ToList();

                players.Shuffle();

                for (int i = 0; i < 7; i++) //Build 7 games
                {
                    var playercount = 4; //no longer using 
                    var game = new Game() { }; //Get the next set of players for this game

                    AddPlayersToGame(players.First(), game, players, currentSet);

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
                new Player {Name = "Elliott"},
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

