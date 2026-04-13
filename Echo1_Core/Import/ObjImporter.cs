// Echo1_RcsSimulator\Echo1_Core\Import\ObjImporter.cs
using Echo1.Core.Geometry;
using System.Globalization;
using System.Numerics;

namespace Echo1.Core.Import;

public static class ObjImporter
{
	// Note: This is a very basic OBJ loader that only supports vertices and faces. It does not handle normals, UVs, materials, or other features of the OBJ format.
	// It also assumes that faces are defined in a way that can be fan-triangulated (i.e., convex polygons). More complex OBJ files may not load correctly with this importer.
	//
	// OBJ files vary significantly between exporters and may contain inconsistencies (e.g., misaligned or non-manifold facets).
	//
	// Geometry issues in OBJ files can cause incorrect calculations, such as treating interior and exterior surfaces as continuous, resulting in inflated RCS (dBsm) values.
	//
	// If an OBJ fails to load or produces incorrect results:
	// → Re-export it as STL using a 3D modeling tool.
	// → Ensure the mesh is watertight and properly connected.
	public static RcsMesh Load(string path)
	{
		var verts = new List<Vector3>(4096);
		var facets = new List<Facet>();
		int idx = 0;

		// Read the OBJ file line by line to extract vertices and faces.
		foreach (var raw in File.ReadLines(path))
		{
			// Trim whitespace and check the line type (vertex or face).
			var line = raw.Trim();
			// Vertex lines start with "v " and are followed by three float values (x, y, z).
			if (line.StartsWith("v "))
			{
				// Split the vertex line into tokens and parse the coordinates.
				var tok = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				// Add the vertex to the list, converting from string to float using invariant culture.
				verts.Add(new Vector3(
					float.Parse(tok[1], CultureInfo.InvariantCulture),
					float.Parse(tok[2], CultureInfo.InvariantCulture),
					float.Parse(tok[3], CultureInfo.InvariantCulture)));
				// Face lines start with "f " and are followed by vertex indices (and optionally texture and normal indices).
			}
			else if (line.StartsWith("f "))
			{
				// Parse the face line to extract vertex indices.
				// The indices may be in the format "v", "v/vt", or "v/vt/vn".
				// We only care about the vertex index (the first part).
				var indices = ParseFaceIndices(line, verts.Count);
				// Fan triangulate: f 0,1,2  0,2,3  0,3,4 ...
				for (int i = 1; i < indices.Length - 1; i++)
					facets.Add(new Facet(idx++,
						verts[indices[0]],
						verts[indices[i]],
						verts[indices[i + 1]]));
				// Note: If the face is not a triangle, we create multiple facets by fan triangulation. This assumes the face is convex and properly defined.
			}
		}

		return new RcsMesh(Path.GetFileNameWithoutExtension(path), facets.ToArray());
	}

	// Helper method to parse face indices from a face line in the OBJ file.
	private static int[] ParseFaceIndices(string line, int vertCount)
	{
		// Split the face line into tokens, skipping the first token ("f").
		return line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1..]
			.Select(tok =>
			{
				// Each token may be in the format "v", "v/vt", or "v/vt/vn".
				// We only care about the vertex index (the first part).
				int raw = int.Parse(tok.Split('/')[0], CultureInfo.InvariantCulture);
				// OBJ indices can be positive (1-based) or negative (relative to the end of the vertex list).
				return raw > 0 ? raw - 1 : vertCount + raw; // OBJ is 1-indexed
			})
			.ToArray();
	}
}