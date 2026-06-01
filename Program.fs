module QuoridorGame.Program

open System
open System.Threading
open QuoridorGame.Core

let runGame () =
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    let initialState = GameInitializer.createInitialState()

    // The game loop now retains an active message context string option
    let rec gameLoop (state: GameState) (msg: string option) : unit =
        // 1. Render layout with the active persistent message flag
        Display.displayBoard state msg

        // 2. Check for game termination
        match GameEngine.checkWinner state with
        | Some Human -> printfn "🎉 Congratulations! You navigated the board and beat the AI!"
        | Some AI    -> printfn "🤖 Game Over! The AI reached the backline first."
        | None ->
            match state.CurrentTurn with
            | Human ->
                printf "Your Turn > "
                let input = Console.ReadLine()
                
                match Display.parseInput input with
                | None ->
                    let err = "❌ Invalid command format! Example: 'move up' or 'wall h 3 4'"
                    gameLoop state (Some err) // Loop with error message
                | Some Quit ->
                    printfn "👋 Game terminated by user. Goodbye!"
                | Some action ->
                    match GameEngine.tryApplyAction state action with
                    | Error "QUIT" -> 
                        printfn "👋 Game terminated by user. Goodbye!"
                    | Error errorMsg ->
                        let formattedError = sprintf "❌ Rule Violation: %s" errorMsg
                        gameLoop state (Some formattedError) // Pass validation error up
                    | Ok nextState ->
                        gameLoop nextState None // Smooth update, clear existing message

            | AI ->
                // Print analyzing status directly at the bottom of the current frame
                printf "🤖 AI is exploring deep futures... "
                Console.Out.Flush() 
                
                // Fetch the calculated action and the total MCTS iteration depth count
                let aiAction, mctsDepth = AI.chooseAction state
                
                match aiAction with
                | Move dir ->
                    let decisionMsg = sprintf "🤖 AI analyzed %d timelines and decided to move: %A" mctsDepth dir
                    match GameEngine.tryApplyAction state aiAction with
                    | Ok nextState -> gameLoop nextState (Some decisionMsg) 
                    | Error _      -> gameLoop state None
                | PlaceWall (o, r, c) ->
                    let orientationStr = match o with | Horizontal -> "H" | Vertical -> "V"
                    let decisionMsg = sprintf "🤖 AI analyzed %d timelines and placed Wall: %s %d %d" mctsDepth orientationStr r c
                    match GameEngine.tryApplyAction state aiAction with
                    | Ok nextState -> gameLoop nextState (Some decisionMsg) 
                    | Error _      -> gameLoop state None
                | _ -> 
                    gameLoop state None

    // Start execution with an empty initial notification context
    gameLoop initialState None
    0