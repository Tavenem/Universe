using System.Collections.Generic;

namespace WorldFoundry.Grid
{
    internal interface IGrid
    {
        List<Corner> Corners { get; }
        List<Edge> Edges { get; }
        List<Tile> Tiles { get; }
    }
}
