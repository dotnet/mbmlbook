This distribution contains 4 datasets of game outcomes gathered during
the Beta testing period for the Xbox game Halo 2. The beta testing 
started on the 8th July 2004 and finished on the 1st September 2004.
A total of 6465 employees from Microsoft (across all time zones) 
participated in this Beta test.

The fields in each line of the dataset files are seperated by commata.
Each line of the 4 files contains the following fields:
     * Date and Time when the game finsihed (PST time zone)
     * Unique game ID (valid across all 4 game modes)
     * Variant of the game (3 different values)
         1 = Capture the Flag
         2 = Slayer 
         9 = Assault
     * Map ID on which the game was played (5 different values)
     * Unique player ID (valid across all 4 game modes)
     * Team association 
     * Score

In order to determine the winning team, team scores are defined as the
sum of the individual member scores (all members with the same team
association).

The four files contain data from 4 different game types
     * FreeForAll (60022 games/5943 players)
         [up to 8 players playing against each other] 
     * HeadToHead (6227 games/1672 players)
         [1 players playing against 1 other player]
     * SmallTeam (27539 games/4992 players)
         [up to 4 players in 2 teams]
     * LargeTeam (1199 games/2576 players)   
         [up to 8 players in 2 teams]

