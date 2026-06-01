namespace QuoridorGame.Core

/// Represents a 0-indexed cell position on the 9x9 board.
/// Rows go from 0 (top) to 8 (bottom).
/// Columns go from 0 (left) to 8 (right).
type Position = {
    Row: int
    Col: int
}

/// Identifies the players in the game.
type PlayerType = 
    | Human
    | AI

/// All possible physical movement directions, including special diagonal jumps.
type Direction = 
    | Up
    | Down
    | Left
    | Right
    | UpLeft
    | UpRight
    | DownLeft
    | DownRight

/// The orientation of a placed wall.
type WallOrientation = 
    | Horizontal
    | Vertical

/// Represents a wall placed on the board.
/// Row and Col (0 to 7) point to the top-left cell of the 4-cell intersection it affects.
type Wall = {
    Orientation: WallOrientation
    Row: int
    Col: int
}

/// Tracks an individual player's current assets and position.
type PlayerState = {
    Position: Position
    RemainingWalls: int
}

/// The master state of the game at any given tick.
type GameState = {
    HumanPlayer: PlayerState
    AIPlayer: PlayerState
    Walls: Wall list
    CurrentTurn: PlayerType
}

/// Represents any parsed action a player intends to make on their turn.
type GameAction =
    | Move of Direction
    | PlaceWall of orientation: WallOrientation * row: int * col: int
    | Quit




module GameInitializer =

    /// Generates the default starting state for a new Quoridor game.
    let createInitialState () : GameState =
        {
            HumanPlayer = {
                // Human starts at center of the bottom row (Row 8, Col 4)
                Position = { Row = 8; Col = 4 }
                RemainingWalls = 10
            }
            AIPlayer = {
                // AI starts at center of the top row (Row 0, Col 4)
                Position = { Row = 0; Col = 4 }
                RemainingWalls = 10
            }
            Walls = []
            CurrentTurn = Human // Human moves first
        }