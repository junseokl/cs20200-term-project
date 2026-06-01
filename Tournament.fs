module QuoridorGame.Tournament
open System
open System.Threading
open QuoridorGame.Core
[<EntryPoint>]
let main argv =
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    let initialState = GameInitializer.createInitialState()
    let rec gameLoop (state: GameState) (msg: string option) : unit =
        // Optionally clear the console to make it look like an animation
        // Console.Clear() 
        Display.displayBoard state msg
        match GameEngine.checkWinner state with
        | Some Human -> printfn "🏆 Tournament Over! White AI (Human Slot) won!"
        | Some AI    -> printfn "🏆 Tournament Over! Black AI (AI Slot) won!"
        | None ->
            let aiName = if state.CurrentTurn = Human then "⚪ White AI" else "⚫ Black AI"
            printf "%s is exploring deep futures... " aiName
            Console.Out.Flush() 
            
            let aiAction, metrics = 
                if state.CurrentTurn = Human then AI.chooseActionMCTS state
                else AI.chooseActionMinimax state
            
            match aiAction with
            | Move dir ->
                let decisionMsg = sprintf "%s (metric: %d) decided to move: %A" aiName metrics dir
                match GameEngine.tryApplyAction state aiAction with
                | Ok nextState -> 
                    Thread.Sleep(500) // Small delay so the user can watch the flow
                    gameLoop nextState (Some decisionMsg) 
                | Error _ -> gameLoop state None
            | PlaceWall (o, r, c) ->
                let orientationStr = match o with | Horizontal -> "H" | Vertical -> "V"
                let decisionMsg = sprintf "%s (metric: %d) placed Wall: %s %d %d" aiName metrics orientationStr r c
                match GameEngine.tryApplyAction state aiAction with
                | Ok nextState -> 
                    Thread.Sleep(500) // Small delay
                    gameLoop nextState (Some decisionMsg) 
                | Error _ -> gameLoop state None
            | _ -> 
                gameLoop state None
    gameLoop initialState None
    0
