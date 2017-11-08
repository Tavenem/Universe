using System.Collections.Generic;

namespace WorldFoundry.WorldGrid
{
    internal interface IGrid
    {
        List<Corner> Corners { get; }
        List<Edge> Edges { get; }
        List<Tile> Tiles { get; }
    }
}
