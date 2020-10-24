:- use_module(library(lists)).

% L is The subset of the list following X
el(X,[X|L],L). %When X is the first item, return the list without X
el(X,[_|L],R) :- el(X,L,R). %When X is not the first elemwnt, run again with the resto of the the list, looking to return the list of items after X 

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

players([rich,joe,ralph,andrew,jeff,schultz,schoppie,marty,eman,ian]).

%Check in any member of the the list X exists in the list A
not_in([],_).
not_in([XH|XT],A) :- \+ memberchk(XH, A), not_in(XT,A).

%Check that all items in List X are anywhere in a list of lists A
all_in([],_).
all_in(X,[AH|AT]) :- is_list(AH),
                     intersection(X, AH, X)
                     ;
                     all_in(X,AT).

%Split a list into pairs, and count how many times each pair appears in the list of combinations
count_pairs_in(ListToCheck, ListOfCombinations, Count) :- selectN(2,ListToCheck,Pair), 
                                                 aggregate_all(count, all_in(Pair,ListOfCombinations),Count).
                                                 

%Get the player combinations that satisfy the rules
games(0,[],_) :- !.
games(I,[A|B],Acc) :- I > 0,
                players(P), 
                group(P,[3,4,3],A), %Group all the players into games for the month
                not_in(A, Acc),  %the new player sets don't contain any previous player sets
                append(Acc,A,Acc1), %Add the new player sets to the full list of previous games
                aggregate_all(min(Count), count_pairs_in(P,Acc1,Count), L),
                between(0,1,L),
                
                %between(0, 2, SumCount),
                I1 is I-1, %move to the next game
                games(I1,B,Acc1).

%aggregate_all(count, member(rich,[rich,andrew,ralph]), X).

%games(3,[L,M,C],[]).
