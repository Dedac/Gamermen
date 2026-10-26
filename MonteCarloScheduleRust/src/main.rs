use std::cmp::Reverse;
use std::collections::HashMap;
use std::error::Error;
use std::fs;
use std::sync::Mutex;
use std::thread;

use rand::seq::SliceRandom;
use rand::{Rng, RngExt};
use serde::{Deserialize, Serialize};

const PARALLEL_RUNS: usize = 7;
const ITERATIONS_PER_RUN: usize = 100_000;
const PROGRESS_INTERVAL: usize = 500;
const IMPROVEMENT_ATTEMPTS: usize = 200;
const INITIAL_FILE: &str = "./MonthAndPlayerInitial.json";

// JSON shapes match the C# MonteCarloSchedule models so files are interchangeable.
#[derive(Deserialize, Serialize, Default)]
#[serde(rename_all = "PascalCase", default)]
struct SetJson {
    months: Vec<MonthJson>,
    players: Vec<PlayerJson>,
}

#[derive(Deserialize, Serialize, Default)]
#[serde(rename_all = "PascalCase", default)]
struct MonthJson {
    name: String,
    game: String,
    games: Vec<GameJson>,
}

#[derive(Deserialize, Serialize, Default)]
#[serde(rename_all = "PascalCase", default)]
struct GameJson {
    day: i32,
    players: Vec<String>,
}

#[derive(Deserialize, Serialize, Default)]
#[serde(rename_all = "PascalCase", default)]
struct PlayerJson {
    name: String,
    played_with: Vec<String>,
    player_count3s: u32,
    player_count4s: u32,
}

#[derive(Clone)]
struct Game {
    day: i32,
    players: Vec<usize>,
}

#[derive(Clone)]
struct Month {
    name: String,
    game: String,
    games: Vec<Game>,
}

#[derive(Clone)]
struct Schedule {
    months: Vec<Month>,
    player_count: usize,
    // pairs[a * player_count + b] = number of games a and b shared.
    pairs: Vec<u16>,
}

#[derive(Clone, Copy, PartialEq, Eq)]
struct Score {
    min: usize,
    max: usize,
    total: usize,
    penalty: usize,
}

#[derive(Clone, Copy, Default, PartialEq, Eq)]
struct PairQuality {
    need: usize,
    new_pairs: usize,
    repeat_penalty: usize,
}

impl PairQuality {
    fn add(self, other: Self) -> Self {
        Self {
            need: self.need + other.need,
            new_pairs: self.new_pairs + other.new_pairs,
            repeat_penalty: self.repeat_penalty + other.repeat_penalty,
        }
    }

    fn key(&self) -> (usize, usize, Reverse<usize>) {
        (self.need, self.new_pairs, Reverse(self.repeat_penalty))
    }
}

impl Score {
    fn key(&self) -> (usize, Reverse<usize>, Reverse<usize>, usize) {
        (
            self.min,
            Reverse(self.max - self.min),
            Reverse(self.penalty),
            self.total,
        )
    }

    fn is_better_than(&self, other: &Score) -> bool {
        self.key() > other.key()
    }

    fn eval_text(&self) -> String {
        format!(
            "Distinct partners: Min {}, Max {}, Total {}, Repeat penalty {}",
            self.min, self.max, self.total, self.penalty
        )
    }
}

impl Schedule {
    fn new(months: Vec<Month>, player_count: usize) -> Self {
        let mut set = Schedule {
            months,
            player_count,
            pairs: vec![0; player_count * player_count],
        };
        set.rebuild_stats();
        set
    }

    fn pair(&self, a: usize, b: usize) -> u16 {
        self.pairs[a * self.player_count + b]
    }

    fn add_pair(&mut self, a: usize, b: usize, delta: i32) {
        let n = self.player_count;
        for idx in [a * n + b, b * n + a] {
            self.pairs[idx] = (self.pairs[idx] as i32 + delta) as u16;
        }
    }

    fn add_game_stats(&mut self, players: &[usize]) {
        for (i, &a) in players.iter().enumerate() {
            for &b in &players[i + 1..] {
                self.add_pair(a, b, 1);
            }
        }
    }

    fn rebuild_stats(&mut self) {
        self.pairs.fill(0);
        let games: Vec<Vec<usize>> = self
            .months
            .iter()
            .flat_map(|m| m.games.iter().map(|g| g.players.clone()))
            .collect();
        for players in games {
            self.add_game_stats(&players);
        }
    }

