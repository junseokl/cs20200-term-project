module QuoridorGame.Program

open System
open System.Threading
open QuoridorGame.Core

[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    
    Console.Clear()
    printfn "===================================="
    printfn "       WELCOME TO QUORIDOR          "
    printfn "===================================="
    printfn "Please select your AI opponent:"
    printfn "1) MCTS AI"
    printfn "2) Minimax AI"
    printf "Enter choice (1 or 2): "
    
    let mutable aiChoice = ""
    let mutable valid = false
    while not valid do
        aiChoice <- Console.ReadLine()
        if aiChoice = "1" || aiChoice = "2" then valid <- true
        else printf "Invalid choice. Please enter 1 or 2: "
        
    let aiChooseAction = 
        if aiChoice = "1" then AI.chooseActionMCTS
        else AI.chooseActionMinimax

    let aiName = if aiChoice = "1" then "MCTS AI" else "Minimax AI"

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
                printf "🤖 %s is exploring deep futures... " aiName
                Console.Out.Flush() 
                
                // Fetch the calculated action and the total MCTS iteration depth count
                let aiAction, metric = aiChooseAction state
                
                match aiAction with
                | Move dir ->
                    let decisionMsg = sprintf "🤖 %s (metric: %d) decided to move: %A" aiName metric dir
                    match GameEngine.tryApplyAction state aiAction with
                    | Ok nextState -> gameLoop nextState (Some decisionMsg) 
                    | Error _      -> gameLoop state None
                | PlaceWall (o, r, c) ->
                    let orientationStr = match o with | Horizontal -> "H" | Vertical -> "V"
                    let decisionMsg = sprintf "🤖 %s (metric: %d) placed Wall: %s %d %d" aiName metric orientationStr r c
                    match GameEngine.tryApplyAction state aiAction with
                    | Ok nextState -> gameLoop nextState (Some decisionMsg) 
                    | Error _      -> gameLoop state None
                | _ -> 
                    gameLoop state None

    // Start execution with an empty initial notification context
    gameLoop initialState None
    0
