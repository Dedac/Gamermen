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

            //If there is a set file specified, start with that as the best set
            if (args.Length > 0)
            {
                BestSet = JsonSerializer.Deserialize<Set>(File.ReadAllText(args[0]));
                //Rebuild player statistics if manually edited games
                BestSet.Players.ForEach(p =>
                {
                    p.PlayerCount3s = 0;
                    p.PlayerCount4s = 0;
                    p.PlayedWith = new List<string>();
                });
                foreach (var game in BestSet.Months.SelectMany(m => m.Games))
                {
                    UpdatePlayerStatistics(game, BestSet);
                }
            }

            //Run as many in parallel as your processors support
            Parallel.For(0, 7, (int j) =>
            {
                int counter = 0;
                int bestSumCycle = 0;
                while (counter < 100000)
                {
                    counter++;

                    if (counter % 1000 == 0)
                    {
                        Console.WriteLine($"{j}/{counter} - {BestSet.EvalText()}, Best Sum This cycle {bestSumCycle}");
                        bestSumCycle = 0;
                    }

                    var set = BuildSet();
                    bestSumCycle = Math.Max(bestSumCycle, set.PlayedWithSum);
                    if (set.PlayedWithSum > BestSet.PlayedWithSum ||
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
            BestSet.Players.ForEach(p => Console.WriteLine($"{p.Name} - {string.Join(", ", p.PlayedWith.GroupBy(x => x).Where(g => g.Count() > 2).Select(x => $"{x.Key}({x.Count()} Times)"))}"));
        }

        static void AddPlayersToGame(string player, Game game, HashSet<string> players, Set currentSet)
        {
            if (player == null || game.Players.Count == 4) return;

            game.Players.Add(player);
            players.Remove(player);

            var currentPlayer = currentSet.Players.FirstOrDefault(mp => mp.Name == player);

            var unplayed = players.Except(currentPlayer.PlayedWith);
            var nextUnplayed = unplayed.FirstOrDefault() ?? 
                players.Except(game.Players).Except(currentPlayer.PlayedWith.GroupBy(x => x)
                .Where(g => g.Count() > 1).Select(x => x.Key))
                .FirstOrDefault();

            AddPlayersToGame(nextUnplayed, game, players, currentSet);
        }

        static Set BuildSet()
        {
            var currentSet = new Set
            {
                Months = GetNewMonthList(),
                Players = GetNewPlayerList()
            };


            foreach (var m in currentSet.Months)
            {
                var players = GetNewPlayerList().Select(p => p.Name).ToList();
                var totalGames = players.Count / 4; //integer division
                players.Shuffle();

                for (int i = m.Games.Count; i < totalGames; i++)
                {
                    var game = new Game();

                    AddPlayersToGame(players.First(), game, players.ToHashSet(), currentSet);

                    m.Games.Add(game);
                    //remove players added to this set from future games
                    players.RemoveAll(p => game.Players.Contains(p));
                    UpdatePlayerStatistics(game, currentSet);
                }
            }

            return currentSet;
        }

        static void UpdatePlayerStatistics(Game game, Set currentSet)
        {
            game.Players.ForEach(p =>
            {
                var currentPlayer = currentSet.Players.FirstOrDefault(mp => mp.Name == p);
                if (game.Players.Count <= 3)
                    currentPlayer.PlayerCount3s++;
                else
                    currentPlayer.PlayerCount4s++;

                currentPlayer.PlayedWith.AddRange(game.Players.Where(a => a != currentPlayer.Name));
            });
        }
        static List<Player> GetNewPlayerList() =>
                JsonSerializer.Deserialize<Set>(File.ReadAllText("./MonthAndPlayerInitial.json")).Players;

        static List<Month> GetNewMonthList() =>
                JsonSerializer.Deserialize<Set>(File.ReadAllText("./MonthAndPlayerInitial.json")).Months;

        static List<(Player, Player, int)> PlayerPairsPlayed() => new();
    }
}