    fn distinct(&self, player: usize) -> usize {
        (0..self.player_count)
            .filter(|&b| self.pair(player, b) > 0)
            .count()
    }

    fn score(&self) -> Score {
        let n = self.player_count;
        let (mut min, mut max, mut total, mut penalty) = (usize::MAX, 0, 0, 0);
        for a in 0..n {
            let mut distinct = 0;
            for b in 0..n {
                let count = self.pair(a, b) as usize;
                if count > 0 {
                    distinct += 1;
                    if a < b {
                        penalty += (count - 1) * (count - 1);
                    }
                }
            }
            min = min.min(distinct);
            max = max.max(distinct);
            total += distinct;
        }
        Score {
            min: if n == 0 { 0 } else { min },
            max,
            total,
            penalty,
        }
    }
}

fn build_set<R: Rng + ?Sized>(template: &[Month], player_count: usize, rng: &mut R) -> Schedule {
    let mut set = Schedule::new(template.to_vec(), player_count);
    let total_games = player_count / 4;

    for m in 0..set.months.len() {
        let prefilled: Vec<usize> = set.months[m]
            .games
            .iter()
            .flat_map(|g| g.players.clone())
            .collect();
        let mut players: Vec<usize> = (0..player_count)
            .filter(|p| !prefilled.contains(p))
            .collect();
        players.shuffle(rng);

        while set.months[m].games.len() < total_games && players.len() >= 4 {
            let games_remaining = total_games - set.months[m].games.len();
            let teams = if players.len() == 8 && games_remaining >= 2 {
                select_best_final_eight_split(&set, &players, rng)
            } else {
                (select_game_players(&set, &players, rng), Vec::new())
            };

            for team in [teams.0, teams.1]
                .into_iter()
                .filter(|team| !team.is_empty())
            {
                players.retain(|p| !team.contains(p));
                set.add_game_stats(&team);
                set.months[m].games.push(Game {
                    day: 0,
                    players: team,
                });
            }
        }
    }

    set
}

fn pair_quality(set: &Schedule, players: &[usize], distinct_counts: &[usize]) -> PairQuality {
    let mut quality = PairQuality::default();
    for (i, &a) in players.iter().enumerate() {
        for &b in &players[i + 1..] {
            let count = set.pair(a, b) as usize;
            if count == 0 {
                let max_distinct = set.player_count.saturating_sub(1);
                quality.need += max_distinct.saturating_sub(distinct_counts[a])
                    + max_distinct.saturating_sub(distinct_counts[b]);
                quality.new_pairs += 1;
            } else {
                quality.repeat_penalty += 2 * count - 1;
            }
        }
    }
    quality
}

fn select_most_constrained(set: &Schedule, players: &[usize], distinct_counts: &[usize]) -> usize {
    *players
        .iter()
        .min_by_key(|&&player| {
            let unplayed = players
                .iter()
                .filter(|&&other| other != player && set.pair(player, other) == 0)
                .count();
            (unplayed, distinct_counts[player])
        })
        .expect("a game requires at least one available player")
}

fn select_game_players<R: Rng + ?Sized>(
    set: &Schedule,
    players: &[usize],
    rng: &mut R,
) -> Vec<usize> {
    let distinct_counts: Vec<usize> = (0..set.player_count).map(|p| set.distinct(p)).collect();
    let mut selected = vec![select_most_constrained(set, players, &distinct_counts)];

    while selected.len() < 4 {
        let mut best_candidate = None;
        let mut best_quality = PairQuality::default();
        for &candidate in players.iter().filter(|&&p| !selected.contains(&p)) {
            let mut table = selected.clone();
            table.push(candidate);
            let quality = pair_quality(set, &table, &distinct_counts);
            if best_candidate.is_none()
                || quality.key() > best_quality.key()
                || (quality == best_quality && rng.random_range(0..2) == 0)
            {
                best_candidate = Some(candidate);
                best_quality = quality;
            }
        }
        let Some(candidate) = best_candidate else {
            break;
        };
        selected.push(candidate);
    }

    selected
}

