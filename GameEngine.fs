namespace QuoridorGame.Core

module GameEngine =

    /// Helper to alternate turns between the Human and the AI.
    let private toggleTurn (player: PlayerType) : PlayerType =
        match player with
        | Human -> AI
        | AI    -> Human

    /// Checks if either player has reached their respective target baseline row.
    /// Returns Some PlayerType if there is a winner, otherwise None.
    let checkWinner (gameState: GameState) : PlayerType option =
        if gameState.HumanPlayer.Position.Row = 0 then 
            Some Human
        elif gameState.AIPlayer.Position.Row = 8 then 
            Some AI
        else 
            None

    /// Evaluates an intended action against the current game state.
    /// Returns Ok with the newly transformed immutable state, or Error with a descriptive string.
    let tryApplyAction (gameState: GameState) (action: GameAction) : Result<GameState, string> =
        match action with
        | Quit -> 
            Error "QUIT" // Intercepted by the main loop to terminate the game

        | Move dir ->
            // Identify active moving player and passive opponent player
            let currentPlayerState = 
                match gameState.CurrentTurn with
                | Human -> gameState.HumanPlayer
                | AI    -> gameState.AIPlayer

            let opponentPosition = 
                match gameState.CurrentTurn with
                | Human -> gameState.AIPlayer.Position
                | AI    -> gameState.HumanPlayer.Position

            // Calculate destination using our Phase 2/3 geometry engine
            match BoardLogic.getMoveDestination currentPlayerState.Position dir opponentPosition gameState.Walls with
            | None -> 
                Error "This movement is blocked by a wall, out of bounds, or is an illegal jump."
            | Some newPos ->
                // Construct a brand new immutable state with updated positioning
                let nextState =
                    match gameState.CurrentTurn with
                    | Human -> 
                        { gameState with 
                            HumanPlayer = { gameState.HumanPlayer with Position = newPos }
                            CurrentTurn = toggleTurn gameState.CurrentTurn }
                    | AI -> 
                        { gameState with 
                            AIPlayer = { gameState.AIPlayer with Position = newPos }
                            CurrentTurn = toggleTurn gameState.CurrentTurn }
                Ok nextState

        | PlaceWall (orientation, r, c) ->
            let currentPlayerState = 
                match gameState.CurrentTurn with
                | Human -> gameState.HumanPlayer
                | AI    -> gameState.AIPlayer

            // 1. Enforce physical wall inventory tracking
            if currentPlayerState.RemainingWalls <= 0 then
                Error "You have no remaining walls to place!"
            else
                let proposedWall = { Orientation = orientation; Row = r; Col = c }
                
                // 2. Validate geometry & path restrictions using Phase 3 engine
                if not (BoardLogic.isValidWallPlacement proposedWall gameState) then
                    Error "Invalid wall alignment! It either overlaps/crosses an existing wall, is out of bounds, or traps a player."
                else
                    // 3. Complete structural transition & decrement inventory
                    let nextState =
                        match gameState.CurrentTurn with
                        | Human ->
                            { gameState with
                                Walls = proposedWall :: gameState.Walls
                                HumanPlayer = { gameState.HumanPlayer with RemainingWalls = gameState.HumanPlayer.RemainingWalls - 1 }
                                CurrentTurn = toggleTurn gameState.CurrentTurn }
                        | AI ->
                            { gameState with
                                Walls = proposedWall :: gameState.Walls
                                AIPlayer = { gameState.AIPlayer with RemainingWalls = gameState.AIPlayer.RemainingWalls - 1 }
                                CurrentTurn = toggleTurn gameState.CurrentTurn }
                    Ok nextState