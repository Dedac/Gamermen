using System.Linq;
using System.Collections.Generic;

namespace Gamermen
{
    class Program
    {
        static void Main(string[] args)
        {
            var Players = new List<Player> {
                new Player {Name = "Rich"},
                new Player {Name = "Joe"},
                new Player {Name = "Andrew"},
                new Player {Name = "Ralph"},
                new Player {Name = "Marty"},
                new Player {Name = "Jeff"},
                new Player {Name = "Other"},
                new Player {Name = "Other1"},
                new Player {Name = "Other2"},
            };

            var Months = new List<Month> {
                new Month {Game = "Mapmaker", Name = "Nov"},
                new Month {Game = "Puerto Rico", Name = "Dec"},
                new Month {Game = "Libertalia", Name = "Jan"}
            };

            foreach (var m in Months)
            {
                var PlayedThisMonth = new List<Player>();
                int CurrentMonday = 1;
                while (PlayedThisMonth.Count < Players.Count)
                {
                    var currentGame = new Game()
                    {
                        Day = CurrentMonday
                    };
                    Players.Where(p => !PlayedThisMonth.Contains(p), )

                    m.Games.Add(currentGame);

                }
            }
        }
    }

    class Player
    {
        public string Name { get; set; }
        public List<string> PlayedWith { get; set; }
        public int PlayerCount3s { get; set; }
        public int PlayerCount4s { get; set; }
    }

    class Month
    {
        public string Name { get; set; }
        public string Game { get; set; }
        public List<Game> Games { get; set; }
    }

    class Game
    {
        public int Day { get; set; }
        public int PlayerCount { get; set; }
        public List<Player> Players { get; set; }
    }
}
