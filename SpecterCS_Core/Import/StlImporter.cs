// Echo1_RcsSimulator\Echo1_Core\Import\StlImporter.cs
using Echo1.Core.Geometry;
using System.Globalization;
using System.Numerics;

namespace Echo1.Core.Import;

public static class StlImporter
{
	public static RcsMesh Load(string path)
	{
		// Detect binary vs ASCII
		var bytes = File.ReadAllBytes(path);
		return IsBinaryStl(bytes) ? LoadBinary(path, bytes) : LoadAscii(path);
	}

	private static bool IsBinaryStl(byte[] data)
	{
		// ASCII STL starts with "solid"; binary has 80-byte header then uint32 count
		if (data.Length < 84) return false;
		var header = System.Text.Encoding.ASCII.GetString(data, 0, 5);
		return !header.StartsWith("solid", StringComparison.OrdinalIgnoreCase);
	}

	private static RcsMesh LoadBinary(string path, byte[] data)
	{
		// 80 bytes header + 4 bytes count + (50 bytes per triangle)
		uint count = BitConverter.ToUInt32(data, 80);
		var facets = new Facet[count];
		int offset = 84;

		for (int i = 0; i < count; i++)
		{
			// Skip normal (12 bytes) — we recompute from vertices
			offset += 12;
			var v0 = ReadVec3(data, ref offset);
			var v1 = ReadVec3(data, ref offset);
			var v2 = ReadVec3(data, ref offset);
			offset += 2; // attribute byte count
			facets[i] = new Facet(i, v0, v1, v2);
		}

		return new RcsMesh(Path.GetFileNameWithoutExtension(path), facets);
	}

	private static Vector3 ReadVec3(byte[] data, ref int offset)
	{
		float x = BitConverter.ToSingle(data, offset);
		float y = BitConverter.ToSingle(data, offset + 4);
		float z = BitConverter.ToSingle(data, offset + 8);
		offset += 12;
		return new Vector3(x, y, z);
	}

	private static RcsMesh LoadAscii(string path)
	{
		var facets = new List<Facet>();
		int idx = 0;
		Vector3[] tri = new Vector3[3];
		int vIdx = 0;

		foreach (var line in File.ReadLines(path))
		{
			var t = line.Trim();
			if (t.StartsWith("vertex "))
			{
				var tok = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				tri[vIdx++] = new Vector3(
					float.Parse(tok[1], CultureInfo.InvariantCulture),
					float.Parse(tok[2], CultureInfo.InvariantCulture),
					float.Parse(tok[3], CultureInfo.InvariantCulture));

				if (vIdx == 3)
				{
					facets.Add(new Facet(idx++, tri[0], tri[1], tri[2]));
					vIdx = 0;
				}
			}
		}

		return new RcsMesh(Path.GetFileNameWithoutExtension(path), facets.ToArray());
	}
}