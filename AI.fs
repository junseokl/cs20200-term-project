namespace QuoridorGame.Core

module AI =
    open System
    open System.Diagnostics

    let rnd = Random()

    let fastCheckWinner (state: GameState) =
        if state.HumanPlayer.Position.Row = 0 then Some Human
        elif state.AIPlayer.Position.Row = 8 then Some AI
        else None

    let fastGetDistancesAndPrev (targetRow: int) (walls: Wall list) : int[] * int[] =
        let dist = Array.create 81 -1
        let prev = Array.create 81 -1
        let q = Array.zeroCreate<Position> 81
        let mutable head = 0
        let mutable tail = 0
        
        for c in 0..8 do
            let p = { Row = targetRow; Col = c }
            q.[tail] <- p
            tail <- tail + 1
            dist.[p.Row * 9 + c] <- 0
            
        while head < tail do
            let curr = q.[head]
            head <- head + 1
            let idx = curr.Row * 9 + curr.Col
            let d = dist.[idx]
            let r, c = curr.Row, curr.Col
            
            if r > 0 && not (BoardLogic.isBlockedByWall curr {Row=r-1; Col=c} walls) then
                let nIdx = (r - 1) * 9 + c
                if dist.[nIdx] = -1 then 
                    dist.[nIdx] <- d + 1
                    prev.[nIdx] <- idx
                    q.[tail] <- {Row=r-1; Col=c}; tail <- tail + 1
            if r < 8 && not (BoardLogic.isBlockedByWall curr {Row=r+1; Col=c} walls) then
                let nIdx = (r + 1) * 9 + c
                if dist.[nIdx] = -1 then 
                    dist.[nIdx] <- d + 1
                    prev.[nIdx] <- idx
                    q.[tail] <- {Row=r+1; Col=c}; tail <- tail + 1
            if c > 0 && not (BoardLogic.isBlockedByWall curr {Row=r; Col=c-1} walls) then
                let nIdx = r * 9 + (c - 1)
                if dist.[nIdx] = -1 then 
                    dist.[nIdx] <- d + 1
                    prev.[nIdx] <- idx
                    q.[tail] <- {Row=r; Col=c-1}; tail <- tail + 1
            if c < 8 && not (BoardLogic.isBlockedByWall curr {Row=r; Col=c+1} walls) then
                let nIdx = r * 9 + (c + 1)
                if dist.[nIdx] = -1 then 
                    dist.[nIdx] <- d + 1
                    prev.[nIdx] <- idx
                    q.[tail] <- {Row=r; Col=c+1}; tail <- tail + 1
        dist, prev

    let fastGetDistances (targetRow: int) (walls: Wall list) : int[] =
        fst (fastGetDistancesAndPrev targetRow walls)

    let fastIsValidWallPlacement (newWall: Wall) (state: GameState) : bool =
        if newWall.Row < 0 || newWall.Row > 7 || newWall.Col < 0 || newWall.Col > 7 then false
        elif state.Walls |> List.exists (fun w -> BoardLogic.wallsIntersect w newWall) then false
        else
            let simulatedWalls = newWall :: state.Walls
            let d1 = fastGetDistances 0 simulatedWalls
            let d2 = fastGetDistances 8 simulatedWalls
            d1.[state.HumanPlayer.Position.Row * 9 + state.HumanPlayer.Position.Col] <> -1 &&
            d2.[state.AIPlayer.Position.Row * 9 + state.AIPlayer.Position.Col] <> -1

    let fastApplyAction (gameState: GameState) (action: GameAction) : Result<GameState, string> =
        match action with
        | Quit -> Error "QUIT"
        | Move dir ->
            let current = if gameState.CurrentTurn = Human then gameState.HumanPlayer else gameState.AIPlayer
            let opp = if gameState.CurrentTurn = Human then gameState.AIPlayer else gameState.HumanPlayer
            match BoardLogic.getMoveDestination current.Position dir opp.Position gameState.Walls with
            | Some newPos ->
                let nextTurn = if gameState.CurrentTurn = Human then AI else Human
                let nextState =
                    if gameState.CurrentTurn = Human then { gameState with HumanPlayer = { gameState.HumanPlayer with Position = newPos }; CurrentTurn = nextTurn }
                    else { gameState with AIPlayer = { gameState.AIPlayer with Position = newPos }; CurrentTurn = nextTurn }
                Ok nextState
            | None -> Error "Invalid move"
        | PlaceWall (o, r, c) ->
            let current = if gameState.CurrentTurn = Human then gameState.HumanPlayer else gameState.AIPlayer
            if current.RemainingWalls <= 0 then Error "No walls"
            else
                let w = { Orientation = o; Row = r; Col = c }
                if fastIsValidWallPlacement w gameState then
                    let nextTurn = if gameState.CurrentTurn = Human then AI else Human
                    let nextState =
                        if gameState.CurrentTurn = Human then { gameState with Walls = w :: gameState.Walls; HumanPlayer = { gameState.HumanPlayer with RemainingWalls = gameState.HumanPlayer.RemainingWalls - 1 }; CurrentTurn = nextTurn }
                        else { gameState with Walls = w :: gameState.Walls; AIPlayer = { gameState.AIPlayer with RemainingWalls = gameState.AIPlayer.RemainingWalls - 1 }; CurrentTurn = nextTurn }
                    Ok nextState
                else Error "Invalid wall"

    let getShortestPathCoords (startPos: Position) (targetRow: int) (walls: Wall list) =
        let dist, prev = fastGetDistancesAndPrev targetRow walls
        let startIdx = startPos.Row * 9 + startPos.Col
        if dist.[startIdx] = -1 then []
        else
            let rec loop currIdx acc =
                if currIdx = -1 then acc
                else
                    let r, c = currIdx / 9, currIdx % 9
                    let p = { Row = r; Col = c }
                    let d = dist.[currIdx]
                    if d = 0 then p :: acc
                    else loop prev.[currIdx] (p :: acc)
            loop startIdx [] |> List.rev

    let generateWallsBlockingPath (pathCoords: Position list) =
        let rec loop (coords: Position list) (acc: Wall list) =
            match coords with
            | p1 :: p2 :: tail ->
                let r1, c1 = p1.Row, p1.Col
                let r2, c2 = p2.Row, p2.Col
                let walls =
                    if r1 = r2 && c2 = c1 + 1 then
                        [ {Orientation=Vertical; Row=r1; Col=c1}; {Orientation=Vertical; Row=r1-1; Col=c1} ]
                    elif r1 = r2 && c2 = c1 - 1 then
                        [ {Orientation=Vertical; Row=r1; Col=c2}; {Orientation=Vertical; Row=r1-1; Col=c2} ]
                    elif c1 = c2 && r2 = r1 + 1 then
                        [ {Orientation=Horizontal; Row=r1; Col=c1}; {Orientation=Horizontal; Row=r1; Col=c1-1} ]
                    elif c1 = c2 && r2 = r1 - 1 then
                        [ {Orientation=Horizontal; Row=r2; Col=c1}; {Orientation=Horizontal; Row=r2; Col=c1-1} ]
                    else []
                loop (p2 :: tail) (walls @ acc)
            | _ -> acc
        loop pathCoords ([] : Wall list)
        |> List.filter (fun (w: Wall) -> w.Row >= 0 && w.Row <= 7 && w.Col >= 0 && w.Col <= 7)
        |> List.distinct

    let getExtensionWalls (walls: Wall list) =
        walls |> List.collect (fun w ->
            match w.Orientation with
            | Horizontal -> [ {Orientation=Horizontal; Row=w.Row; Col=w.Col-2}; {Orientation=Horizontal; Row=w.Row; Col=w.Col+2} ]
            | Vertical -> [ {Orientation=Vertical; Row=w.Row-2; Col=w.Col}; {Orientation=Vertical; Row=w.Row+2; Col=w.Col} ]
        ) |> List.filter (fun (w: Wall) -> w.Row >= 0 && w.Row <= 7 && w.Col >= 0 && w.Col <= 7)

    let getCandidateActions (state: GameState) (player: PlayerType) : GameAction list =
        let current = player
        let opp = if current = Human then AI else Human
        let playerState = if current = Human then state.HumanPlayer else state.AIPlayer
        let oppState = if current = Human then state.AIPlayer else state.HumanPlayer
        
        let targetRow = if current = Human then 0 else 8
        let oppTargetRow = if current = Human then 8 else 0
        
        let dists = fastGetDistances targetRow state.Walls
        let myDistBefore = dists.[playerState.Position.Row * 9 + playerState.Position.Col]
        
        let allDirs = [Up; Down; Left; Right; UpLeft; UpRight; DownLeft; DownRight]
        // ONLY valid pawn moves that physically reduce distance to goal (STRICT FORWARD MOVEMENT, NO DANCING)
        let optimalMoves = 
            allDirs |> List.choose (fun d -> 
                match BoardLogic.getMoveDestination playerState.Position d oppState.Position state.Walls with
                | Some dest -> 
                    let dist = dists.[dest.Row * 9 + dest.Col]
                    if dist < myDistBefore then Some (Move d) else None
                | None -> None
            )
            
        if playerState.RemainingWalls > 0 then
            let oppPath = getShortestPathCoords oppState.Position oppTargetRow state.Walls
            let blockingWalls = generateWallsBlockingPath oppPath
            let extensionWalls = getExtensionWalls state.Walls
            
            let potentialWalls = blockingWalls @ extensionWalls |> List.distinct
            let oppDistBefore = (fastGetDistances oppTargetRow state.Walls).[oppState.Position.Row * 9 + oppState.Position.Col]
            
            let validWalls = 
                potentialWalls
                |> List.filter (fun (w: Wall) -> w.Row >= 0 && w.Row <= 7 && w.Col >= 0 && w.Col <= 7)
                |> List.filter (fun (w: Wall) -> not (state.Walls |> List.exists (fun ew -> BoardLogic.wallsIntersect ew w)))
                |> List.choose (fun (w: Wall) ->
                    let testWalls = w :: state.Walls
                    let d1 = fastGetDistances 0 testWalls
                    let d2 = fastGetDistances 8 testWalls
                    let hDist = d1.[state.HumanPlayer.Position.Row * 9 + state.HumanPlayer.Position.Col]
                    let aDist = d2.[state.AIPlayer.Position.Row * 9 + state.AIPlayer.Position.Col]
                    
                    if hDist <> -1 && aDist <> -1 then
                        let oppDistAfter = if current = Human then aDist else hDist
                        let myDistAfter = if current = Human then hDist else aDist
                        
                        let oppDiff = oppDistAfter - oppDistBefore
                        let myDiff = myDistAfter - myDistBefore
                        
                        if oppDiff > 0 && oppDiff >= myDiff then 
                            Some (PlaceWall(w.Orientation, w.Row, w.Col), oppDiff - myDiff)
                        else None
                    else None
                )
                |> List.sortByDescending snd 
                |> List.truncate 5 
                |> List.map fst
                
            optimalMoves @ validWalls
        else
            optimalMoves

    // -----------------------------------------------------
    // MINIMAX AI WITH ALPHA-BETA (Black / AIPlayer Slot)
    // -----------------------------------------------------
    let evaluateMinimax (state: GameState) =
        match fastCheckWinner state with
        | Some AI -> 10000.0
        | Some Human -> -10000.0
        | None ->
            let aiDist = (fastGetDistances 8 state.Walls).[state.AIPlayer.Position.Row * 9 + state.AIPlayer.Position.Col]
            let humanDist = (fastGetDistances 0 state.Walls).[state.HumanPlayer.Position.Row * 9 + state.HumanPlayer.Position.Col]
            
            let distScore = float (humanDist - aiDist)
            let wallScore = float (state.AIPlayer.RemainingWalls - state.HumanPlayer.RemainingWalls) * 0.5
            distScore * 10.0 + wallScore

    let rec alphabeta (state: GameState) (depth: int) (alpha: float) (beta: float) (isMax: bool) (sw: Stopwatch) (timeLimit: int64) : float =
        if depth = 0 || fastCheckWinner state |> Option.isSome || sw.ElapsedMilliseconds > timeLimit then
            evaluateMinimax state
        else
            let actions = getCandidateActions state state.CurrentTurn
            if isMax then
                let mutable value = Double.NegativeInfinity
                let mutable a = alpha
                let mutable brk = false
                for act in actions do
                    if not brk && sw.ElapsedMilliseconds <= timeLimit then
                        match fastApplyAction state act with
                        | Ok nextState ->
                            let v = alphabeta nextState (depth - 1) a beta false sw timeLimit
                            value <- max value v
                            a <- max a value
                            if value >= beta then brk <- true
                        | Error _ -> ()
                value
            else
                let mutable value = Double.PositiveInfinity
                let mutable b = beta
                let mutable brk = false
                for act in actions do
                    if not brk && sw.ElapsedMilliseconds <= timeLimit then
                        match fastApplyAction state act with
                        | Ok nextState ->
                            let v = alphabeta nextState (depth - 1) alpha b true sw timeLimit
                            value <- min value v
                            b <- min b value
                            if value <= alpha then brk <- true
                        | Error _ -> ()
                value

    let chooseActionMinimax (gameState: GameState) : GameAction * int =
        let sw = Stopwatch.StartNew()
        let timeLimitMs = 2800L 
        
        let actions = getCandidateActions gameState gameState.CurrentTurn
        let mutable bestAction = if actions.Length > 0 then actions.Head else Move Down
        let mutable maxDepth = 1
        
        let isMax = gameState.CurrentTurn = AI 
        
        try
            while sw.ElapsedMilliseconds < timeLimitMs && maxDepth <= 15 do
                let mutable bestVal = if isMax then Double.NegativeInfinity else Double.PositiveInfinity
                let mutable currentBestAction = bestAction
                
                for act in actions do
                    if sw.ElapsedMilliseconds < timeLimitMs then
                        match fastApplyAction gameState act with
                        | Ok nextState ->
                            let v = alphabeta nextState (maxDepth - 1) Double.NegativeInfinity Double.PositiveInfinity (not isMax) sw timeLimitMs
                            if isMax then
                                if v > bestVal then
                                    bestVal <- v
                                    currentBestAction <- act
                            else
                                if v < bestVal then
                                    bestVal <- v
                                    currentBestAction <- act
                        | Error _ -> ()
                        
                if sw.ElapsedMilliseconds < timeLimitMs then
                    bestAction <- currentBestAction
                maxDepth <- maxDepth + 1
        with _ -> ()
        
        (bestAction, maxDepth - 1)


    // -----------------------------------------------------
    // MCTS AI WITH PAWN RACE HEURISTIC (White / HumanPlayer Slot)
    // -----------------------------------------------------
    type MCTSNode = {
        State: GameState
        Action: GameAction option
        Parent: MCTSNode option
        mutable Visits: int
        mutable Wins: float
        mutable UntriedActions: GameAction list
        mutable Children: MCTSNode list
    }

    let createNode state action parent untried = {
        State = state; Action = action; Parent = parent
        Visits = 0; Wins = 0.0; UntriedActions = untried; Children = []
    }

    let uctConst = 1.414

    let selectNode (node: MCTSNode) : MCTSNode =
        let rec loop n =
            if n.UntriedActions.Length > 0 || n.Children.Length = 0 then n
            else
                let totalVisits = float n.Visits
                let bestChild = 
                    n.Children |> List.maxBy (fun c -> 
                        let exploitation = c.Wins / float c.Visits
                        let uctScore = 
                            if n.State.CurrentTurn = Human then exploitation + uctConst * sqrt (log totalVisits / float c.Visits)
                            else (1.0 - exploitation) + uctConst * sqrt (log totalVisits / float c.Visits)
                        uctScore)
                loop bestChild
        loop node

    let expandNode (node: MCTSNode) : MCTSNode =
        if node.UntriedActions.Length > 0 then
            let action = node.UntriedActions.Head
            node.UntriedActions <- node.UntriedActions.Tail
            match fastApplyAction node.State action with
            | Ok nextState ->
                let untried = if fastCheckWinner nextState |> Option.isSome then [] else getCandidateActions nextState nextState.CurrentTurn
                let child = createNode nextState (Some action) (Some node) untried
                node.Children <- child :: node.Children
                child
            | Error _ -> node
        else node

    let simulateMCTS (startNode: MCTSNode) : float =
        let rec loop state depth =
            if depth > 40 then 0.5 
            else
                match fastCheckWinner state with
                | Some Human -> 1.0 
                | Some AI -> 0.0
                | None ->
                    let current = state.CurrentTurn
                    let playerState = if current = Human then state.HumanPlayer else state.AIPlayer
                    let oppState = if current = Human then state.AIPlayer else state.HumanPlayer
                    let targetRow = if current = Human then 0 else 8
                    
                    let dists = fastGetDistances targetRow state.Walls
                    let myDistBefore = dists.[playerState.Position.Row * 9 + playerState.Position.Col]
                    
                    let allDirs = [Up; Down; Left; Right; UpLeft; UpRight; DownLeft; DownRight]
                    let validMoves = 
                        allDirs |> List.choose (fun d -> 
                            match BoardLogic.getMoveDestination playerState.Position d oppState.Position state.Walls with
                            | Some dest -> Some (Move d, dists.[dest.Row * 9 + dest.Col])
                            | None -> None
                        )
                    
                    let optimalMoves = validMoves |> List.filter (fun (_, d) -> d < myDistBefore) |> List.map fst
                    let allValidMoves = validMoves |> List.map fst
                    
                    let chosen =
                        if optimalMoves.Length > 0 && rnd.NextDouble() < 0.95 then
                            optimalMoves.[rnd.Next(optimalMoves.Length)]
                        elif allValidMoves.Length > 0 then
                            allValidMoves.[rnd.Next(allValidMoves.Length)]
                        else Move Down 
                        
                    match fastApplyAction state chosen with
                    | Ok ns -> loop ns (depth + 1)
                    | Error _ -> 0.5
        loop startNode.State 0

    let backpropagate (node: MCTSNode) (result: float) =
        let rec loop nOpt =
            match nOpt with
            | Some n -> n.Visits <- n.Visits + 1; n.Wins <- n.Wins + result; loop n.Parent
            | None -> ()
        loop (Some node)

    let chooseActionMCTS (gameState: GameState) : GameAction * int =
        let rootUntried = getCandidateActions gameState gameState.CurrentTurn
        if rootUntried.Length = 0 then (Move Down, 0)
        elif rootUntried.Length = 1 then (rootUntried.Head, 0)
        else
            let root = createNode gameState None None rootUntried
            let sw = Stopwatch.StartNew()
            
            let timeLimitMs = 2800L 
            let mutable iterations = 0
            
            while sw.ElapsedMilliseconds < timeLimitMs do
                let leaf = selectNode root
                let expanded = expandNode leaf
                let result = simulateMCTS expanded
                backpropagate expanded result
                iterations <- iterations + 1
                
            let bestChild = root.Children |> List.maxBy (fun c -> c.Visits)
            (bestChild.Action.Value, iterations)

    // DEFAULT ACTION FOR NORMAL PLAY
    let chooseAction (gameState: GameState) = chooseActionMinimax gameState
