
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonteCarloSchedule
{
    class Player
    {
        public string Name { get; set; }
        public List<string> PlayedWith { get; set; } = new List<string>();
        public int PlayerCount3s { get; set; }
        public int PlayerCount4s { get; set; }
    }

    class Month
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public List<Game> Games { get; set; } = new List<Game>();
    }

    class Game
    {
        public int Day { get; set; }
        public List<string> Players { get; set; } = new List<string>();
    }

    class Set
    {
        public List<Month> Months { get; set; }
        public List<(Player, Player, int)> PlayedCounts { get; set; } = new List<(Player, Player, int)>();
        public List<Player> Players { get; internal set; } = new List<Player>();
        
        public bool Range3 => Players.TrueForAll(p => p.PlayerCount3s > 0);
        public int PlayedWithMin => Players.Select(p => p.PlayedWith.Distinct().Count()).Min();
        public int PlayedWithMax => Players.Select(p => p.PlayedWith.Distinct().Count()).Max();
        public int PlayedWithSum => Players.Select(p => p.PlayedWith.Distinct().Count()).Sum();

        public void Display()
        {
            foreach (var m in Months)
            {                
                Console.WriteLine($"{Environment.NewLine}{m.Game}");
                foreach (var g in m.Games)
                {
                    Console.WriteLine(string.Join(", ", g.Players.Select(p => p)));
                }
            }

            foreach (var p in Players){
                Console.WriteLine($"{p.Name.PadRight(8)} - 3s: {p.PlayerCount3s}, 4s: {p.PlayerCount4s} - Played With Different:{p.PlayedWith.Distinct().Count()}");
            }

            EvalText();
        }

        public void EvalText(){
            Console.WriteLine($"Distinct Played With: Min {PlayedWithMin}, Max {PlayedWithMax}, Sum {PlayedWithSum}");
        }
    }
}
