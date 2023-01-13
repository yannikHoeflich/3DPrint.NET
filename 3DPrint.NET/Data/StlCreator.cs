using System.Reflection.Metadata.Ecma335;
using System.Text;

using Blazor3D.Maths;

namespace _3DPrint.NET.Data;
public static class StlCreator {
    public static string Create(Vector3[,] vectors) {
        StringBuilder builder = new StringBuilder();
        AppenLine(builder, 0, "solid test");

        int width = vectors.GetLength(0);
        int height = vectors.GetLength(1);

        int subX = height / 2;
        int subY = width / 2;

        for(int x = 0; x < width - 1; x++) {
            for (int y = 0; y < height - 1; y++) {
                Vector3 v1 = vectors[x    , y];
                Vector3 v2 = vectors[x + 1, y];
                Vector3 v3 = vectors[x    , y + 1];
                Vector3 v4 = vectors[x + 1, y + 1];
                AppendTriangle(builder, v1, v2, v4, subX, subY);
                AppendTriangle(builder, v1, v4, v3, subX, subY);
            }
        }

        AppenLine(builder, 0, "endsolid test");

        return builder.ToString();
    }

    private static void AppenLine(StringBuilder builder, int indentation, string line) {
        builder.Append(new String(Enumerable.Repeat(' ', indentation).ToArray()));
        builder.AppendLine(line);
    }

    private static void AppendTriangle(StringBuilder builder, Vector3 v1, Vector3 v2, Vector3 v3, float subX, float subY) {
        AppenLine(builder, 1, "facet normal 0, 0, 0");
        AppenLine(builder, 2, "outer loop");

        AppenLine(builder, 3, $"vertex {v1.X - subX} {v1.Y - subY} {v1.Z}");
        AppenLine(builder, 3, $"vertex {v2.X - subX} {v2.Y - subY} {v2.Z}");
        AppenLine(builder, 3, $"vertex {v3.X - subX} {v3.Y - subY} {v3.Z}");

        AppenLine(builder, 2, "endloop");
        AppenLine(builder, 1, "endfacet");
    }
}
