// Echo1_RcsSimulator\Echo1_Wpf\Rendering\FacetMaterialCache.cs
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Echo1.Wpf.Rendering;

public sealed class FacetMaterialCache
{
	private readonly Dictionary<Color, DiffuseMaterial> _cache = new();

	public DiffuseMaterial Get(Color color)
	{
		if (_cache.TryGetValue(color, out var mat)) return mat;
		mat = new DiffuseMaterial(new SolidColorBrush(color));
		mat.Freeze();
		_cache[color] = mat;
		return mat;
	}

	public void Clear() => _cache.Clear();
}