using Canvas.Core.EnumSpace;
using Canvas.Core.ShapeSpace;
using System.Collections.Generic;

namespace Canvas.Core.ModelSpace
{
  public class LineShape : GroupShape, IGroupShape
  {
    /// <summary>
    /// Render the shape
    /// </summary>
    /// <param name="index"></param>
    /// <param name="name"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    public override void CreateShape(int index, string name, IList<IShape> items)
    {
      var current = Y;
      var previous = GetItem(index - 1, name, items)?.Y;

      if (current is null)
      {
        return;
      }

      var component = Composer.Components[nameof(ComponentEnum.ShapeSection)];
      var coordinates = new DataModel[]
      {
        Composer.GetPixels(Engine, index, (previous ?? current).Value),
        Composer.GetPixels(Engine, index + 1, current.Value)
      };

      Engine.CreateLine(coordinates, Component ?? component);
    }
  }
}
