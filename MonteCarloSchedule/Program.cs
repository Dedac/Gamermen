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
        readonly record struct PairQuality(int Need, int NewPairs, int RepeatPenalty)
        {
            public PairQuality Add(PairQuality other) =>
                new(Need + other.Need, NewPairs + other.NewPairs, RepeatPenalty + other.RepeatPenalty);

            public int CompareTo(PairQuality other)
            {
                var comparison = Need.CompareTo(other.Need);
                if (comparison != 0) return comparison;
                comparison = NewPairs.CompareTo(other.NewPairs);
                return comparison != 0 ? comparison : other.RepeatPenalty.CompareTo(RepeatPenalty);
            }
        }

        sealed class PartnerHistory
        {
            readonly Dictionary<string, int> _playerIndices;
            readonly int[,] _pairCounts;
            readonly int[] _distinctCounts;

            public PartnerHistory(IEnumerable<string> playerNames)
            {
                var names = playerNames.ToList();
                _playerIndices = names.Select((name, index) => (name, index))
                    .ToDictionary(item => item.name, item => item.index);
                _pairCounts = new int[names.Count, names.Count];
                _distinctCounts = new int[names.Count];
            }

            public int PairCount(string first, string second) =>
                _pairCounts[_playerIndices[first], _playerIndices[second]];

            public int DistinctCount(string player) => _distinctCounts[_playerIndices[player]];

            public void AddGame(IReadOnlyList<string> players)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var first = _playerIndices[players[i]];
                    for (int j = i + 1; j < players.Count; j++)
                    {
                        var second = _playerIndices[players[j]];
                        if (_pairCounts[first, second] == 0)
                        {
                            _distinctCounts[first]++;
                            _distinctCounts[second]++;
                        }
                        _pairCounts[first, second]++;
                        _pairCounts[second, first]++;
                    }
                }
            }

            public PairQuality EvaluateTable(IReadOnlyList<string> players)
            {
                var quality = new PairQuality();
                for (int i = 0; i < players.Count; i++)
                {
                    var first = players[i];
                    for (int j = i + 1; j < players.Count; j++)
                    {
                        var second = players[j];
                        var count = PairCount(first, second);
                        if (count == 0)
                        {
                            var maxDistinct = _playerIndices.Count - 1;
                            quality = quality.Add(new PairQuality(
                                maxDistinct - DistinctCount(first) + maxDistinct - DistinctCount(second),
                                1,
                                0));
                        }
                        else
                        {
                            quality = quality.Add(new PairQuality(0, 0, 2 * count - 1));
                        }
                    }
                }
                return quality;
            }
        }

        const int ParallelRuns = 7;
        const int IterationsPerRun = 100000;
        const int ProgressInterval = 500;
        const int ImprovementAttempts = 200;

        static void Main(string[] args)
        {
            Set BestSet = BuildSet();
            var bestSetLock = new object();
            var saveSequence = 0;

            //If there is a set file specified, start with that as the best set
            if (args.Length > 0)
            {
                BestSet = JsonSerializer.Deserialize<Set>(File.ReadAllText(args[0]));
                RebuildPlayerStatistics(BestSet);
            }

            var startingBest = CloneSet(BestSet);
            Parallel.For(0, ParallelRuns, (int j) =>
            {
                var workerBest = CloneSet(startingBest);
                int counter = 0;
                int bestTotalCycle = 0;
                while (counter < IterationsPerRun)
                {
                    counter++;

                    if (counter % ProgressInterval == 0)
                    {
                        Console.WriteLine($"{j}/{counter} - {workerBest.EvalText()}, Best Total This cycle {bestTotalCycle}");
                        bestTotalCycle = 0;
                    }
                    var set = BuildSet();
                    bestTotalCycle = Math.Max(bestTotalCycle, set.PlayedWithTotal);
                    if (set.IsBetterThan(workerBest))
                    {
                        set = ImproveSet(set, ImprovementAttempts);
                        if (set.IsBetterThan(workerBest))
                            workerBest = set;

                        lock (bestSetLock)
                        {
                            if (workerBest.IsBetterThan(BestSet))
                            {
                                BestSet = CloneSet(workerBest);
                                var sequence = ++saveSequence;
                                var fileName = $"BestSet{BestSet.Players.Count}Players{DateTime.Now:yyyyMMddHHmmssfff}-{j}-{counter}-{sequence}.json";
                                File.WriteAllText(fileName, JsonSerializer.Serialize(BestSet));
                            }
                        }
                    }
                }
            });

            BestSet.Display();
            BestSet.Players.ForEach(p => Console.WriteLine($"{p.Name} - {string.Join(", ", p.PlayedWith.GroupBy(x => x).Where(g => g.Count() > 2).Select(x => $"{x.Key}({x.Count()} Times)"))}"));
        }

        static Set ImproveSet(Set set, int attempts)
        {
            var random = ThreadSafeRandom.ThisThreadsRandom;
            var currentScore = CloneSet(set);
            var bestScore = CloneSet(set);
            var eligibleMonths = set.Months.Where(month => month.Games.Count > 1).ToList();
            if (eligibleMonths.Count == 0)
                return bestScore;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var month = eligibleMonths[random.Next(eligibleMonths.Count)];
                var firstGameIndex = random.Next(month.Games.Count);
                var secondGameIndex = random.Next(month.Games.Count - 1);
                if (secondGameIndex >= firstGameIndex)
                    secondGameIndex++;

                var firstGame = month.Games[firstGameIndex];
                var secondGame = month.Games[secondGameIndex];
                if (firstGame.Players.Count == 0 || secondGame.Players.Count == 0)
                    continue;

                var firstPlayerIndex = random.Next(firstGame.Players.Count);
                var secondPlayerIndex = random.Next(secondGame.Players.Count);
                var firstPlayer = firstGame.Players[firstPlayerIndex];
                var secondPlayer = secondGame.Players[secondPlayerIndex];

                firstGame.Players[firstPlayerIndex] = secondPlayer;
                secondGame.Players[secondPlayerIndex] = firstPlayer;
                RebuildPlayerStatistics(set);

                if (set.IsBetterThan(currentScore) ||
                    (!currentScore.IsBetterThan(set) && !set.IsBetterThan(currentScore) && random.Next(5) == 0))
                {
                    currentScore = CloneSet(set);
                    if (currentScore.IsBetterThan(bestScore))
                        bestScore = CloneSet(currentScore);
                }
                else
                {
                    firstGame.Players[firstPlayerIndex] = firstPlayer;
                    secondGame.Players[secondPlayerIndex] = secondPlayer;
                    RebuildPlayerStatistics(set);
                }
            }

            return bestScore;
        }

        static Set CloneSet(Set set) => new()
        {
            Players = set.Players.Select(player => new Player
            {
                Name = player.Name,
                PlayerCount3s = player.PlayerCount3s,
                PlayerCount4s = player.PlayerCount4s,
                PlayedWith = new List<string>(player.PlayedWith)
            }).ToList(),
            Months = set.Months.Select(month => new Month
            {
                Name = month.Name,
                Game = month.Game,
                Games = month.Games.Select(game => new Game
                {
                    Day = game.Day,
                    Players = new List<string>(game.Players)
                }).ToList()
            }).ToList()
        };

        static string SelectMostConstrainedPlayer(IReadOnlyList<string> players, PartnerHistory history)
        {
            var selected = players[0];
            var fewestUnplayed = int.MaxValue;
            var fewestPartners = int.MaxValue;

            foreach (var player in players)
            {
                var unplayed = players.Count(other =>
                    other != player && history.PairCount(player, other) == 0);
                var distinct = history.DistinctCount(player);
                if (unplayed < fewestUnplayed ||
                    (unplayed == fewestUnplayed && distinct < fewestPartners))
                {
                    selected = player;
                    fewestUnplayed = unplayed;
                    fewestPartners = distinct;
                }
            }

            return selected;
        }

        static List<string> AddPlayersToGame(IReadOnlyList<string> players, PartnerHistory history)
        {
            var selected = new List<string>(4) { SelectMostConstrainedPlayer(players, history) };

            while (selected.Count < 4)
            {
                string bestCandidate = null;
                var bestQuality = new PairQuality();
                var hasBestCandidate = false;

                foreach (var candidate in players.Where(player => !selected.Contains(player)))
                {
                    var candidateTable = selected.Append(candidate).ToList();
                    var quality = history.EvaluateTable(candidateTable);
                    var comparison = hasBestCandidate ? quality.CompareTo(bestQuality) : 1;
                    if (comparison > 0 || (comparison == 0 && ThreadSafeRandom.ThisThreadsRandom.Next(2) == 0))
                    {
                        bestCandidate = candidate;
                        bestQuality = quality;
                        hasBestCandidate = true;
                    }
                }

                if (bestCandidate == null)
                    break;
                selected.Add(bestCandidate);
            }

            return selected;
        }

        static (List<string> First, List<string> Second) SelectBestFinalEightSplit(
            IReadOnlyList<string> players,
            PartnerHistory history)
        {
            var bestFirst = new List<string>();
            var bestSecond = new List<string>();
            var bestQuality = new PairQuality();
            var hasBest = false;

            for (int i = 1; i < 6; i++)
            {
                for (int j = i + 1; j < 7; j++)
                {
                    for (int k = j + 1; k < 8; k++)
                    {
                        var first = new List<string> { players[0], players[i], players[j], players[k] };
                        var second = players.Where(player => !first.Contains(player)).ToList();
                        var quality = history.EvaluateTable(first).Add(history.EvaluateTable(second));
                        var comparison = hasBest ? quality.CompareTo(bestQuality) : 1;
                        if (comparison > 0 || (comparison == 0 && ThreadSafeRandom.ThisThreadsRandom.Next(2) == 0))
                        {
                            bestFirst = first;
                            bestSecond = second;
                            bestQuality = quality;
                            hasBest = true;
                        }
                    }
                }
            }

            return (bestFirst, bestSecond);
        }

        static Set BuildSet()
        {
            var currentSet = new Set
            {
                Months = GetNewMonthList(),
                Players = GetNewPlayerList()
            };
            var history = new PartnerHistory(currentSet.Players.Select(player => player.Name));

            foreach (var m in currentSet.Months)
            {
                var players = GetNewPlayerList().Select(p => p.Name).ToList();
                var totalGames = players.Count / 4; //integer division
                players.Shuffle();

                while (m.Games.Count < totalGames && players.Count >= 4)
                {
                    var gamesRemaining = totalGames - m.Games.Count;
                    var teams = players.Count == 8 && gamesRemaining >= 2
                        ? SelectBestFinalEightSplit(players, history)
                        : (AddPlayersToGame(players, history), new List<string>());

                    foreach (var team in new[] { teams.Item1, teams.Item2 }.Where(team => team.Count > 0))
                    {
                        var game = new Game { Players = team };
                        m.Games.Add(game);
                        players.RemoveAll(player => game.Players.Contains(player));
                        UpdatePlayerStatistics(game, currentSet);
                        history.AddGame(game.Players);
                    }
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

        static void RebuildPlayerStatistics(Set currentSet)
        {
            currentSet.Players.ForEach(player =>
            {
                player.PlayerCount3s = 0;
                player.PlayerCount4s = 0;
                player.PlayedWith.Clear();
            });

            foreach (var game in currentSet.Months.SelectMany(month => month.Games))
                UpdatePlayerStatistics(game, currentSet);
        }

        static List<Player> GetNewPlayerList() =>
                JsonSerializer.Deserialize<Set>(File.ReadAllText("./MonthAndPlayerInitial.json")).Players;

        static List<Month> GetNewMonthList() =>
                JsonSerializer.Deserialize<Set>(File.ReadAllText("./MonthAndPlayerInitial.json")).Months;

        static List<(Player, Player, int)> PlayerPairsPlayed() => new();
    }
}