fn select_best_final_eight_split<R: Rng + ?Sized>(
    set: &Schedule,
    players: &[usize],
    rng: &mut R,
) -> (Vec<usize>, Vec<usize>) {
    assert_eq!(players.len(), 8);
    let distinct_counts: Vec<usize> = (0..set.player_count).map(|p| set.distinct(p)).collect();
    let mut best: Option<(Vec<usize>, Vec<usize>, PairQuality)> = None;

    for i in 1..6 {
        for j in i + 1..7 {
            for k in j + 1..8 {
                let first = vec![players[0], players[i], players[j], players[k]];
                let second: Vec<usize> = players
                    .iter()
                    .copied()
                    .filter(|p| !first.contains(p))
                    .collect();
                let quality = pair_quality(set, &first, &distinct_counts).add(pair_quality(
                    set,
                    &second,
                    &distinct_counts,
                ));
                match &best {
                    None => best = Some((first, second, quality)),
                    Some((_, _, current)) if quality.key() > current.key() => {
                        best = Some((first, second, quality));
                    }
                    Some((_, _, current)) if quality == *current && rng.random_range(0..2) == 0 => {
                        best = Some((first, second, quality));
                    }
                    _ => {}
                }
            }
        }
    }

    let (first, second, _) = best.expect("eight players yield at least one split");
    (first, second)
}

fn improve_set<R: Rng + ?Sized>(
    mut set: Schedule,
    attempts: usize,
    rng: &mut R,
) -> (Schedule, Score) {
    let mut current = set.score();
    let mut best = (set.clone(), current);
    let eligible: Vec<usize> = (0..set.months.len())
        .filter(|&m| set.months[m].games.len() > 1)
        .collect();
    if eligible.is_empty() {
        return best;
    }

    for _ in 0..attempts {
        let m = eligible[rng.random_range(0..eligible.len())];
        let game_count = set.months[m].games.len();
        let g1 = rng.random_range(0..game_count);
        let mut g2 = rng.random_range(0..game_count - 1);
        if g2 >= g1 {
            g2 += 1;
        }
        let len1 = set.months[m].games[g1].players.len();
        let len2 = set.months[m].games[g2].players.len();
        if len1 == 0 || len2 == 0 {
            continue;
        }
        let (i1, i2) = (rng.random_range(0..len1), rng.random_range(0..len2));

        swap_players(&mut set, m, (g1, i1), (g2, i2));
        let candidate = set.score();

        if candidate.is_better_than(&current)
            || (candidate == current && rng.random_range(0..5) == 0)
        {
            current = candidate;
            if current.is_better_than(&best.1) {
                best = (set.clone(), current);
            }
        } else {
            swap_players(&mut set, m, (g1, i1), (g2, i2));
        }
    }

    best
}

// Swaps two players between games in the same month, updating pair counts incrementally.
fn swap_players(set: &mut Schedule, m: usize, (g1, i1): (usize, usize), (g2, i2): (usize, usize)) {
    let a = set.months[m].games[g1].players[i1];
    let b = set.months[m].games[g2].players[i2];
    let others1: Vec<usize> = set.months[m].games[g1]
        .players
        .iter()
        .copied()
        .filter(|&x| x != a)
        .collect();
    let others2: Vec<usize> = set.months[m].games[g2]
        .players
        .iter()
        .copied()
        .filter(|&x| x != b)
        .collect();

    for &x in &others1 {
        set.add_pair(a, x, -1);
        set.add_pair(b, x, 1);
    }
    for &y in &others2 {
        set.add_pair(b, y, -1);
        set.add_pair(a, y, 1);
    }

    set.months[m].games[g1].players[i1] = b;
    set.months[m].games[g2].players[i2] = a;
}

fn to_json(set: &Schedule, names: &[String]) -> SetJson {
    let mut players: Vec<PlayerJson> = names
        .iter()
        .map(|name| PlayerJson {
            name: name.clone(),
            ..Default::default()
        })
        .collect();

    for game in set.months.iter().flat_map(|m| &m.games) {
        for &p in &game.players {
            if game.players.len() <= 3 {
                players[p].player_count3s += 1;
            } else {
                players[p].player_count4s += 1;
            }
            players[p].played_with.extend(
                game.players
                    .iter()
                    .filter(|&&o| o != p)
                    .map(|&o| names[o].clone()),
            );
        }
    }

    let months = set
        .months
        .iter()
        .map(|m| MonthJson {
            name: m.name.clone(),
            game: m.game.clone(),
            games: m
                .games
                .iter()
                .map(|g| GameJson {
                    day: g.day,
                    players: g.players.iter().map(|&p| names[p].clone()).collect(),
                })
                .collect(),
        })
        .collect();

    SetJson { months, players }
}

