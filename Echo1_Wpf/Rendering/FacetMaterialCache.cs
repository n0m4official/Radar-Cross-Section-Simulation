using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Echo1.Wpf.Rendering;

/// <summary>
/// Caches DiffuseMaterial instances by color to avoid GC pressure
/// from creating thousands of materials per frame.
/// </summary>
public sealed class FacetMaterialCache
{
	private readonly Dictionary<Color, DiffuseMaterial> _cache = new();

	public DiffuseMaterial Get(Color color)
	{
		if (_cache.TryGetValue(color, out var mat)) return mat;
		mat = new DiffuseMaterial(new SolidColorBrush(color));
		mat.Freeze();  // freeze = GPU upload once, massive perf gain
		_cache[color] = mat;
		return mat;
	}

	public void Clear() => _cache.Clear();
}