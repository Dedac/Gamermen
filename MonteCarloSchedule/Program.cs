using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonteCarloSchedule
{
    class Program
    {
        static void Main(string[] args)
        {
            var BestSet = BuildSet();

            Parallel.For(0, 4, (int j) =>
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
        }

        static Set BuildSet()
        {
            Set currentSet = new Set();
            currentSet.Months = GetNewMonthList();
            currentSet.Players = GetNewPlayerList();

            foreach (var m in currentSet.Months)
            {
                var players = GetNewPlayerList();
                players.Shuffle();
                for (int i = 0; i < 7; i++)
                {
                    var playercount = (i == 5 || i == 6) ? 3 : 4;
                    var game = new Game() { Players = players.GetRange(0, playercount).Select(p => p.Name).ToList() };
                    players.RemoveRange(0, playercount);
                    game.Players.ForEach(p =>
                    {
                        var mp = currentSet.Players.FirstOrDefault(mp => mp.Name == p);
                        if (playercount == 3)
                            mp.PlayerCount3s++;
                        else if (playercount == 4)
                            mp.PlayerCount4s++;

                        mp.PlayedWith.AddRange(game.Players.Where(a => a != mp.Name));
                    });

                    m.Games.Add(game);
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

