:- use_module(library(lists)).

players([rich,joe,ralph,andrew,jeff,schultz,schoppie,marty,eman,ian,matthew,melanie,alf,arbuckle,q,w,e,r,t,y,u,i,o,r]).

% L is The subset of the list following X
el(X,[X|L],L). %When X is the first item, return the list without X
el(X,[_|L],R) :- el(X,L,R). %When X is not the first elemwnt, run again with the rest of the the list, looking to return the list of items after X 

% selectN(N,L,S) :- select N elements of the list L and put them in 
%    the set S. Via backtracking return all posssible selections, but
%    avoid permutations; i.e. after generating S = [a,b,c] do not return
%    S = [b,a,c], etc.
selectN(0,_,[]) :- !.
selectN(N,L,[X|S]) :- N > 0, 
   el(X,L,R), 
   N1 is N-1,
   selectN(N1,R,S).

% group(G,Ns,Gs) :- distribute the elements of G into the groups Gs.
%    The group sizes are given in the list Ns.
group([],[],[]).
group(G,[N1|Ns],[G1|Gs]) :- 
   selectN(N1,G,G1),
   subtract(G,G1,R),
   group(R,Ns,Gs).


%Check in any member of the the list X exists in the list A
not_in([],_):- !.
not_in([XH|XT],A) :- \+ memberchk(XH, A), not_in(XT,A).

%Check that all items in List X are anywhere in a list of lists A
all_in([],_) :- !, fail.
all_in(X,[AH|AT]) :- is_list(AH),
                     intersection(X, AH, X)
                     ;
                     all_in(X,AT).

%Split a list into pairs, and count how many times each pair appears in the list of combinations
pairs_in(ListToCheck, ListOfCombinations, Count) :- selectN(2,ListToCheck,Pair), 
                                                    aggregate_all(count, all_in(Pair,ListOfCombinations),Count).

%Get the length of a list when an item is in that list
in_list_length(_,[],_) :- !, fail.
in_list_length(X,[H|T],Length) :- memberchk(X,H), length(H,Length) ; in_list_length(X,T,Length).

%Get the list of all list lengths for an item
in_lists_lengths([], _, _) :- !, fail.
in_lists_lengths([XH|XT],Y, Lengths) :- aggregate_all(bag(L), in_list_length(XH,Y,L), Lengths); in_lists_lengths(XT, Y, Lengths).

list_lengths(X,Y,SetLengths) :- aggregate_all(bag(L), in_lists_lengths(X,Y,L), SetLengths).

%count(NumberToCount,List,Total) - Number of times Number is in the list
count(_,[],0).
count(Number,[Number|T],N) :- count(Number,T,N1), N is N1 + 1.
count(Number,[X|T],N) :- X \= Number, count(Number,T,N).

%counts(Number, ListofLists, Count) true for each one of the number of times a 'Number' is found in a list of lists
counts(_,[],_) :- !, fail.
counts(Number, [H|T], Count) :- is_list(H), count(Number,H,Count); counts(Number, T, Count).

%list_counts(Number, ListOfLists, Counts) - Counts is a List of occurences for a 'Number' in the given list of lists
list_counts(Number, X, Counts) :- aggregate_all(bag(C), counts(Number,X,C), Counts).

%Total player count group count for a given player is not significantly different than any other player

%True if all the items in a numeric list are within 'Range' of one another
items_in_range([H|T], Range) :- items_in_range([H|T], Range, H, H), !. %The first min value is the first item in the list
items_in_range([],_,_,_) :- !. %If the list is empty, items are always within the range                                             
items_in_range([H|T], Range, Min, Max) :- Min1 is min(Min,H),
                                          Max1 is max(Max,H),!,
                                          Max1 =< Range+Min1, %max and min are still within the range                                          
                                          items_in_range(T, Range, Min1, Max1).

%Get the player combinations that satisfy the rules
games(GamesToPlay) :- length(GamesToPlay, NumberOfGames), games(NumberOfGames, GamesToPlay, []), !.
games(0,[],_) :- !.
games(I,[A|B],Acc) :- I > 0,
                players(P), !,
                group(P,[4,4,4,4,4,4],A), %Group all the players into games for the month
                not_in(A, Acc), %the new player sets don't contain any previous player sets
                append(Acc,A,Acc1), %Add the new player sets to the full list of previous games
                aggregate_all(bag(Count), pairs_in(P,Acc1,Count), ListOfReplayCounts), %Get the list of times a player pair plays each other
                items_in_range(ListOfReplayCounts,1), %Players play with each other player close to the same number of times
                writeln(I),
                list_counts(3,Acc1,ThreePlayerCounts),
                items_in_range(ThreePlayerCounts,1),
                list_counts(4,Acc1,FourPlayerCounts),
                items_in_range(FourPlayerCounts,1),
                I1 is I-1, !, %move to the next game
                writeln(A),
                games(I1,B,Acc1).