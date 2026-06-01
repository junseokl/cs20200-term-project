namespace QuoridorGame.Core

module BoardLogic =

    /// Checks if a position is within the bounds of the 9x9 board.
    let inBounds (pos: Position) : bool =
        pos.Row >= 0 && pos.Row <= 8 && pos.Col >= 0 && pos.Col <= 8

    /// Validates if a wall's placement indices are between 0 and 7.
    let isValidWallRange (wall: Wall) : bool =
        wall.Row >= 0 && wall.Row <= 7 && wall.Col >= 0 && wall.Col <= 7

    /// Determines if two walls physically overlap or cross at the same intersection.
    let wallsIntersect (w1: Wall) (w2: Wall) : bool =
        if w1.Row = w2.Row && w1.Col = w2.Col then
            true // Share the exact same intersection vertex (crosses or overlaps)
        elif w1.Orientation = Horizontal && w2.Orientation = Horizontal && w1.Row = w2.Row && abs (w1.Col - w2.Col) = 1 then
            true // Overlapping horizontal wings
        elif w1.Orientation = Vertical && w2.Orientation = Vertical && w1.Col = w2.Col && abs (w1.Row - w2.Row) = 1 then
            true // Overlapping vertical wings
        else
            false

    /// Checks if there is a wall blocking direct orthogonal movement between two adjacent cells.
    let isBlockedByWall (fromPos: Position) (toPos: Position) (walls: Wall list) : bool =
        let r1, c1 = fromPos.Row, fromPos.Col
        let r2, c2 = toPos.Row, toPos.Col
        
        if r1 = r2 && c2 = c1 + 1 then // Moving Right
            walls |> List.exists (fun w -> w.Orientation = Vertical && w.Col = c1 && (w.Row = r1 || w.Row = r1 - 1))
        elif r1 = r2 && c2 = c1 - 1 then // Moving Left
            walls |> List.exists (fun w -> w.Orientation = Vertical && w.Col = c2 && (w.Row = r1 || w.Row = r1 - 1))
        elif c1 = c2 && r2 = r1 + 1 then // Moving Down
            walls |> List.exists (fun w -> w.Orientation = Horizontal && w.Row = r1 && (w.Col = c1 || w.Col = c1 - 1))
        elif c1 = c2 && r2 = r1 - 1 then // Moving Up
            walls |> List.exists (fun w -> w.Orientation = Horizontal && w.Row = r2 && (w.Col = c1 || w.Col = c1 - 1))
        else
            false

    /// Calculates the resulting position if a specific movement direction is attempted.
    /// Returns Some position if valid under board geometry/rules, or None if illegal.
    let getMoveDestination (current: Position) (dir: Direction) (opponent: Position) (walls: Wall list) : Position option =
        let r, c = current.Row, current.Col
        
        // Internal helper: Evaluates standard step vs a straight leap over an opponent
        let checkOrthogonal (nextRow: int) (nextCol: int) (jumpRow: int) (jumpCol: int) =
            let nextPos = { Row = nextRow; Col = nextCol }
            if not (inBounds nextPos) || isBlockedByWall current nextPos walls then
                None
            elif nextPos = opponent then
                let jumpPos = { Row = jumpRow; Col = jumpCol }
                if inBounds jumpPos && not (isBlockedByWall opponent jumpPos walls) then
                    Some jumpPos
                else
                    None // Straight jump path is blocked by a wall or board edge
            else
                Some nextPos

        // FIXED Internal helper: Evaluates special diagonal rules
        let checkDiagonal (oppRow: int) (oppCol: int) (jumpRow: int) (jumpCol: int) (diagRow: int) (diagCol: int) =
            let oppPos = { Row = oppRow; Col = oppCol }
            
            // CRITICAL FIX 1: We MUST verify the opponent is actually standing on this pivot cell!
            if oppPos <> opponent then 
                None
            else
                let jumpPos = { Row = jumpRow; Col = jumpCol }
                let diagPos = { Row = diagRow; Col = diagCol }
                
                // CRITICAL FIX 2: Verify no wall blocks the approach to the opponent
                if isBlockedByWall current oppPos walls then 
                    None
                else
                    // Verify the straight jump over the opponent is blocked by a wall or board edge
                    let straightJumpBlocked = not (inBounds jumpPos) || isBlockedByWall oppPos jumpPos walls
                    
                    if straightJumpBlocked then
                        // Verify the sideways diagonal step off the opponent is clear
                        if inBounds diagPos && not (isBlockedByWall oppPos diagPos walls) then
                            Some diagPos
                        else None
                    else None

        match dir with
        | Up    -> checkOrthogonal (r - 1) c (r - 2) c
        | Down  -> checkOrthogonal (r + 1) c (r + 2) c
        | Left  -> checkOrthogonal r (c - 1) r (c - 2)
        | Right -> checkOrthogonal r (c + 1) r (c + 2)
        
        | UpLeft ->
            match checkDiagonal (r - 1) c (r - 2) c (r - 1) (c - 1) with
            | Some p -> Some p
            | None   -> checkDiagonal r (c - 1) r (c - 2) (r - 1) (c - 1)

        | UpRight ->
            match checkDiagonal (r - 1) c (r - 2) c (r - 1) (c + 1) with
            | Some p -> Some p
            | None   -> checkDiagonal r (c + 1) r (c + 2) (r - 1) (c + 1)

        | DownLeft ->
            match checkDiagonal (r + 1) c (r + 2) c (r + 1) (c - 1) with
            | Some p -> Some p
            | None   -> checkDiagonal r (c - 1) r (c - 2) (r + 1) (c - 1)

        | DownRight ->
            match checkDiagonal (r + 1) c (r + 2) c (r + 1) (c + 1) with
            | Some p -> Some p
            | None   -> checkDiagonal r (c + 1) r (c + 2) (r + 1) (c + 1)


    /// Retrieves all valid orthogonally adjacent neighbor cells that are not blocked by a wall.
    /// Note: Opponent pawns are ignored for pathfinding validation per Quoridor rules.
    let getWalkableNeighbors (current: Position) (walls: Wall list) : Position list =
        let r, c = current.Row, current.Col
        let potentialDirections = [
            { Row = r - 1; Col = c } // Up
            { Row = r + 1; Col = c } // Down
            { Row = r; Col = c - 1 } // Left
            { Row = r; Col = c + 1 } // Right
        ]
        
        potentialDirections
        |> List.filter (fun neighbor -> inBounds neighbor && not (isBlockedByWall current neighbor walls))

    /// Evaluates if a target player can successfully reach their respective victory baseline.
    let hasPathToGoal (startPos: Position) (player: PlayerType) (walls: Wall list) : bool =
        let isGoal (pos: Position) =
            match player with
            | Human -> pos.Row = 0 // Top row victory
            | AI    -> pos.Row = 8 // Bottom row victory

        // Functional BFS tracking a structural queue and a visited coordinate set
        let rec bfs (queue: Position list) (visited: Set<Position>) : bool =
            match queue with
            | [] -> false // Queue empty; no valid paths remaining
            | head :: tail ->
                if isGoal head then 
                    true // Goal reached!
                elif Set.contains head visited then 
                    bfs tail visited // Skip already checked nodes
                else
                    let neighbors = getWalkableNeighbors head walls
                    let unvisitedNeighbors = neighbors |> List.filter (fun n -> not (Set.contains n visited))
                    
                    // Add current node to visited and append neighbors to the end of the queue
                    let nextVisited = Set.add head visited
                    let nextQueue = tail @ unvisitedNeighbors
                    bfs nextQueue nextVisited

        bfs [startPos] Set.empty

    /// Comprehensive check determining if a newly proposed wall placement is legal.
    let isValidWallPlacement (newWall: Wall) (gameState: GameState) : bool =
        // 1. Is the wall layout within the 0-7 boundary limit?
        if not (isValidWallRange newWall) then 
            false
        
        // 2. Does it overlap or cross an existing wall?
        elif gameState.Walls |> List.exists (fun existingWall -> wallsIntersect existingWall newWall) then 
            false
        
        else
            // 3. Temporarily inject the wall to verify it doesn't trap either player
            let simulatedWalls = newWall :: gameState.Walls
            let humanHasPath = hasPathToGoal gameState.HumanPlayer.Position Human simulatedWalls
            let aiHasPath = hasPathToGoal gameState.AIPlayer.Position AI simulatedWalls
            
            humanHasPath && aiHasPath

    /// Calculates the exact number of steps required to reach the victory row.
    let getShortestPathDistance (startPos: Position) (player: PlayerType) (walls: Wall list) : int option =
        let isGoal (pos: Position) =
            match player with
            | Human -> pos.Row = 0
            | AI    -> pos.Row = 8

        // BFS queue tracks the coordinate AND the distance traveled
        let rec bfs (queue: (Position * int) list) (visited: Set<Position>) : int option =
            match queue with
            | [] -> None // No path found
            | (pos, dist) :: tail ->
                if isGoal pos then Some dist
                elif Set.contains pos visited then bfs tail visited
                else
                    let neighbors = getWalkableNeighbors pos walls
                    let unvisited = neighbors |> List.filter (fun n -> not (Set.contains n visited))
                    let newItems = unvisited |> List.map (fun n -> (n, dist + 1))
                    
                    bfs (tail @ newItems) (Set.add pos visited)

        bfs [(startPos, 0)] Set.empty