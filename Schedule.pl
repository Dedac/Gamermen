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

players([rich,joe,ralph,andrew,jeff,schultz,schoppie,marty,eman]).

games(0,[],_) :- !.
games(I,[A|B],Acc) :- I > 0,
                players(P), 
                group(P,[3,3,3],A),
                \+ member(A, Acc),
                I1 is I-1,
                games(I1,B,[A|Acc]).

%aggregate_all(count, member(rich,[rich,andrew,ralph]), X).

%games(3,[L,M,C],[]).