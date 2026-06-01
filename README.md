# Quoridor Game in F#

**KAIST CS20200: Programming Principles — Term Project (Spring 2026)** 

* **Author:** Junseok Lee 

* **Student ID:** 20250575 

* **Implementation Language:** F# 

* **Target Runtime:** .NET 10 

---

## 1. Project Overview

This project provides a complete, production-ready console implementation of the classic strategy board game **Quoridor**. The application features a fully rule-compliant simulation engine , a clean scaled ASCII renderer , and an advanced Monte Carlo Tree Search (MCTS) inspired AI engine that explores deep futures to challenge human players.

### Core Game Specifications

* **The Grid:** The game is played on a 0-indexed 9x9 cell grid.

* **Starting Positions:** The Human player controls the pawn denoted as `•` and starts at the center of the bottom row `(8, 4)`. The AI opponent controls the pawn denoted as `o` and starts at the center of the top row `(0, 4)`.

* **Victory Conditions:** The Human wins instantly by advancing their pawn to any cell along the top row (`Row 0`). The AI wins by reaching any cell along the bottom row (`Row 8`).

* **Turn Sequence:** Turns alternate sequentially. The Human player moves first. On any given turn, a player must choose exactly one valid action: move their pawn orthogonally/diagonally or place a wall.

* **Wall Inventories:** Each player starts with an asset pool of exactly 10 walls. Placed walls span two consecutive cell edges, permanently blocking pawn movement across those lines. Walls cannot overlap or cross each other, and a wall cannot be placed if it completely blocks either player's path to their respective goal row.

---

## 2. Compilation and Execution Guide

This project relies entirely on standard cross-platform `.NET CLI` tooling with zero external system dependencies.

### Prerequisites

Verify that the core .NET SDK runtime environment is accessible on your machine:

```bash
dotnet --version

```

### Build and Compilation

Navigate to the repository's root directory containing the project configuration file and execute a clean compilation:

```bash
dotnet build

```

### Running the Game

To boot up the complete interactive game loop, execute the run command from your terminal entry point:

```bash
dotnet run

```

Upon launching the game, you will be prompted to select your AI opponent:
* **1) MCTS AI:** A fast, aggressive algorithm using Monte Carlo Tree Search with a specialized "Pawn Race" heuristic rollout.
* **2) Minimax AI:** A deeply calculating algorithm utilizing Alpha-Beta Pruning and Iterative Deepening to foresee complex, multi-wall traps.

> **Note on AI Selection (Beyond Proposal):** The option to choose between two different AI models was not originally included in the project proposal. It was added to provide players with varying challenges and distinct playstyles. The dual-AI system allows users to test their skills against both a fast, aggressive heuristic approach (MCTS) and a deep, calculating trap-setter (Minimax).

The game window will clear automatically, display the commands index, and output the interactive game board.

---

## 3. Project Architecture

F# compiles code linearly, requiring dependent modules to be listed strictly from top to bottom. The layout inside your project file (`QuoridorGame.fsproj`) is sequenced as follows:

```xml
  <ItemGroup>
    <Compile Include="Domain.fs" />
    <Compile Include="BoardLogic.fs" />
    <Compile Include="GameEngine.fs" />
    <Compile Include="Display.fs" />
    <Compile Include="AI.fs" />
    <Compile Include="Program.fs" />
  </ItemGroup>

```

### Module Roles

* **`Domain.fs`:** Defines type parameters, core structures (`Position`, `Wall`), player asset states, and the `GameAction` union.

* **`BoardLogic.fs`:** Handles cell adjacency boundaries, wall collision rules, special jump kinematics, and BFS pathfinding verification.

* **`GameEngine.fs`:** Coordinates pure state transitions by executing actions and safely updating immutable records or returning error text flags.

* **`Display.fs`:** Parses string text streams and handles real-time visual grid adjustments.

* **`AI.fs`:** The tactical logic engine that conducts deep simulations across prospective grid nodes to extract the optimal move path.

* **`Program.fs`:** The central runtime executable loop managing turn progression, error output passing, and victory notifications.

---

## 4. Command Interface and Grammar Rules

When it is your turn, the application pauses and waits for text entry. Input tokens are case-insensitive and safely handled by an internal parser array.

