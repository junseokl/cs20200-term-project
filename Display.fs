namespace QuoridorGame.Core

open System

module Display =

    /// Renders the layout including commands, notifications, and the 9x9 board grid.
    let displayBoard (state: GameState) (msg: string option) : unit =
        // 1. Clear terminal view and reposition cursor to top-left (0,0)
        printf "\x1b[2J\x1b[H"

        // 2. Always print the standard commands instructions at the very top
        printfn "================================================="
        printfn "          WELCOME TO QUORIDOR IN F#              "
        printfn "================================================="
        printfn " Commands:"
        printfn "   move up / down / left / right"
        printfn "   move up-left / up-right / down-left / down-right"
        printfn "   wall h <row> <col>  |  wall v <row> <col>"
        printfn "   quit"
        printfn "================================================="
        
        // 3. Print any active alerts (AI movements or human errors) directly below instructions
        match msg with
        | Some actualMessage -> 
            printfn "%s" actualMessage
            printfn "================================================="
        | None -> ()

        // 4. Render Row Cell Line & Gap Lines
        printfn "     0   1   2   3   4   5   6   7   8"
        printfn "   ┌───────────────────────────────────┐"
        
        for r in 0 .. 8 do
            printf " %d │" r
            for c in 0 .. 8 do
                let pos = { Row = r; Col = c }
                if state.HumanPlayer.Position = pos then printf " • "
                elif state.AIPlayer.Position = pos then printf " o "
                else printf " . "
                
                if c < 8 then
                    let hasVWall = state.Walls |> List.exists (fun w -> w.Orientation = Vertical && w.Col = c && (w.Row = r || w.Row = r - 1))
                    if hasVWall then printf "│" else printf " "
            printfn "│"
            
            if r < 8 then
                printf "   │"
                for c in 0 .. 8 do
                    // Draw horizontal wall segment under cell (r, c)
                    let hasHWall = state.Walls |> List.exists (fun w -> w.Orientation = Horizontal && w.Row = r && (w.Col = c || w.Col = c - 1))
                    if hasHWall then printf "───" else printf "   "
                    
                    // NEW CLEAN GAP LOGIC: 
                    // Only draw a continuous bridge if it's the exact internal center vertex of a wall piece.
                    if c < 8 then
                        let exactH = state.Walls |> List.exists (fun w -> w.Orientation = Horizontal && w.Row = r && w.Col = c)
                        let exactV = state.Walls |> List.exists (fun w -> w.Orientation = Vertical && w.Row = r && w.Col = c)
                        
                        if exactH then printf "─"
                        elif exactV then printf "│"
                        else printf " " // Leaves a pristine gap between separate back-to-back walls
                printfn "│"
                
        printfn "   └───────────────────────────────────┘"
        printfn "   [REMAINING WALLS]  Human: %d / 10  |  AI: %d / 10" state.HumanPlayer.RemainingWalls state.AIPlayer.RemainingWalls
        printfn "   [CURRENT TURN]     %A" state.CurrentTurn
        printfn "─────────────────────────────────────────────────"

    /// Parses string inputs into structured GameAction configurations.
    let parseInput (input: string) : GameAction option =
        let tokens = input.Trim().ToLower().Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        match tokens with
        | [| "quit" |] -> Some Quit
        | [| "move"; "up" |]         -> Some (Move Up)
        | [| "move"; "down" |]       -> Some (Move Down)
        | [| "move"; "left" |]       -> Some (Move Left)
        | [| "move"; "right" |]      -> Some (Move Right)
        | [| "move"; "up-left" |]    -> Some (Move UpLeft)
        | [| "move"; "up-right" |]   -> Some (Move UpRight)
        | [| "move"; "down-left" |]  -> Some (Move DownLeft)
        | [| "move"; "down-right" |] -> Some (Move DownRight)
        | [| "wall"; "h"; rStr; cStr |] ->
            match Int32.TryParse(rStr), Int32.TryParse(cStr) with
            | (true, r), (true, c) -> Some (PlaceWall (Horizontal, r, c))
            | _ -> None
        | [| "wall"; "v"; rStr; cStr |] ->
            match Int32.TryParse(rStr), Int32.TryParse(cStr) with
            | (true, r), (true, c) -> Some (PlaceWall (Vertical, r, c))
            | _ -> None
        | _ -> None