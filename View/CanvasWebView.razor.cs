using Core;
using Core.EngineSpace;
using Core.ModelSpace;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ScriptContainer;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace View
{
  public partial class CanvasWebView : IDisposable
  {
    [Inject] protected virtual IJSRuntime RuntimeService { get; set; }

    /// <summary>
    /// Mouse position
    /// </summary>
    protected IPointModel _mouse = null;

    /// <summary>
    /// Accessors
    /// </summary>
    public virtual Composer Composer { get; set; }
    public virtual Subject<Composer> Observer { get; set; }
    public virtual StreamServer Server { get; protected set; }
    public virtual ScriptMessage Bounds { get; protected set; }
    public virtual ScriptService ScaleService { get; protected set; }
    public virtual ElementReference ScaleContainer { get; protected set; }
    public virtual ElementReference CanvasContainer { get; protected set; }

    /// <summary>
    /// Events
    /// </summary>
    public virtual Action<ViewMessage> OnSize { get; set; } = o => { };
    public virtual Action<ViewMessage> OnCreate { get; set; } = o => { };

    /// <summary>
    /// Enumerate indices
    /// </summary>
    public virtual IEnumerable<IPointModel> GetIndexEnumerator()
    {
      if (Composer?.Engine is not null)
      {
        var cnt = Composer.IndexLabelCount;
        var step = Composer.Engine.IndexSize / cnt;
        var stepValue = (Composer.MaxIndex - Composer.MinIndex) / cnt;

        for (var i = 1; i < cnt; i++)
        {
          yield return new PointModel
          {
            Index = step * i,
            Value = Composer.ShowIndex(Composer.MinIndex + i * stepValue)
          };
        }
      }
    }

    /// <summary>
    /// Enumerate values
    /// </summary>
    public virtual IEnumerable<IPointModel> GetValueEnumerator()
    {
      if (Composer?.Engine is not null)
      {
        var cnt = Composer.ValueLabelCount;
        var step = Composer.Engine.ValueSize / cnt;
        var stepValue = (Composer.MaxValue - Composer.MinValue) / cnt;

        for (var i = 1; i < cnt; i++)
        {
          yield return new PointModel
          {
            Index = step * i,
            Value = Composer.ShowValue(Composer.MinValue + (cnt - i) * stepValue)
          };
        }
      }
    }

    /// <summary>
    /// Render
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public virtual Task Update(Composer message = null)
    {
      return InvokeAsync(async () =>
      {
        Composer.Engine.Clear();
        Composer.Update();

        var engine = Composer.Engine as CanvasEngine;

        using (var image = engine.Map.Encode(SKEncodedImageFormat.Webp, 100))
        {
          await Server.Stream.Writer.WriteAsync(image.ToArray());
        }

        if (message is not null && Observer is not null)
        {
          Observer.OnNext(message);
        }

        StateHasChanged();
      });
    }

    /// <summary>
    /// Setup
    /// </summary>
    /// <param name="setup"></param>
    /// <returns></returns>
    protected override async Task OnAfterRenderAsync(bool setup)
    {
      if (setup)
      {
        Dispose(0);

        Server = await (new StreamServer()).Create();
        ScaleService = new ScriptService(RuntimeService);

        await ScaleService.CreateModule();

        OnCreate(await CreateMessage());
        ScaleService.OnSize = async scriptMessage => OnSize(await CreateMessage());
      }

      await base.OnAfterRenderAsync(setup);
    }

    /// <summary>
    /// Get information about event
    /// </summary>
    /// <returns></returns>
    protected async Task<ViewMessage> CreateMessage()
    {
      Bounds = await ScaleService.GetElementBounds(CanvasContainer);

      return new ViewMessage
      {
        View = this,
        X = Bounds.Width,
        Y = Bounds.Height
      };
    }

    /// <summary>
    /// Dispose
    /// </summary>
    protected void Dispose(int o)
    {
      Composer?.Dispose();
      Server?.Dispose();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    void IDisposable.Dispose()
    {
      Dispose(0);
    }

    /// <summary>
    /// Mouse wheel event
    /// </summary>
    /// <param name="e"></param>
    protected void OnWheel(WheelEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      var isZoom = e.ShiftKey;

      switch (true)
      {
        case true when e.DeltaY > 0: _ = isZoom ? Composer.ZoomIndexScale(-1) : Composer.PanIndexScale(1); break;
        case true when e.DeltaY < 0: _ = isZoom ? Composer.ZoomIndexScale(1) : Composer.PanIndexScale(-1); break;
      }

      Update(Composer);
    }

    /// <summary>
    /// Horizontal drag and resize event
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseMove(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      var position = new PointModel
      {
        Index = e.ClientX,
        Value = e.ClientY
      };

      if (_mouse is null)
      {
        _mouse = position;

        return;
      }

      if (e.Buttons == 1)
      {
        var deltaX = _mouse.Index - position.Index;
        var deltaY = _mouse.Value - position.Value;
        var isZoom = e.ShiftKey;

        _mouse = position;

        switch (true)
        {
          case true when deltaX > 0: _ = isZoom ? Composer.ZoomIndexScale(-1) : Composer.PanIndexScale(1); break;
          case true when deltaX < 0: _ = isZoom ? Composer.ZoomIndexScale(1) : Composer.PanIndexScale(-1); break;
        }

        switch (true)
        {
          case true when deltaY > 0: Composer.ZoomValueScale(-1); break;
          case true when deltaY < 0: Composer.ZoomValueScale(1); break;
        }
      }
      else
      {
        _mouse = null;
      }

      Update(Composer);
    }

    /// <summary>
    /// Double clck event in the view area
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseDown(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }

      Composer.ValueDomain = null;

      Update(Composer);
    }

    /// <summary>
    /// Mouse leave event
    /// </summary>
    /// <param name="e"></param>
    protected void OnMouseLeave(MouseEventArgs e)
    {
      if (Composer?.Engine is null)
      {
        return;
      }
    }
  }
}
