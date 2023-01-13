using System.Security.Cryptography.X509Certificates;

using _3DPrint.NET.Data;
using _3DPrint.NET.Services;
using Blazor3D.Maths;
using Microsoft.AspNetCore.Mvc;

namespace _3DPrint.NET.Api;
[Route("/Api/Stl")]
public class Stl : Controller {
    private readonly Printer _printer;

    public Stl(Printer printer) {
        _printer= printer;
    }

    [HttpGet("leveling")]
    public async Task<string> Leveling() {
        double[,] mesh = await _printer.GetBedlevelingMeshAsync();
        var vectors = new Vector3[mesh.GetLength(0), mesh.GetLength(1)];

        for(int x = 0; x < mesh.GetLength(0); x++) {
            for (int y = 0; y < mesh.GetLength(1); y++) {
                vectors[x, y] = new Vector3(x, y, (float)mesh[x, y]);
            }
        }

        return StlCreator.Create(vectors);
    }
}
