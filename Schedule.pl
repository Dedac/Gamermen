:- use_module(library(lists)).

% Select Element X, from List L, Returning the Rest of the List
% Can I replace this with a built in function?
el(X,[X|L],L).
el(X,[_|L],R) :- el(X,L,R).

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

games(0,_) :- !.
games(I,X) :- I > 0,
                players(P), 
                group(P,[3,3,3],X), 
                I1 is I-1,
                games(I1,X).
 
