using Echo1.Core.Geometry;
using System.Globalization;
using System.Numerics;

namespace Echo1.Core.Import;

public static class ObjImporter
{
	public static RcsMesh Load(string path)
	{
		var verts = new List<Vector3>(4096);
		var facets = new List<Facet>();
		int idx = 0;

		foreach (var raw in File.ReadLines(path))
		{
			var line = raw.Trim();
			if (line.StartsWith("v "))
			{
				var tok = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				verts.Add(new Vector3(
					float.Parse(tok[1], CultureInfo.InvariantCulture),
					float.Parse(tok[2], CultureInfo.InvariantCulture),
					float.Parse(tok[3], CultureInfo.InvariantCulture)));
			}
			else if (line.StartsWith("f "))
			{
				var indices = ParseFaceIndices(line, verts.Count);
				// Fan triangulate: f 0,1,2  0,2,3  0,3,4 ...
				for (int i = 1; i < indices.Length - 1; i++)
					facets.Add(new Facet(idx++,
						verts[indices[0]],
						verts[indices[i]],
						verts[indices[i + 1]]));
			}
		}

		return new RcsMesh(Path.GetFileNameWithoutExtension(path), facets.ToArray());
	}

	private static int[] ParseFaceIndices(string line, int vertCount)
	{
		return line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1..]
			.Select(tok =>
			{
				int raw = int.Parse(tok.Split('/')[0], CultureInfo.InvariantCulture);
				return raw > 0 ? raw - 1 : vertCount + raw; // OBJ is 1-indexed
			})
			.ToArray();
	}
}