fn from_json(json: &SetJson, index: &HashMap<String, usize>) -> Result<Vec<Month>, Box<dyn Error>> {
    let mut months = Vec::with_capacity(json.months.len());
    for m in &json.months {
        let mut games = Vec::with_capacity(m.games.len());
        for g in &m.games {
            let mut players = Vec::with_capacity(g.players.len());
            for name in &g.players {
                let p = index
                    .get(name)
                    .ok_or_else(|| format!("Unknown player '{name}' in month {}", m.name))?;
                players.push(*p);
            }
            games.push(Game {
                day: g.day,
                players,
            });
        }
        months.push(Month {
            name: m.name.clone(),
            game: m.game.clone(),
            games,
        });
    }
    Ok(months)
}

fn display(set: &Schedule, names: &[String]) {
    for month in &set.months {
        println!("\n{}", month.game);
        for game in &month.games {
            let players: Vec<&str> = game.players.iter().map(|&p| names[p].as_str()).collect();
            println!("{}", players.join(", "));
        }
    }

    for (p, name) in names.iter().enumerate() {
        let same = (0..set.player_count)
            .filter(|&o| set.pair(p, o) > 1)
            .count();
        println!(
            "{name:<8} - Played With Different:{} - Same:{same}",
            set.distinct(p)
        );
    }

    println!("{}", set.score().eval_text());

    for (p, name) in names.iter().enumerate() {
        let repeats: Vec<String> = (0..set.player_count)
            .filter(|&o| set.pair(p, o) > 2)
            .map(|o| format!("{}({} Times)", names[o], set.pair(p, o)))
            .collect();
        println!("{name} - {}", repeats.join(", "));
    }
}

fn main() -> Result<(), Box<dyn Error>> {
    let initial: SetJson = serde_json::from_str(&fs::read_to_string(INITIAL_FILE)?)?;
    let names: Vec<String> = initial.players.iter().map(|p| p.name.clone()).collect();
    let index: HashMap<String, usize> = names
        .iter()
        .enumerate()
        .map(|(i, n)| (n.clone(), i))
        .collect();
    let template = from_json(&initial, &index)?;
    let player_count = names.len();

    // If a set file is specified, start with that as the best set.
    let starting_best = match std::env::args().nth(1) {
        Some(path) => {
            let json: SetJson = serde_json::from_str(&fs::read_to_string(path)?)?;
            Schedule::new(from_json(&json, &index)?, player_count)
        }
        None => build_set(&template, player_count, &mut rand::rng()),
    };
    let starting_score = starting_best.score();
    let shared_best = Mutex::new((starting_best.clone(), starting_score, 0usize));

    thread::scope(|scope| {
        for j in 0..PARALLEL_RUNS {
            let (template, names, shared_best, starting_best) =
                (&template, &names, &shared_best, &starting_best);
            scope.spawn(move || {
                let mut rng = rand::rng();
                let mut worker_best = (starting_best.clone(), starting_score);
                let mut best_total_cycle = 0;

                for counter in 1..=ITERATIONS_PER_RUN {
                    if counter % PROGRESS_INTERVAL == 0 {
                        println!(
                            "{j}/{counter} - {}, Best Total This cycle {best_total_cycle}",
                            worker_best.1.eval_text()
                        );
                        best_total_cycle = 0;
                    }

                    let set = build_set(template, player_count, &mut rng);
                    let score = set.score();
                    best_total_cycle = best_total_cycle.max(score.total);
                    if !score.is_better_than(&worker_best.1) {
                        continue;
                    }

                    let improved = improve_set(set, IMPROVEMENT_ATTEMPTS, &mut rng);
                    if improved.1.is_better_than(&worker_best.1) {
                        worker_best = improved;
                    }

                    let mut best = shared_best.lock().unwrap();
                    if worker_best.1.is_better_than(&best.1) {
                        best.0 = worker_best.0.clone();
                        best.1 = worker_best.1;
                        best.2 += 1;
                        let file_name = format!(
                            "BestSet{player_count}Players{}-{j}-{counter}-{}.json",
                            chrono::Local::now().format("%Y%m%d%H%M%S%3f"),
                            best.2
                        );
                        let json = serde_json::to_string(&to_json(&best.0, names))
                            .expect("serialize schedule");
                        if let Err(err) = fs::write(&file_name, json) {
                            eprintln!("Failed to write {file_name}: {err}");
                        }
                    }
                }
            });
        }
    });

    let (best, _, _) = shared_best.into_inner().unwrap();
    display(&best, &names);
    Ok(())
}