### Valid Command Dictionary

| Input Command String | Resulting Game Action | Validation Rule Constraints |
| --- | --- | --- |
| `move up` | Moves pawn 1 cell north. | Blocked if cut off by a horizontal wall or board limit. |
| `move down` | Moves pawn 1 cell south. | Blocked if cut off by a horizontal wall or board limit. |
| `move left` | Moves pawn 1 cell west. | Blocked if cut off by a vertical wall or board limit. |
| `move right` | Moves pawn 1 cell east. | Blocked if cut off by a vertical wall or board limit. |
| `move up-left` | Jumps pawn diagonally north-west. | Allowed if opponent is adjacent and straight jump path is blocked. |
| `move up-right` | Jumps pawn diagonally north-east. | Allowed if opponent is adjacent and straight jump path is blocked. |
| `move down-left` | Jumps pawn diagonally south-west. | Allowed if opponent is adjacent and straight jump path is blocked. |
| `move down-right` | Jumps pawn diagonally south-east. | Allowed if opponent is adjacent and straight jump path is blocked. |
| `wall h <r> <c>` | Places a horizontal wall at intersection $(r, c)$. | Bounds: $0 \le r, c \le 7$. Blocks vertical steps between rows $r$ and $r+1$. |
| `wall v <r> <c>` | Places a vertical wall at intersection $(r, c)$. | Bounds: $0 \le r, c \le 7$. Blocks horizontal steps between cols $c$ and $c+1$. |
| `quit` | Gracefully closes the active game match. | Exits the command line context cleanly. |

---

## 5. Large Language Model (LLM) Attribution

### LLM Specifications Used

* **Model Name:** Gemini 3.5 Flash Extended

### Practical Scope of LLM Assistance

The LLM was engaged as an automated boilerplate and syntax generation assistant. It helped draft structural code frameworks translating basic properties into initial F# types, provided starting layouts for simple text token array splitting, and generated basic nested console printing statements.

### Strategic Innovations & Essential Engineering Work Contributed by the User

The tactical strength, architectural resilience, and fluid presentation of this game are the explicit result of extensive architectural modifications, manual overrides, and conceptual feature design driven entirely by the user. The core developments spearheaded by the user to fix significant omissions or limitations in the model's output include:

1. 
**The MCTS Strategy Blueprint Integration:** The user initiated a complete structural exploration of deep-searching timeline verification methods, shifting the project toward an advanced strategy engine modeled after modern Monte Carlo Tree Search optimization frameworks. The user structured the system to output real-time metric feedback tuples (`aiAction, mctsDepth`), revealing exactly how many alternative simulated futures were explored prior to executing each calculated move.

2. 
**Type System Namespace Resolution (The `GameAction` Refactor):** The LLM initially compiled the main actions discriminated union using the name `Action`. This generated severe build errors by colliding directly with the core .NET base runtime library delegate definition (`System.Action`) when standard system libraries were opened. The user resolved this type pollution crash by refactoring the domain to use a distinct `GameAction` declaration.

3. 
**Record Field Collision Resolution via Constraints:** The LLM designed two structural types (`Position` and `Wall`) that shared identical `.Row` and `.Col` property naming conventions. This triggered severe type-inference compilation failures because the F# type checker defaults to matching fields against whichever record was specified last. The user identified this constraint limitation and manually integrated strict type definitions (such as explicitly specifying `pairs: Position list`) to restore correct build routing.

4. 
**Soft Terminal Frame Clearing and Input Buffering:** The LLM's early versions dropped refreshed boards continually down the terminal window or used harsh system commands that completely deleted scroll history. The user engineered a fluid alternative by leveraging native ANSI escape strings (`\x1b[2J\x1b[H`) coupled with a stateful `string option` messaging layer inside the recursive loop. This allows errors and real-time AI metrics to render at the top of a clean visual workspace while completely preserving standard scrollable terminal turn history.

5. 
**Non-Bleeding Visual Gap Enhancement:** The initial rendering layout melded separate, adjacent wall markers into large, confusing blocks of text. The user designed a unique row gap check inside the console pipeline that skips secondary alignment lookups. This isolates walls to exactly their two-cell boundary footprints and leaves a clean 1-character visual gap between separate pieces, vastly improving board scannability.