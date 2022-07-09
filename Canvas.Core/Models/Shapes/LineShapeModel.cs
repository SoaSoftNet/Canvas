using System.Collections.Generic;

namespace Canvas.Core.ModelSpace
{
  public class LineShapeModel : ComponentModel, IComponentModel
  {
    public virtual IList<int> IndexLevels { get; set; }
    public virtual IList<double> ValueLevels { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public LineShapeModel()
    {
      IndexLevels = new List<int>();
      ValueLevels = new List<double>();
    }

    /// <summary>
    /// Update
    /// </summary>
    public override void UpdateShape()
    {
      var pointMin = new ItemModel { Index = 0, Value = 0 };
      var pointMax = new ItemModel { Index = 0, Value = 0 };
      var points = new IItemModel[]
      {
        pointMin, 
        pointMax
      };

      foreach (var level in IndexLevels)
      {
        var pixelLevel = Composer.GetPixels(Engine, level, 0);

        points[0].Index = pointMax.Index = pixelLevel.Index;
        points[0].Value = 0;
        points[1].Value = Engine.ValueSize;

        Engine.CreateLine(points, this);
      }

      foreach (var level in ValueLevels)
      {
        var pixelLevel = Composer.GetPixels(Engine, 0, level);

        points[0].Value = pointMax.Value = pixelLevel.Value;
        points[0].Index = 0;
        points[1].Index = Engine.IndexSize;

        Engine.CreateLine(points, this);
      }
    }
  }
}
