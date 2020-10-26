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
                        BestSet.EvalText();

                    var set = BuildSet();
                    if (set.Min3s > 0 && set.Max3s < 5 &&
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

                var needs3player = currentSet.Players.Where(p => p.PlayerCount3s < 2).Select(p => p.Name).ToList();
                players = players.Except(needs3player).ToList();
                
                players.Shuffle();
                needs3player.Shuffle();

                players.AddRange(needs3player);

                for (int i = 0; i < 7; i++)
                {
                    var playercount = (i == 5 || i == 6) ? 3 : 4;
                    var game = new Game() { Players = players.GetRange(0, playercount) };

                    game.Players.ToList().ForEach(p =>
                    {
                        //If there are any unplayed players, replace the most played player with the unplayed player
                        var others = game.Players.FindAll(a => a != p);
                        var mp = currentSet.Players.FirstOrDefault(mp => mp.Name == p);
                        
                        var firstUnplayed = players.Skip(playercount).FirstOrDefault(a => !mp.PlayedWith.Contains(a));
                        if (firstUnplayed != null){
                            var mostPlayedWith = others.Select(o => (o, mp.PlayedWith.Count(c => c == o))).OrderBy(o => o.Item2).First();
                            if (mostPlayedWith.Item2 > 0) {
                                game.Players.Remove(mostPlayedWith.o);
                                game.Players.Add(firstUnplayed);
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
                new Player {Name = "Jordan B"}
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

