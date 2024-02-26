# Unity A* Pathfinding With Bezier Curve Path Smoothing
This project is aimed at creating a grid generator that mimics a basic form of a NavMesh. the 2D grid can be set to a width and height, and scaled to make each cell a range of sizes to best suite a given map. It comes with an A* based path finding system to find the shortest path around obstacles populated on the grid. Finally, a path smoothing function is applied using Bezier curves to smooth the path the player takes along the provided A* path. This is to mitigate much of the robotic movement of following a grid-based path. 

## Running and Testing the project
1. Open up the project and enter play mode
2. The default grid is a 50x50 with cell size of 1. I feel cell size 1 is a tad small for some testing cases so I enter size 3 to make the cells larger. This will also make the walls scale up in size to reflect the size of each cell.
3. Controls will be found in the top left of the screen.
4. Once your grid size is generated, create any number of walls (black cubes) and test the pathfinding algroithm.
5. The black debug line is the main A* path initially generated. The Blue line is the Bezier Smoothed path the player follows.

## Final Notes
This project is at about 90-95% where I want it. The path smoothing handles almost all path scenarios, however certain complex paths with multiple back to back turns sometimes showcases this path smoothing function's limitations. I'm researching more dynamic ways to handle these specific scenarios. I have some other optimizations I'd like to make in the future as well which I'm happy to share while reviewing the project.

**Update 1: Added a Pacman-like maze wall generation function that will randomly generate walls throughout the grid. Assigned the action to the Spacebar.
