using System.Drawing;
using Promete.Elements;
using Promete.Example.Kernel;
using Promete.ImGui;
using UI = ImGuiNET.ImGui;

namespace Promete.Example.examples.experimental;

[Demo("/experimental/vertex", "")]
public class vertex(ImGuiPlugin ui) : Scene
{
	private VectorInt[] _vertices =
	[
		(0, 0),
		(64, 0),
		(0, 64),
	];

	private Container _container = new Container()
		.Location(64, 64);

	private bool _isDirty = false;

	private Vector _pivot = (0, 0);
	private Vector _translate = (0, 0);
	private Vector _scale = (1, 1);
	private float _angle = 0;

	public override void OnStart()
	{
		Root.Add(_container);
		ui.Render += RenderWindow;
		DrawVertices();
	}

	public override void OnDestroy()
	{
		ui.Render -= RenderWindow;
	}

	public override void OnUpdate()
	{
		if (_isDirty) DrawVertices();
	}

	private void DrawVertices()
	{
		_container.Clear();
		Span<Vector> transformed = stackalloc Vector[3];
		for (var i = 0; i < 3; i++)
		{
			var v = _vertices[i];
		}
		_container.Add(Shape.CreateTriangle(_vertices[0], _vertices[1], _vertices[2], Color.Lime));
		_isDirty = false;
	}

	private void RenderWindow()
	{
		UI.Begin("Vertex Editor");
		{
			Span<int> v0 = [_vertices[0].X, _vertices[0].Y];
			Span<int> v1 = [_vertices[1].X, _vertices[1].Y];
			Span<int> v2 = [_vertices[2].X, _vertices[2].Y];

			if (UI.InputInt2("Vertex 1", ref v0[0]))
			{
				_vertices[0] = (v0[0], v0[1]);
				_isDirty = true;
			}

			if (UI.InputInt2("Vertex 2", ref v1[0]))
			{
				_vertices[1] = (v1[0], v1[1]);
				_isDirty = true;
			}

			if (UI.InputInt2("Vertex 3", ref v2[0]))
			{
				_vertices[2] = (v2[0], v2[1]);
				_isDirty = true;
			}

			var translate = _translate.ToNumerics();
			if (UI.InputFloat2("Translate", ref translate))
			{
				_translate = translate.ToPromete();
				_isDirty = true;
			}

			var scale = _scale.ToNumerics();
			if (UI.InputFloat2("Scale", ref scale))
			{
				_scale = scale.ToPromete();
				_isDirty = true;
			}

			if (UI.InputFloat("Angle", ref _angle))
			{
				_isDirty = true;
			}

			var pivot = _pivot.ToNumerics();
			if (UI.InputFloat2("Pivot", ref pivot))
			{
				_pivot = pivot.ToPromete();
				_isDirty = true;
			}

			if (UI.Button("Reset"))
			{
				_pivot = (0, 0);
				_translate = (0, 0);
				_scale = (1, 1);
				_angle = 0;
				_isDirty = true;
			}

			UI.End();
		}
	}
}
